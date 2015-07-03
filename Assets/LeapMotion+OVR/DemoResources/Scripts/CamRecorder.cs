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
  public float progress = 0.0f;
  [HideInInspector]
  public float duration = 0.0f;
  [HideInInspector]
  public string directory = "";
  [HideInInspector]
  public int frameCount = 0;

  // Objects required to record a camera
  private Camera m_camera;
  private RenderTexture m_cameraRenderTexture;
  private Texture2D m_cameraTexture2D;
  private Rect m_cameraRect;

  // Queue and Thread required to optimize camera recorder
  private Queue<string> m_loadQueue;
  private Queue<KeyValuePair<string, byte[]>> m_saveQueue;
  private Thread m_QueueThread;
  private EventWaitHandle m_saveQueueEnqueueEvent;
  private bool m_terminateThreads;
  
  private float m_startTime = 0.0f;
  private float m_targetTime = 0.0f;
  private float m_targetInterval = 0.0f;

  private float m_quality = 0.0f;

  private bool m_performance_test = true;
  private int m_performance_frames = 30;
  private float m_performance = 0.0f;

  private enum CamRecorderState
  {
    Idle,
    Recording,
    Processing
  }
  private CamRecorderState m_camRecorderState = CamRecorderState.Idle;

  private void ProcessSaveQueue()
  {
    while (m_saveQueueEnqueueEvent.WaitOne())
    {
      if (m_terminateThreads)
        return;

      KeyValuePair<string, byte[]> item;
      lock (((ICollection)m_saveQueue).SyncRoot)
      {
        
        item = m_saveQueue.Dequeue();
        if (m_saveQueue.Count == 0)
          m_saveQueueEnqueueEvent.Reset(); // If list is empty, disable signal
      }

      try
      {
        System.IO.File.WriteAllBytes(item.Key, item.Value);
      }
      catch (IOException)
      {
        Debug.LogWarning("ProcessQueue: File cannot be saved. Adding data back into queue");
        AddToSaveQueue(item.Key, item.Value);
      }
    }
  }

  private void AddToSaveQueue(string filename, byte[] data)
  {
    lock (((ICollection)m_saveQueue).SyncRoot)
    {
      m_saveQueue.Enqueue(new KeyValuePair<string, byte[]>(filename, data));
    }
    m_saveQueueEnqueueEvent.Set(); // If add to list (list is not empty), enable signal
  }

  private void SaveRawData()
  {
    duration = Time.time - m_startTime;
    if (Time.time > m_targetTime)
    {
      RenderTexture currentRenderTexture = RenderTexture.active;
      RenderTexture.active = m_cameraRenderTexture;
      m_cameraTexture2D.ReadPixels(m_cameraRect, 0, 0, false);
      RenderTexture.active = currentRenderTexture;

      frameCount++;
      string filename = directory + "/" + frameCount.ToString();
      filename += (m_quality == 1.0f) ? ".png" : ".jpg";
      m_loadQueue.Enqueue(filename);
      AddToSaveQueue(filename, m_cameraTexture2D.GetRawTextureData());
      m_targetTime = m_startTime + m_targetInterval * (frameCount + 1);

      if (m_performance_test && frameCount == m_performance_frames)
      {
        StopRecording();
      }
    }
  }

  private void ProcessRawData()
  {
    progress = (frameCount > 0) ? ((float)frameCount - (float)m_loadQueue.Count) / (float)frameCount : 0.0f;

    if (m_loadQueue.Count == 0)
      return;

    string filename = m_loadQueue.Dequeue();
    try
    {
      byte[] rawData = System.IO.File.ReadAllBytes(filename);
      m_cameraTexture2D.LoadRawTextureData(rawData);
      if (m_quality == 1.0f)
        AddToSaveQueue(filename, m_cameraTexture2D.EncodeToPNG());
      else
        AddToSaveQueue(filename, m_cameraTexture2D.EncodeToJPG((int)(m_quality*100.0f)));
    }
    catch (IOException)
    {
      Debug.LogWarning("ConvertRawToPNG: File cannot be read. Adding data back into queue");
      m_loadQueue.Enqueue(filename);
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
  }

  private void SetupMultithread()
  {
    m_terminateThreads = false;
    m_loadQueue = new Queue<string>();
    m_saveQueue = new Queue<KeyValuePair<string, byte[]>>();
    m_saveQueueEnqueueEvent = new ManualResetEvent(false);
    m_QueueThread = new Thread(ProcessSaveQueue);
    m_QueueThread.Start();
  }

  public bool SetDirectory(string directory)
  {
    try
    {
      directory = Application.persistentDataPath + "/" + directory;
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

  public bool IsIdling()
  {
    return (m_camRecorderState == CamRecorderState.Idle);
  }

  public bool IsRecording()
  {
    return (m_camRecorderState == CamRecorderState.Recording);
  }

  public bool IsProcessing()
  {
    return (m_camRecorderState == CamRecorderState.Processing);
  }

  /// <summary>
  /// Begin Recording
  /// [1.0f] Generates PNG. [0.0f]...(1.0f) Generates JPG with quality between 0.0 ~ 1.0
  /// </summary>
  /// <param name="quality"></param>
  public void StartRecording(float quality = 1.0f)
  {
    m_quality = Mathf.Clamp(quality, 0.0f, 1.0f);
    if (m_camRecorderState == CamRecorderState.Idle)
    {
      frameCount = 0;
      m_startTime = Time.time;
      m_targetTime = m_startTime + m_targetInterval * (frameCount + 1);

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
        m_camera.targetTexture = m_cameraRenderTexture;
      }

      if (m_performance_test)
        m_performance = Time.time;
      m_camRecorderState = CamRecorderState.Recording;
    }
  }

  /// <summary>
  /// Stops Recording
  /// </summary>
  public void StopRecording()
  {
    if (m_camRecorderState == CamRecorderState.Recording)
    {
      if (m_performance_test)
      {
        Debug.Log(m_targetInterval);
        Debug.Log(Time.time - m_startTime);
        Debug.Log((Time.time - m_startTime) / (float)frameRate);
        Debug.Log(Time.time - m_performance);
      }
      m_camRecorderState = CamRecorderState.Processing;
    }
  }

  /// <summary>
  /// Stops Processing (WARNING: May leave corrupt files)
  /// </summary>
  public void StopProcessing()
  {
    if (m_camRecorderState == CamRecorderState.Processing)
    {
      m_loadQueue.Clear();
      m_saveQueue.Clear();
      m_camRecorderState = CamRecorderState.Idle;
    }
  }

  void OnDestroy()
  {
    m_saveQueueEnqueueEvent.Set();
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
    switch (m_camRecorderState)
    {
      case CamRecorderState.Idle:
        progress = 1.0f;
        break;
      case CamRecorderState.Recording:
        SaveRawData();
        break;
      case CamRecorderState.Processing:
        ProcessRawData();
        if (m_loadQueue.Count == 0 && m_saveQueue.Count == 0)
        {
          m_camRecorderState = CamRecorderState.Idle;
        }
        break;
      default:
        break;
    }
  }
}