using UnityEngine;
using System;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

public class SyncEvents
{
  private EventWaitHandle _newItemEvent;
  private EventWaitHandle _exitThreadEvent;
  private WaitHandle[] _eventArray;

  public SyncEvents()
  {
    _newItemEvent = new AutoResetEvent(false);
    _exitThreadEvent = new ManualResetEvent(false);
    _eventArray = new WaitHandle[2];
    _eventArray[0] = _newItemEvent;
    _eventArray[1] = _exitThreadEvent;
  }

  public EventWaitHandle ExitThreadEvent
  {
    get { return _exitThreadEvent; }
  }
  public EventWaitHandle NewItemEvent
  {
    get { return _newItemEvent; }
  }
  public WaitHandle[] EventArray
  {
    get { return _eventArray; }
  }
}

[RequireComponent (typeof(Camera))]
public class CamRecorder : MonoBehaviour 
{
  public int frameRate = 30;

  private Camera m_camera;
  private RenderTexture m_cameraTexture;
  private Texture2D m_cameraTextureData;
  private Rect m_cameraRect;

  private Queue<string> m_filenames;
  private Queue<KeyValuePair<string, byte[]>> m_frameQueue;
  private SyncEvents m_syncEvents;
  private Thread m_QueueThread;

  private int m_saveCount = 0;
  private float m_prevTime = 0;
  private float m_targetInterval = 0;

  private enum CamRecorderState
  {
    Idle,
    Recording,
    PostRecording,
    Saving,
    PostSaving
  }
  private CamRecorderState m_camRecorderState = CamRecorderState.Idle;

  private void ProcessQueue()
  {
    while (WaitHandle.WaitAny(m_syncEvents.EventArray) != 1)
    {
      KeyValuePair<string, byte[]> item;
      lock (((ICollection)m_frameQueue).SyncRoot)
      {
        item = m_frameQueue.Peek();
      }
      try
      {
        System.IO.File.WriteAllBytes(item.Key, item.Value);
      }
      catch { }
      lock (((ICollection)m_frameQueue).SyncRoot)
      {
        item = m_frameQueue.Dequeue();
      }
    }
  }

  private void AddFilename(string filename)
  {
    m_filenames.Enqueue(filename);
  }

  private void AddToQueue(string filename, byte[] data)
  {
    lock (((ICollection)m_frameQueue).SyncRoot)
    {
      m_frameQueue.Enqueue(new KeyValuePair<string, byte[]>(filename, data));
    }
  }

  private void SaveRawFrame()
  {
    RenderTexture currentRenderTexture = RenderTexture.active;
    RenderTexture.active = m_cameraTexture;
    m_cameraTextureData.ReadPixels(m_cameraRect, 0, 0, false);
    string filename = m_saveCount.ToString() + ".png";
    AddFilename(filename);
    AddToQueue(filename, m_cameraTextureData.GetRawTextureData());
    m_saveCount++;
    RenderTexture.active = currentRenderTexture;
  }

  private void ConvertRawToImg()
  {
    try
    {
      string filename = m_filenames.Peek();
      m_cameraTextureData.LoadRawTextureData(System.IO.File.ReadAllBytes(filename));
      AddToQueue(filename, m_cameraTextureData.EncodeToPNG());
      m_filenames.Dequeue();
    }
    catch (IOException)
    {
    }
  }

  void OnDestroy()
  {
    m_syncEvents.ExitThreadEvent.Set();
    m_QueueThread.Join();
  }

  void SetupCamera()
  {
    m_camera = GetComponent<Camera>();
    int width = m_camera.pixelWidth;
    int height = m_camera.pixelHeight;
    m_cameraTexture = new RenderTexture(width, height, 24);
    m_cameraTextureData = new Texture2D(width, height, TextureFormat.RGB24, false);
    m_cameraRect = new Rect(0, 0, width, height);
    m_camera.targetTexture = m_cameraTexture;
    if (frameRate > 0)
      m_targetInterval = 1.0f / (float)frameRate;
  }

  void SetupMultithread()
  {
    m_filenames = new Queue<string>();
    m_frameQueue = new Queue<KeyValuePair<string, byte[]>>();
    m_syncEvents = new SyncEvents();
    m_QueueThread = new Thread(ProcessQueue);
    m_QueueThread.Start();
  }

  private void CheckQueue()
  {
    lock (((ICollection)m_frameQueue).SyncRoot)
    {
      if (m_frameQueue.Count > 0)
      {
        m_syncEvents.NewItemEvent.Set();
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
    CheckQueue();
    switch (m_camRecorderState)
    {
      case CamRecorderState.Idle:
        Debug.Log("Idle");
        if (Input.GetKeyDown(KeyCode.Z))
        {
          m_saveCount = 0;
          m_camRecorderState = CamRecorderState.Recording;
        }
        break;
      case CamRecorderState.Recording:
        Debug.Log("Recording");
        if (Input.GetKeyDown(KeyCode.Z))
        {
          m_camRecorderState = CamRecorderState.PostRecording;
        }
        break;
      case CamRecorderState.PostRecording:
        Debug.Log("PostRecording");
        if (m_frameQueue.Count == 0)
        {
          m_camRecorderState = CamRecorderState.Saving;
        }
        break;
      case CamRecorderState.Saving:
        Debug.Log("Saving");
        ConvertRawToImg();
        if (m_filenames.Count == 0)
        {
          m_camRecorderState = CamRecorderState.PostSaving;
        }
        break;
      case CamRecorderState.PostSaving:
        Debug.Log("PostSaving");
        if (m_frameQueue.Count == 0)
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
      if ((Time.time - m_prevTime) > m_targetInterval)
      {
        SaveRawFrame();
        m_prevTime = Time.time;
      }
    }
  }
}