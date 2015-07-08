using UnityEngine;
using System;
using System.IO;
using System.ComponentModel;
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
  private RenderTexture m_cameraRenderTexture;
  private RenderTexture m_currentRenderTexture;
  private Texture2D m_cameraTexture2D;
  private Rect m_cameraRect;
  private int m_layersToIgnore; // Bit-array represented in int32. 1 = Ignore. 0 = Do not ignore
  private float m_countdownTimer;

  // Queue and Thread required to optimize camera recorder
  private const int QUEUE_LIMIT = 4;
  private BackgroundWorker m_saveRawWorker; // Responsible for saving raw files
  private BackgroundWorker m_loadRawWorker; // Responsible for loading raw files
  private BackgroundWorker m_saveImgWorker; // Responsible for saving processed files
  private Stack<string> m_rawFilesStack; // We'll write to most recent rawFile
  private Queue<KeyValuePair<int, byte[]>> m_rawQueue; // We'll process oldest raw data
  private Queue<KeyValuePair<string, byte[]>> m_imgQueue; // We'll process oldest img data
  
  // Time objects used for countdown and maintaining frames-per-second
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
        m_saveRawWorker.CancelAsync();
        m_loadRawWorker.CancelAsync();
        m_saveImgWorker.CancelAsync();
        m_rawQueue.Clear();
        m_imgQueue.Clear();
        break;
      case CamRecorderState.Countdown:
        countdownRemaining = m_countdownTimer;
        m_startTime = Time.time;
        m_targetTime = m_startTime + m_countdownTimer;
        break;
      case CamRecorderState.Recording:

        PrepareCamRecorder();
        countdownRemaining = 0.0f;
        framesRecorded = 0;
        passedFrames = 0;
        failedFrames = 0;
        duration = 0.0f;
        m_startTime = Time.time;
        m_targetTime = m_startTime + m_targetInterval;
        m_saveRawWorker.RunWorkerAsync(directory + "/test.txt");
        break;
      case CamRecorderState.Processing:
        framesProcessed = 0;
        m_saveRawWorker.CancelAsync();
        m_loadRawWorker.RunWorkerAsync(directory + "/test.txt");
        m_saveImgWorker.RunWorkerAsync();
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

  private void SaveRawQueue(object sender, DoWorkEventArgs e)
  {
    BackgroundWorker worker = (BackgroundWorker)sender;
    string filename = (string)e.Argument;
    BinaryWriter writer = new BinaryWriter(File.Open(filename, FileMode.Create));
    KeyValuePair<int, byte[]> rawData;
    while (!worker.CancellationPending)
    {
      try
      {
        lock (((ICollection)m_rawQueue).SyncRoot)
        {
          if (m_rawQueue.Count == 0)
            continue;
          rawData = m_rawQueue.Dequeue();
        }

        writer.Write(BitConverter.GetBytes(rawData.Key)); // Raw Data Index
        writer.Write(BitConverter.GetBytes(rawData.Value.Length)); // Raw Data Length
        writer.Write(rawData.Value); // Raw Data
        passedFrames++;
      }
      catch (IOException)
      {
        failedFrames++;
      }
    }
    writer.Close();
  }

  private void LoadRawQueue(object sender, DoWorkEventArgs e)
  {
    BackgroundWorker worker = (BackgroundWorker)sender;
    string filename = (string)e.Argument;
    BinaryReader reader = new BinaryReader(File.Open(filename, FileMode.Open));
    while (!worker.CancellationPending && reader.BaseStream.Position < reader.BaseStream.Length)
    {
      try
      {
        int rawIndex = BitConverter.ToInt32(reader.ReadBytes(sizeof(int)), 0);
        int rawDataSize = BitConverter.ToInt32(reader.ReadBytes(sizeof(int)), 0);
        byte[] rawData = reader.ReadBytes(rawDataSize);

        while (!worker.CancellationPending)
        {
          lock (((ICollection)m_rawQueue).SyncRoot)
          {
            if (m_rawQueue.Count < QUEUE_LIMIT)
            {
              m_rawQueue.Enqueue(new KeyValuePair<int, byte[]>(rawIndex, rawData));
              break;  
            }
          }
        }
      }
      catch (IOException) { }
    }
    reader.Close();
  }

  private void SaveImgQueue(object sender, DoWorkEventArgs e)
  {
    BackgroundWorker worker = (BackgroundWorker)sender;
    BinaryWriter writer;
    KeyValuePair<string, byte[]> imgData;
    while (!worker.CancellationPending)
    {
      try
      {
        lock (((ICollection)m_imgQueue).SyncRoot)
        {
          if (m_imgQueue.Count == 0)
            continue;
          imgData = m_imgQueue.Dequeue();
        }

        writer = new BinaryWriter(File.Open(imgData.Key, FileMode.Create));
        writer.Write(imgData.Value);
        writer.Close();
      }
      catch (IOException) { }
    }
  }

  private void SaveRawTexture()
  {
    duration = Time.time - m_startTime;
    if (Time.time > m_targetTime)
    {
      m_currentRenderTexture = RenderTexture.active;
      RenderTexture.active = m_cameraRenderTexture;
      m_cameraTexture2D.ReadPixels(m_cameraRect, 0, 0, false);
      RenderTexture.active = m_currentRenderTexture;

      framesRecorded++;
      lock (((ICollection)m_rawQueue).SyncRoot)
      {
        if (m_rawQueue.Count < QUEUE_LIMIT)
          m_rawQueue.Enqueue(new KeyValuePair<int, byte[]>(framesRecorded, m_cameraTexture2D.GetRawTextureData()));
        else
          failedFrames++;
      }
      m_targetTime = m_startTime + m_targetInterval * (framesRecorded + 1);
    }
  }

  private void ProcessRawTexture()
  {
    KeyValuePair<int, byte[]> rawData;
    try
    {
      lock (((ICollection)m_rawQueue).SyncRoot)
      {
        if (m_rawQueue.Count == 0)
          return;
        rawData = m_rawQueue.Dequeue();
      }
      
      string filename = directory + "/" + rawData.Key.ToString();
      m_cameraTexture2D.LoadRawTextureData(rawData.Value);

      while (true)
      {
        lock (((ICollection)m_imgQueue).SyncRoot)
        {
          if (m_imgQueue.Count < QUEUE_LIMIT)
          {
            if (highResolution)
              m_imgQueue.Enqueue(new KeyValuePair<string, byte[]>(filename + ".png", m_cameraTexture2D.EncodeToPNG()));
            else
              m_imgQueue.Enqueue(new KeyValuePair<string, byte[]>(filename + ".jpg", m_cameraTexture2D.EncodeToJPG()));
            break;
          }
        }
      }
      framesProcessed++;
    }
    catch (IOException)
    {
      Debug.LogWarning("ConvertRawToPNG: File cannot be read. Adding data back into queue");
    }
  }

  private void PrepareCamRecorder()
  {
    if ((syncCamera != null) && (syncCamera.pixelRect != m_camera.pixelRect))
    {
      m_camera.CopyFrom(syncCamera);
      int width = m_camera.pixelWidth;
      int height = m_camera.pixelHeight;
      if (m_cameraRenderTexture != null)
        Destroy(m_cameraRenderTexture);
      m_cameraRenderTexture = new RenderTexture(width, height, 24);
      m_cameraTexture2D.Resize(width, height);
      m_cameraRect.width = width;
      m_cameraRect.height = height;
    }
    m_camera.cullingMask &= ~(m_layersToIgnore);
    m_camera.targetTexture = m_cameraRenderTexture;
    
    try
    {
      if (!System.IO.Directory.Exists(directory))
        System.IO.Directory.CreateDirectory(directory);
    }
    catch (IOException)
    {
      Debug.LogWarning("Unable to create directory: " + directory);
    }
  }

  private void SetupCamera()
  {
    m_camera = GetComponent<Camera>();
    if (frameRate <= 0)
      frameRate = 30;
    m_targetInterval = 1.0f / (float)frameRate;

    int width = m_camera.pixelWidth;
    int height = m_camera.pixelHeight;
    m_cameraRenderTexture = new RenderTexture(width, height, 24);
    m_cameraTexture2D = new Texture2D(width, height, TextureFormat.RGB24, false);
    m_cameraRect = new Rect(0, 0, width, height);
    m_camera.targetTexture = m_cameraRenderTexture;
    directory = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
    SetCountdown(0.0f);
    ResetLayerToIgnore();
  }

  private void SetupMultithread()
  {
    m_rawFilesStack = new Stack<string>();
    m_rawQueue = new Queue<KeyValuePair<int, byte[]>>();
    m_imgQueue = new Queue<KeyValuePair<string, byte[]>>();

    m_saveRawWorker = new BackgroundWorker();
    m_saveRawWorker.DoWork += SaveRawQueue;
    m_saveRawWorker.WorkerSupportsCancellation = true;

    m_loadRawWorker = new BackgroundWorker();
    m_loadRawWorker.DoWork += LoadRawQueue;
    m_loadRawWorker.WorkerSupportsCancellation = true;

    m_saveImgWorker = new BackgroundWorker();
    m_saveImgWorker.DoWork += SaveImgQueue;
    m_saveImgWorker.WorkerSupportsCancellation = true;
  }

  void OnDestroy()
  {
    m_saveRawWorker.CancelAsync();
    m_loadRawWorker.CancelAsync();
    m_saveImgWorker.CancelAsync();
    m_cameraRenderTexture.Release();
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
        SaveRawTexture();
        break;
      case CamRecorderState.Processing:
        if (framesProcessed == framesRecorded)
        {
          StopProcessing();
        }
        else
        {
          ProcessRawTexture();
        }
        break;
      default:
        break;
    }
  }
}