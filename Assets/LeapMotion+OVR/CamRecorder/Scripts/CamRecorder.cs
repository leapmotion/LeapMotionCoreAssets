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
  public int frameRate = 30;
  public GameObject startingIndicators;
  public Camera syncCamera;

  [HideInInspector]
  public float duration = 0.0f;
  [HideInInspector]
  public string directory = "";
  [HideInInspector]
  public int framesExpect = 0; // Number of all frames expected to record
  [HideInInspector]
  public int framesActual = 0; // Number of all frames actually recorded
  [HideInInspector]
  public int framesDropped = 0; // Number of frames not recorded after countdown but expected
  [HideInInspector]
  public int framesCountdown = 0; // Number of frames recorded during countdown
  [HideInInspector]
  public int framesSucceeded = 0; // Number of frames recorded after countdown
  [HideInInspector]
  public float countdownRemaining = 0.0f;
  [HideInInspector]
  public bool useHighResolution = false;
  private int m_frameIndex = 0;

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
        framesExpect = 0;
        framesActual = 0;
        framesDropped = 0;
        m_frameIndex = -1; // Countdown Frames have negative frame index
        m_tempWorker.RunWorkerAsync(TempWorkerState.Save);
        break;
      case CamRecorderState.Recording:
        m_startRecordTime = Time.time;
        m_targetTime = Time.time + m_targetInterval;
        m_frameIndex = 0;
        if (startingIndicators != null)
          startingIndicators.gameObject.SetActive(true); // Enable indicators for first frame
        break;
      case CamRecorderState.Processing:
        framesCountdown = 0;
        framesSucceeded = 0;
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

  private string GetFullPath(string filename) { return directory + "/" + filename; }

  private void StopWorker(BackgroundWorker worker)
  {
    worker.CancelAsync();
    while (worker.IsBusy) ;
  }

  private bool QueueIsEmpty(Queue<KeyValuePair<int, byte[]>> queue)
  {
    lock (((ICollection)queue).SyncRoot)
    {
      return (queue.Count == 0);
    }
  }

  private bool QueueEnqueue(Queue<KeyValuePair<int, byte[]>> queue, KeyValuePair<int, byte[]> data)
  {
    lock (((ICollection)queue).SyncRoot)
    {
      if (queue.Count < QUEUE_LIMIT)
      {
        queue.Enqueue(data);
        return true;
      }
      else
      {
        return false;
      }
    }
  }

  private KeyValuePair<int, byte[]> QueueDequeue(Queue<KeyValuePair<int, byte[]>> queue)
  {
    lock (((ICollection)queue).SyncRoot)
    {
      if (queue.Count > 0)
        return queue.Dequeue();
      else
        return new KeyValuePair<int, byte[]>();
    }
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
            if (QueueIsEmpty(m_tempQueue))
              continue;
            data = QueueDequeue(m_tempQueue);

            writer.Write(BitConverter.GetBytes(data.Key)); // Data Index
            writer.Write(BitConverter.GetBytes(data.Value.Length)); // Data Length
            writer.Write(data.Value); // Data (Byte array)
          }
          catch (IOException)
          {
            framesDropped++;
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

            while (!worker.CancellationPending && !QueueEnqueue(m_tempQueue, data)) ;
          }
          catch (IOException) 
          {
            framesDropped++;
          }
          if (reader.BaseStream.Position >= reader.BaseStream.Length)
          {
            reader.Close();
            File.Delete(GetFullPath(m_tempFilesStack.Pop()));
            break;
          }
        }
        reader.Close();
        if (m_tempFilesStack.Count == 0)
          break;
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
        if (QueueIsEmpty(m_processQueue))
          continue;
        data = QueueDequeue(m_processQueue);
        string filename = (data.Key >= 0) ? (data.Key).ToString() : "." + (-data.Key).ToString();
        filename += (useHighResolution) ? ".png" : ".jpg";
        writer = new BinaryWriter(File.Open(GetFullPath(filename), FileMode.Create));
        writer.Write(data.Value);
        writer.Close();
        framesActual++;
        if (data.Key > 0)
          framesSucceeded++;
        else
          framesCountdown++;
      }
      catch (IOException) 
      {
        framesDropped++;
      }
    }
  }

  private void SaveCameraTexture(int frameIndex)
  {
    m_currentRenderTexture = RenderTexture.active;
    RenderTexture.active = m_cameraRenderTexture;
    m_cameraTexture2D.ReadPixels(m_cameraRect, 0, 0, false);
    RenderTexture.active = m_currentRenderTexture;
    KeyValuePair<int, byte[]> data = new KeyValuePair<int, byte[]>(frameIndex, m_cameraTexture2D.GetRawTextureData());
    framesExpect++;
    if (!QueueEnqueue(m_tempQueue, data))
      framesDropped++;
  }

  private void SaveBufTexture()
  {
    if (Time.time > m_targetTime)
    {
      SaveCameraTexture(m_frameIndex);
      m_frameIndex--;
      m_targetTime = m_startCountdownTime + m_targetInterval * (Mathf.Abs(m_frameIndex) + 1);
    }
  }

  private void SaveRawTexture()
  {
    duration = Mathf.Max(Time.time - m_startRecordTime, 0.0f);
    if (Time.time > m_targetTime)
    {
      if (m_frameIndex == 0)
        startingIndicators.gameObject.SetActive(false);

      SaveCameraTexture(m_frameIndex);
      m_frameIndex++;
      m_targetTime = m_startRecordTime + m_targetInterval * (Mathf.Abs(m_frameIndex) + 1);
    }
  }

  private void ProcessTextures()
  {
    KeyValuePair<int, byte[]> data = new KeyValuePair<int, byte[]>();
    try
    {
      if (QueueIsEmpty(m_tempQueue))
        return;
      data = QueueDequeue(m_tempQueue);
      m_cameraTexture2D.LoadRawTextureData(data.Value);
      if (useHighResolution)
        data = new KeyValuePair<int, byte[]>(data.Key, m_cameraTexture2D.EncodeToPNG());
      else
        data = new KeyValuePair<int, byte[]>(data.Key, m_cameraTexture2D.EncodeToJPG());
      while (!QueueEnqueue(m_processQueue, data)) ;
    }
    catch (IOException)
    {
      Debug.LogWarning("ProcessTexture: File cannot be read. Adding data back into queue");
      QueueEnqueue(m_tempQueue, data);
    }
  }

  private void ProcessMissingTextures()
  {
    string[] files = Directory.GetFiles(directory);
    for (int i = 0; i < files.Length; ++i)
    {
      Debug.Log(Path.GetFileNameWithoutExtension(files[i]));
    }
    framesActual = framesExpect;
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
        if (framesActual == framesExpect)
        {
          StopProcessing();
        }
        else if (m_tempWorker.IsBusy || !QueueIsEmpty(m_tempQueue) || !QueueIsEmpty(m_processQueue))
        {
          ProcessTextures();
        }
        else
        {
          ProcessMissingTextures();
        }
        break;
      default:
        break;
    }
  }
}