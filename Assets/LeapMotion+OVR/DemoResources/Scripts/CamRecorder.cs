using UnityEngine;
using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

[RequireComponent (typeof(Camera))]
public class CamRecorder : MonoBehaviour 
{
  public Camera copyCameraProperties;
  public int frameRate = 30;

  [HideInInspector]
  public float progress = 0.0f;
  [HideInInspector]
  public float duration = 0.0f;

  private Camera m_camera;
  private RenderTexture m_cameraRenderTexture;
  private Texture2D m_cameraTexture2D;
  private Rect m_cameraRect;

  private Queue<string> m_loadQueue;
  private Queue<KeyValuePair<string, byte[]>> m_saveQueue;
  private Thread m_QueueThread;

  private int m_saveCount = 0;
  private float m_startTime = 0.0f;
  private float m_targetTime = 0.0f;
  private float m_targetInterval = 0.0f;

  private EventWaitHandle m_saveQueueEnqueueEvent;

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
      KeyValuePair<string, byte[]> item;
      lock (((ICollection)m_saveQueue).SyncRoot)
      {
        item = m_saveQueue.Dequeue();
      }
      try
      {
        System.IO.File.WriteAllBytes(item.Key, item.Value);
      }
      catch (IOException)
      {
        Debug.Log("ProcessQueue: File cannot be saved. Adding data back into queue");
        lock (((ICollection)m_saveQueue).SyncRoot)
        {
          m_saveQueue.Enqueue(item);
        }
      }
    }
  }

  private void AddToLoadQueue(string filename)
  {
    m_loadQueue.Enqueue(filename);
  }

  private void AddToSaveQueue(string filename, byte[] data)
  {
    lock (((ICollection)m_saveQueue).SyncRoot)
    {
      m_saveQueue.Enqueue(new KeyValuePair<string, byte[]>(filename, data));
    }
  }

  private void SaveRawFrame()
  {
    RenderTexture currentRenderTexture = RenderTexture.active;
    RenderTexture.active = m_cameraRenderTexture;
    m_cameraTexture2D.ReadPixels(m_cameraRect, 0, 0, false);
    string filename = m_saveCount.ToString() + ".png";
    AddToLoadQueue(filename);
    AddToSaveQueue(filename, m_cameraTexture2D.GetRawTextureData());
    m_saveCount++;
    RenderTexture.active = currentRenderTexture;
  }

  private void ConvertRawToImg()
  {
    string filename = m_loadQueue.Dequeue();
    try
    {
      byte[] rawData = System.IO.File.ReadAllBytes(filename);
      m_cameraTexture2D.LoadRawTextureData(rawData);
      AddToSaveQueue(filename, m_cameraTexture2D.EncodeToPNG());
    }
    catch (IOException)
    {
      Debug.Log("ConvertRawToImg: File cannot be read. Adding data back into queue");
      m_loadQueue.Enqueue(filename);
    }
  }

  void SetupCamera()
  {
    m_camera = GetComponent<Camera>();
    if (frameRate > 0)
      m_targetInterval = 1.0f / (float)frameRate;

    SetCameraProperties();
  }

  void SetCameraProperties()
  {
    int width = m_camera.pixelWidth;
    int height = m_camera.pixelHeight;

    if (m_cameraRenderTexture == null)
    {
      m_cameraRenderTexture = new RenderTexture(width, height, 24);
    }
    else
    {
      m_cameraRenderTexture.width = width;
      m_cameraRenderTexture.height = height;
    }

    if (m_cameraTexture2D == null)
    {
      m_cameraTexture2D = new Texture2D(width, height, TextureFormat.RGB24, false);
    }
    else
    {
      m_cameraTexture2D.Resize(width, height);
    }

    if (m_cameraRect == null)
    {
      m_cameraRect = new Rect(0, 0, width, height);
    }
    else
    {
      m_cameraRect.width = width;
      m_cameraRect.height = height;
    }

    m_camera.targetTexture = m_cameraRenderTexture;
  }

  void UpdateCameraProperties()
  {
    if (copyCameraProperties == null)
      return;

    if (copyCameraProperties.pixelRect != m_camera.pixelRect)
    {
      m_camera.CopyFrom(copyCameraProperties); 
      SetCameraProperties();
    }
  }

  void SetupMultithread()
  {
    m_loadQueue = new Queue<string>();
    m_saveQueue = new Queue<KeyValuePair<string, byte[]>>();
    m_QueueThread = new Thread(ProcessSaveQueue);
    m_QueueThread.Start();
    m_saveQueueEnqueueEvent = new AutoResetEvent(false); 
  }

  private void CheckQueue()
  {
    lock (((ICollection)m_saveQueue).SyncRoot)
    {
      if (m_saveQueue.Count > 0)
      {
        m_saveQueueEnqueueEvent.Set();
      }
    }
  }

  void Start()
  {
    SetupCamera();
    SetupMultithread();
  }

  void LateUpdate()
  {
    UpdateCameraProperties();
    CheckQueue();
    switch (m_camRecorderState)
    {
      case CamRecorderState.Idle:
        Debug.Log("Idle");
        if (Input.GetKeyDown(KeyCode.Z))
        {
          m_saveCount = 0;
          m_startTime = Time.time;
          m_camRecorderState = CamRecorderState.Recording;
        }
        break;
      case CamRecorderState.Recording:
        Debug.Log("Recording");
        duration = Time.time - m_startTime;
        if (Input.GetKeyDown(KeyCode.Z))
        {
          m_camRecorderState = CamRecorderState.Processing;
        }
        break;
      case CamRecorderState.Processing:
        Debug.Log("Processing");
        progress = (m_saveCount > 0) ? (m_saveCount - m_loadQueue.Count) / m_saveCount : 0.0f;
        if (m_loadQueue.Count == 0 && m_saveQueue.Count == 0)
        {
          m_camRecorderState = CamRecorderState.Idle;
        }

        if (m_loadQueue.Count != 0)
        {
          ConvertRawToImg();
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
      if (Time.time > m_targetTime)
      {
        SaveRawFrame();
        m_targetTime = Time.time + m_targetInterval;
      }
    }
  }
}