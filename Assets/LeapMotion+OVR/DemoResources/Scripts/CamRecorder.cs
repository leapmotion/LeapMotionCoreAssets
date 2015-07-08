using UnityEngine;
using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

[RequireComponent (typeof(Camera))]
public class CamRecorder : MonoBehaviour 
{
  public Camera syncCamera;
  public int frameRate = 30;

  [HideInInspector]
  public float duration = 0.0f;
  [HideInInspector]
  public string directory = "";
  [HideInInspector]
  public int framesRecorded = 0;
  [HideInInspector]
  public int framesProcessed = 0;
  [HideInInspector]
  public int passedFrames = 0;
  [HideInInspector]
  public int failedFrames = 0;
  [HideInInspector]
  public bool highResolution = false;
  [HideInInspector]
  public float countdownRemaining = 0.0f;

  // Objects required to record a camera
  private Camera m_camera;
  private int m_width;
  private int m_height;
  private RenderTexture m_cameraRenderTexture;
  private RenderTexture m_currentRenderTexture;
  private Texture2D m_cameraTexture2D;
  private Rect m_cameraRect;
  private int m_layersToIgnore; // Bit-array represented in int32. 1 = Ignore. 0 = Do not ignore
  private float m_countdownTimer;

  // Queue and Thread required to optimize camera recorder
  private Queue<string> m_loadQueue;
  private Queue<KeyValuePair<string, byte[]>> m_saveQueue;
  private const int SAVE_QUEUE_LIMIT = 3;
  private Thread m_QueueThread;
  private EventWaitHandle m_saveQueueEnable;
  private bool m_terminateThreads;
  
  private float m_startTime = 0.0f;
  private float m_targetTime = 0.0f;
  private float m_targetInterval = 0.0f;

  private enum CamRecorderState
  {
    Idle,
    Countdown,
    Recording,
    Processing
  }
  private CamRecorderState m_state = CamRecorderState.Idle;
  private void SetState(CamRecorderState state)
  {
    m_state = state;
    switch (m_state)
    {
      case CamRecorderState.Idle:  
        m_saveQueueEnable.Reset(); // Disable Save Thread
        m_loadQueue.Clear();
        m_saveQueue.Clear();
        break;
      case CamRecorderState.Countdown:
        countdownRemaining = m_countdownTimer;
        m_startTime = Time.time;
        m_targetTime = m_startTime + m_countdownTimer;
        break;
      case CamRecorderState.Recording:
        countdownRemaining = 0.0f;
        m_saveQueueEnable.Set(); // Enable Save Thread
        SyncCameras();
        framesRecorded = 0;
        duration = 0.0f;
        m_startTime = Time.time;
        m_targetTime = m_startTime + m_targetInterval;
        break;
      case CamRecorderState.Processing:
        framesProcessed = 0;
        passedFrames = 0;
        failedFrames = 0;
        break;
      default:
        break;
    }
  }
  
  /// <summary>
  /// Begin Recording
  /// [1.0f] Generates PNG. [0.0f]...(1.0f) Generates JPG with quality between 0.0 ~ 1.0
  /// </summary>
  /// <param name="quality"></param>
  public void StartRecording()
  {
    if (m_state == CamRecorderState.Idle)
    {
      SetState(CamRecorderState.Countdown);
    }
  }

  /// <summary>
  /// If counting down, resets back to idle. If recording, proceed to processing
  /// </summary>
  public void StopRecording()
  {
    if (m_state == CamRecorderState.Recording)
    {
      SetState(CamRecorderState.Processing);
    } 
    else if (m_state == CamRecorderState.Countdown) 
    {
      SetState(CamRecorderState.Idle);
    }
  }

  /// <summary>
  /// Stops Processing (WARNING: May leave corrupt files if triggered early)
  /// </summary>
  public void StopProcessing()
  {
    if (m_state == CamRecorderState.Processing)
    {
      SetState(CamRecorderState.Idle);
    }
  }

  public bool IsIdling()
  {
    return (m_state == CamRecorderState.Idle);
  }

  public bool IsCountingDown()
  {
    return (m_state == CamRecorderState.Countdown);
  }

  public bool IsRecording()
  {
    return (m_state == CamRecorderState.Recording);
  }

  public bool IsProcessing()
  {
    return (m_state == CamRecorderState.Processing);
  }

  public void AddLayerToIgnore(int layer)
  {
    m_layersToIgnore |= (1 << layer);
  }

  public void RemoveLayerToIgnore(int layer)
  {
    m_layersToIgnore &= ~(1 << layer);
  }

  public void ResetLayerToIgnore()
  {
    m_layersToIgnore = 0;
  }

  public void SetCountdown(float seconds)
  {
    m_countdownTimer = seconds;
  }

  public bool SetDirectory(string directory)
  {
    if (directory == "")
      directory = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

    try
    {
      if (!System.IO.Directory.Exists(directory))
        System.IO.Directory.CreateDirectory(directory);
    }
    catch (IOException)
    {
      Debug.LogWarning("Unable to create directory: " + directory);
      return false;
    }

    this.directory = directory;
    return true;
  }

