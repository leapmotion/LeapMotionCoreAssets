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
  private bool m_activeThreads;

  private string m_directory = "";
  private int m_saveCount = 0;
  private float m_startTime = 0.0f;
  private float m_targetTime = 0.0f;
  private float m_targetInterval = 0.0f;

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
      if (!m_activeThreads)
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
        Debug.Log("ProcessQueue: File cannot be saved. Adding data back into queue");
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

  private void RecordRawData()
  {
    if (Time.time > m_targetTime)
    {
      RenderTexture currentRenderTexture = RenderTexture.active;
      RenderTexture.active = m_cameraRenderTexture;
      m_cameraTexture2D.ReadPixels(m_cameraRect, 0, 0, false);
      string filename = m_directory + m_saveCount.ToString() + ".png";
      m_loadQueue.Enqueue(filename);
      AddToSaveQueue(filename, m_cameraTexture2D.GetRawTextureData());
      m_saveCount++;
      RenderTexture.active = currentRenderTexture;
      m_targetTime = Time.time + m_targetInterval;
    }
  }

  private void ConvertRawDataToImages()
  {
    if (m_loadQueue.Count == 0)
      return;

    string filename = m_loadQueue.Dequeue();
    try
    {
      byte[] rawData = System.IO.File.ReadAllBytes(filename);
      m_cameraTexture2D.LoadRawTextureData(rawData);
      AddToSaveQueue(filename, m_cameraTexture2D.EncodeToPNG());
    }
    catch (IOException)
    {
      Debug.Log("ConvertRawToPNG: File cannot be read. Adding data back into queue");
      m_loadQueue.Enqueue(filename);
    }
    progress = (m_saveCount > 0) ? (m_saveCount - m_loadQueue.Count) / m_saveCount : 0.0f;
  }

  void UpdateCameraProperties()
  {
    if (syncCamera == null)
      return;

    if (syncCamera.pixelRect != m_camera.pixelRect)
    {
      m_camera.CopyFrom(syncCamera);
      int width = m_camera.pixelWidth;
      int height = m_camera.pixelHeight;
      m_cameraRenderTexture.width = width;
      m_cameraRenderTexture.height = height;
      m_cameraTexture2D.Resize(width, height);
      m_cameraRect.width = width;
      m_cameraRect.height = height;
      m_camera.targetTexture = m_cameraRenderTexture;
    }
  }

  void SetupRecordingParameters()
  {
    m_directory = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss/");
    bool directoryExists = System.IO.Directory.Exists(m_directory);
    if (!directoryExists)
      System.IO.Directory.CreateDirectory(m_directory);
    m_saveCount = 0;
    m_startTime = Time.time;
  }

  void SetupCamera()
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

  void SetupMultithread()
  {
    m_loadQueue = new Queue<string>();
    m_saveQueue = new Queue<KeyValuePair<string, byte[]>>();
    m_QueueThread = new Thread(ProcessSaveQueue);
    m_QueueThread.Start();
    m_saveQueueEnqueueEvent = new ManualResetEvent(false);
    m_activeThreads = true;
  }

  public void StartRecording()
  {

  }

  public void StopRecording()
  {

  }

  void OnDestroy()
  {
    m_saveQueueEnqueueEvent.Set();
    m_activeThreads = false;
    m_QueueThread.Join();
  }

  void Start()
  {
    SetupCamera();
    SetupMultithread();
  }

  void LateUpdate()
  {
    switch (m_camRecorderState)
    {
      case CamRecorderState.Idle:
        UpdateCameraProperties();
        if (Input.GetKeyDown(KeyCode.Z))
        {
          SetupRecordingParameters();        
          m_camRecorderState = CamRecorderState.Recording;
        }
        break;
      case CamRecorderState.Recording:
        duration = Time.time - m_startTime;
        if (Input.GetKeyDown(KeyCode.Z))
        {
          m_camRecorderState = CamRecorderState.Processing;
        }
        break;
      case CamRecorderState.Processing:
        ConvertRawDataToImages();
        if (m_loadQueue.Count == 0 && m_saveQueue.Count == 0)
        {
          m_camRecorderState = CamRecorderState.Idle;
        }
        break;
      default:
        break;
    }
  }

  void OnPostRender()
  {
    if (m_camRecorderState == CamRecorderState.Recording)
    {
      RecordRawData();
    }
  }
}