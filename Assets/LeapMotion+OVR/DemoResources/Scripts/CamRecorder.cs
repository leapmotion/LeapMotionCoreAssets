using UnityEngine;
using UnityEngine.UI;
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
  public GameObject startingIndicators;

  [HideInInspector]
  public float duration = 0.0f;
  [HideInInspector]
  public string directory = "";
  [HideInInspector]
  public int framesRecorded = 0; // Total count (Buf + Raw) of frames recorded
  [HideInInspector]
  public int framesProcessed = 0; // Total count (Buf + Img) of frames processed
  [HideInInspector]
  public int bufFramesCount = 0; // Buff Frames - Frames recorded during countdown
  [HideInInspector]
  public int rawFramesCount = 0; // Raw Frames - Raw data frames recorded
  [HideInInspector]
  public int rawFramesPassed = 0;
  [HideInInspector]
  public int rawFramesFailed = 0;
  [HideInInspector]
  public int imgFramesCount = 0; // Img Frames - Processed frames from raw data frames (Excludes buff frames)
  [HideInInspector]
  public int imgFramesPassed = 0;
  [HideInInspector]
  public int imgFramesFailed = 0;
  [HideInInspector]
  public float countdownRemaining = 0.0f;
  [HideInInspector]
  public bool useHighResolution = false;

  // Objects required to record a camera
  private Camera m_camera;
  private RenderTexture m_cameraRenderTexture;
  private RenderTexture m_currentRenderTexture;
  private Texture2D m_cameraTexture2D;
  private Rect m_cameraRect;
  private int m_layersToIgnore; // Bit-array represented in int32. 1 = Ignore. 0 = Do not ignore

  // Queue and Thread required to optimize camera recorder
  private const int QUEUE_LIMIT = 4;
  private const int TEMP_BYTE_LIMIT = 2000000000; // 2GB because FAT32 limit is 4GB
  private Stack<string> m_tempFilesStack; // We'll write to most recent temp file
  private BackgroundWorker m_tempWorker; // Responsible for save/load temp (raw/buf) data for processing
  private BackgroundWorker m_processWorker; // Responsible for saving processed files
  private Queue<KeyValuePair<int, byte[]>> m_tempQueue; // We'll process oldest temp (raw/buf) data
  private Queue<KeyValuePair<int, byte[]>> m_processQueue; // We'll process oldest img data
  private enum TempWorkerState
  {
    Save,
    Load
  }

  // Time objects used for countdown and maintaining frames-per-second
  private float m_startCountdownTime = 0.0f;
  private float m_endCountdownTime = 0.0f;
  private float m_startRecordTime = 0.0f;
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
        StopWorker(m_tempWorker);
        StopWorker(m_processWorker);
        while (m_tempFilesStack.Count > 0)
        {
          File.Delete(GetFullPath(m_tempFilesStack.Pop()));
        }
        m_tempFilesStack.Clear();
        if (
          System.IO.Directory.Exists(directory) &&
          Directory.GetFileSystemEntries(directory).Length == 0
          )
        {
          Directory.Delete(directory);
        }
        m_tempQueue.Clear();
        m_processQueue.Clear();
        if (startingIndicators != null)
          startingIndicators.gameObject.SetActive(false);
        break;
      case CamRecorderState.Countdown:
        PrepareCamRecorder();
        countdownRemaining = Mathf.Max(countdownRemaining, 0.0f);
        m_startCountdownTime = Time.time;
        m_endCountdownTime = m_startCountdownTime + countdownRemaining;
        m_targetTime = Time.time + m_targetInterval;
        bufFramesCount = 0;
        m_tempWorker.RunWorkerAsync(TempWorkerState.Save);
        break;
      case CamRecorderState.Recording:
        m_startRecordTime = Time.time;
        m_targetTime = Time.time + m_targetInterval;
        rawFramesCount = 0;
        rawFramesPassed = 0;
        rawFramesFailed = 0;
        if (startingIndicators != null)
          startingIndicators.gameObject.SetActive(true); // Enable indicators for first frame
        break;
      case CamRecorderState.Processing:
        imgFramesCount = 0;
        imgFramesPassed = 0;
        imgFramesFailed = rawFramesFailed;
        StopWorker(m_tempWorker);
        m_tempWorker.RunWorkerAsync(TempWorkerState.Load);
        m_processWorker.RunWorkerAsync();
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
  /// Stops Processing (WARNING: May leave corrupt files if triggered before processing is complete)
  /// </summary>
  public void StopProcessing()
  {
    if (m_state == CamRecorderState.Processing)
    {
      SetState(CamRecorderState.Idle);
    }
  }

  public bool IsIdling() { return (m_state == CamRecorderState.Idle); }
  public bool IsCountingDown() { return (m_state == CamRecorderState.Countdown); }
  public bool IsRecording() { return (m_state == CamRecorderState.Recording); }
  public bool IsProcessing() { return (m_state == CamRecorderState.Processing); }

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
    countdownRemaining = seconds;
  }

  // Buff images would have non-positive index
  private bool IsBufFrame(int index) { return (index < 0); }
  private string GetFullPath(string filename) { return directory + "/" + filename; }

  private void StopWorker(BackgroundWorker worker)
  {
    worker.CancelAsync();
    while (worker.IsBusy) ;
  }

  private void TempQueueWork(object sender, DoWorkEventArgs e)
  {
    BackgroundWorker worker = (BackgroundWorker)sender;
    TempWorkerState state = (TempWorkerState)e.Argument;
    KeyValuePair<int, byte[]> data = new KeyValuePair<int, byte[]>();
    if (state == TempWorkerState.Save)
    {
      BinaryWriter writer;
      while (!worker.CancellationPending)
      {
        writer = new BinaryWriter(File.Open(GetFullPath(m_tempFilesStack.Peek()), FileMode.Create));
        while (!worker.CancellationPending)
        {
          try
          {
            lock (((ICollection)m_tempQueue).SyncRoot)
            {
              if (m_tempQueue.Count == 0)
                continue;
              data = m_tempQueue.Dequeue();
            }

            writer.Write(BitConverter.GetBytes(data.Key)); // Data Index
            writer.Write(BitConverter.GetBytes(data.Value.Length)); // Data Length
            writer.Write(data.Value); // Data (Byte array)
            framesRecorded++;
            if (!IsBufFrame(data.Key))
              rawFramesPassed++;
          }
          catch (IOException)
          {
            if (!IsBufFrame(data.Key))
              rawFramesFailed++;
          }
          if (writer.BaseStream.Length > TEMP_BYTE_LIMIT)
          {
            m_tempFilesStack.Push("stack" + (m_tempFilesStack.Count + 1) + ".tmp");
            break;
          }
        }
        writer.Close();
      }
    }
    if (state == TempWorkerState.Load)
    {
      BinaryReader reader;
      while (!worker.CancellationPending)
      {
        reader = new BinaryReader(File.Open(GetFullPath(m_tempFilesStack.Peek()), FileMode.Open));
        while (!worker.CancellationPending)
        {
          try
          {
            int dataIndex = BitConverter.ToInt32(reader.ReadBytes(sizeof(int)), 0);
            int dataSize = BitConverter.ToInt32(reader.ReadBytes(sizeof(int)), 0);
            data = new KeyValuePair<int, byte[]>(dataIndex, reader.ReadBytes(dataSize));

            while (!worker.CancellationPending)
            {
              lock (((ICollection)m_tempQueue).SyncRoot)
              {
                if (m_tempQueue.Count < QUEUE_LIMIT)
                {
                  m_tempQueue.Enqueue(data);
                  break;
                }
              }
            }
          }
          catch (IOException) 
          {
            if (!IsBufFrame(data.Key))
              imgFramesFailed++;
          }
          if (reader.BaseStream.Position >= reader.BaseStream.Length)
          {
            reader.Close();
            File.Delete(GetFullPath(m_tempFilesStack.Pop()));
            break;
          }
        }
        reader.Close();
      }
    }
  }

  private void ProcessQueueWork(object sender, DoWorkEventArgs e)
  {
    BackgroundWorker worker = (BackgroundWorker)sender;
    BinaryWriter writer;
    KeyValuePair<int, byte[]> data = new KeyValuePair<int, byte[]>();
    
    while (!worker.CancellationPending)
    {
      try
      {
        lock (((ICollection)m_processQueue).SyncRoot)
        {
          if (m_processQueue.Count == 0)
            continue;
          data = m_processQueue.Dequeue();
        }

        string filename = !IsBufFrame(data.Key) ? (data.Key).ToString() : "." + (-data.Key).ToString();
        filename += (useHighResolution) ? ".png" : ".jpg";
        writer = new BinaryWriter(File.Open(GetFullPath(filename), FileMode.Create));
        writer.Write(data.Value);
        writer.Close();
        framesProcessed++;
        if (!IsBufFrame(data.Key))
          imgFramesPassed++;
      }
      catch (IOException) 
      {
        if (!IsBufFrame(data.Key))
          imgFramesFailed++;
      }
    }
  }

  private void ReadCameraTexture()
  {
    m_currentRenderTexture = RenderTexture.active;
    RenderTexture.active = m_cameraRenderTexture;
    m_cameraTexture2D.ReadPixels(m_cameraRect, 0, 0, false);
    RenderTexture.active = m_currentRenderTexture;
  }

  private void SaveBufTexture()
  {
    if (Time.time > m_targetTime)
    {
      ReadCameraTexture();
      bufFramesCount++;
      lock (((ICollection)m_tempQueue).SyncRoot)
      {
        if (m_tempQueue.Count < QUEUE_LIMIT)
          m_tempQueue.Enqueue(new KeyValuePair<int, byte[]>(-bufFramesCount, m_cameraTexture2D.GetRawTextureData()));
      }
      m_targetTime = m_startCountdownTime + m_targetInterval * (bufFramesCount + 1);
    }
  }

  private void SaveRawTexture()
  {
    duration = Mathf.Max(Time.time - m_startRecordTime, 0.0f);
    if (Time.time > m_targetTime)
    {
      ReadCameraTexture();
      lock (((ICollection)m_tempQueue).SyncRoot)
      {
        if (m_tempQueue.Count < QUEUE_LIMIT)
          m_tempQueue.Enqueue(new KeyValuePair<int, byte[]>(rawFramesCount, m_cameraTexture2D.GetRawTextureData()));
        else
          rawFramesFailed++;
      }
      rawFramesCount++;
      m_targetTime = m_startRecordTime + m_targetInterval * (rawFramesCount + 1);

      if (startingIndicators != null)
        startingIndicators.gameObject.SetActive(false);
    }
  }

  private void ProcessTextures()
  {
    KeyValuePair<int, byte[]> data;
    try
    {
      lock (((ICollection)m_tempQueue).SyncRoot)
      {
        if (m_tempQueue.Count == 0)
          return;
        data = m_tempQueue.Dequeue();
      }
      m_cameraTexture2D.LoadRawTextureData(data.Value);
      while (true)
      {
        lock (((ICollection)m_processQueue).SyncRoot)
        {
          if (m_processQueue.Count < QUEUE_LIMIT)
          {
            if (useHighResolution)
              m_processQueue.Enqueue(new KeyValuePair<int, byte[]>(data.Key, m_cameraTexture2D.EncodeToPNG()));
            else
              m_processQueue.Enqueue(new KeyValuePair<int, byte[]>(data.Key, m_cameraTexture2D.EncodeToJPG()));
            break;
          }
        }
      }
      if (!IsBufFrame(data.Key))
        imgFramesCount++;
    }
    catch (IOException)
    {
      Debug.LogWarning("ProcessTexture: File cannot be read. Adding data back into queue");
    }
  }

  private void InterpolateMissingTextures()
  {
    //string[] files = Directory.GetFiles(directory);
    //for (int i = 0; i < files.Length; ++i)
    //{
    //  Debug.Log(files[i]);
    //}
    //imgFramesCount = rawFramesCount;
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

    if (startingIndicators != null)
      startingIndicators.transform.localPosition = transform.localPosition + new Vector3(0.0f, 0.0f, m_camera.nearClipPlane + 0.001f);

    try
    {
      if (!System.IO.Directory.Exists(directory))
        System.IO.Directory.CreateDirectory(directory);
      m_tempFilesStack.Push("stack" + (m_tempFilesStack.Count + 1) + ".tmp");
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
    m_tempFilesStack = new Stack<string>();
    m_tempQueue = new Queue<KeyValuePair<int, byte[]>>();
    m_processQueue = new Queue<KeyValuePair<int, byte[]>>();

    m_tempWorker = new BackgroundWorker();
    m_tempWorker.DoWork += TempQueueWork;
    m_tempWorker.WorkerSupportsCancellation = true;

    m_processWorker = new BackgroundWorker();
    m_processWorker.DoWork += ProcessQueueWork;
    m_processWorker.WorkerSupportsCancellation = true;
  }

  void OnDestroy()
  {
    SetState(CamRecorderState.Idle);
    m_cameraRenderTexture.Release();
  }

  void Start()
  {
    SetupCamera();
    SetupMultithread();
    SetState(CamRecorderState.Idle);
  }

  void OnPostRender()
  {
    switch (m_state)
    {
      case CamRecorderState.Countdown:
        SaveBufTexture();
        countdownRemaining = Mathf.Max(m_endCountdownTime - Time.time, 0.0f);
        if (countdownRemaining == 0.0f)
          SetState(CamRecorderState.Recording);
        break;
      case CamRecorderState.Recording:
        SaveRawTexture();
        break;
      case CamRecorderState.Processing:
        if (imgFramesCount == rawFramesCount)
        {
          StopProcessing();
        }
        else if (imgFramesPassed != rawFramesPassed)
        {
          ProcessTextures();
        }
        else
        {
          StopProcessing();
          //InterpolateMissingTextures(); 
        }
        break;
      default:
        break;
    }
  }
}