  private void ProcessSaveQueue()
  {
    BinaryWriter writer;
    KeyValuePair<string, byte[]> item;
    while (m_saveQueueEnable.WaitOne())
    {
      if (m_terminateThreads)
        return;

      lock (((ICollection)m_saveQueue).SyncRoot)
      {
        if (m_saveQueue.Count == 0)
        {
          continue;
        }
        item = m_saveQueue.Dequeue();
      }

      try
      {
        writer = new BinaryWriter(new FileStream(item.Key, FileMode.Create));
        writer.Write(item.Value);
        writer.Close();
        if (m_state == CamRecorderState.Recording)
          passedFrames++;
      }
      catch (IOException)
      {
        Debug.LogWarning("ProcessQueue: File cannot be saved. Adding data back into queue");
        if (m_state == CamRecorderState.Recording)
          failedFrames++;
      }
    }
  }

  private void AddToSaveQueue(string filename, byte[] data)
  {
    lock (((ICollection)m_saveQueue).SyncRoot)
    {
      if (m_saveQueue.Count >= SAVE_QUEUE_LIMIT)
      {
        Debug.LogWarning("Dropping " + filename);
        failedFrames++;
      }
      else
      {
        m_saveQueue.Enqueue(new KeyValuePair<string, byte[]>(filename, data));
      }
    }
  }

  private void SaveRawData()
  {
    duration = Time.time - m_startTime;
    if (Time.time > m_targetTime)
    {
      m_currentRenderTexture = RenderTexture.active;
      RenderTexture.active = m_cameraRenderTexture;
      m_cameraTexture2D.ReadPixels(m_cameraRect, 0, 0, false);
      RenderTexture.active = m_currentRenderTexture;

      string filename = directory + "/" + (framesRecorded + 1).ToString();
      filename += (highResolution) ? ".png" : ".jpg";
      m_loadQueue.Enqueue(filename);
      AddToSaveQueue(filename, m_cameraTexture2D.GetRawTextureData());
      framesRecorded++;
      m_targetTime = m_startTime + m_targetInterval * (framesRecorded + 1);
    }
  }

  private void ProcessRawData()
  {
    string filename = m_loadQueue.Dequeue();
    try
    {
      if (File.Exists(filename))
      {
        m_cameraTexture2D.LoadRawTextureData(System.IO.File.ReadAllBytes(filename));
      }

      if (highResolution)
        AddToSaveQueue(filename, m_cameraTexture2D.EncodeToPNG());
      else
        AddToSaveQueue(filename, m_cameraTexture2D.EncodeToJPG());

      framesProcessed++;
    }
    catch (IOException)
    {
      Debug.LogWarning("ConvertRawToPNG: File cannot be read. Adding data back into queue");
      m_loadQueue.Enqueue(filename);
      ProcessRawData();
    }
  }

  private void SyncCameras()
  {
    if ((syncCamera != null) && (syncCamera.pixelRect != m_camera.pixelRect))
    {
      m_camera.CopyFrom(syncCamera);
      m_width = m_camera.pixelWidth;
      m_height = m_camera.pixelHeight;
      if (m_cameraRenderTexture != null)
        Destroy(m_cameraRenderTexture);
      m_cameraRenderTexture = new RenderTexture(m_width, m_height, 24);
      m_cameraTexture2D.Resize(m_width, m_height);
      m_cameraRect.width = m_width;
      m_cameraRect.height = m_height;
      m_camera.targetTexture = m_cameraRenderTexture;
      m_camera.cullingMask &= ~(m_layersToIgnore);
    }
  }

  private void SetupCamera()
  {
    m_camera = GetComponent<Camera>();
    if (frameRate <= 0)
      frameRate = 30;
    m_targetInterval = 1.0f / (float)frameRate;

    m_width = m_camera.pixelWidth;
    m_height = m_camera.pixelHeight;
    m_cameraRenderTexture = new RenderTexture(m_width, m_height, 24);
    m_cameraTexture2D = new Texture2D(m_width, m_height, TextureFormat.RGB24, false);
    m_cameraRect = new Rect(0, 0, m_width, m_height);
    m_camera.targetTexture = m_cameraRenderTexture;
    m_countdownTimer = 0.0f;
    ResetLayerToIgnore();
  }

  private void SetupMultithread()
  {
    m_terminateThreads = false;
    m_loadQueue = new Queue<string>();
    m_saveQueue = new Queue<KeyValuePair<string, byte[]>>();
    m_saveQueueEnable = new ManualResetEvent(false); 
    m_QueueThread = new Thread(ProcessSaveQueue);
    m_QueueThread.Start();
  }

  void OnDestroy()
  {
    m_cameraRenderTexture.Release();
    m_saveQueueEnable.Set();
    m_terminateThreads = true;
    m_QueueThread.Join();
  }

  void Start()
  {
    SetupCamera();
    SetupMultithread();
  }

  void OnPostRender()
  {
    switch (m_state)
    {
      case CamRecorderState.Countdown:
        if (m_targetTime - Time.time > 0)
        {
          countdownRemaining = m_targetTime - Time.time;
        }
        else
        {
          SetState(CamRecorderState.Recording);
        }
        break;
      case CamRecorderState.Recording:
        SaveRawData();
        break;
      case CamRecorderState.Processing:
        if (m_loadQueue.Count == 0 && m_saveQueue.Count == 0)
        {
          StopProcessing();
        }
        else if (m_loadQueue.Count != 0)
        {
          ProcessRawData();
        }
        break;
      default:
        break;
    }
  }
}