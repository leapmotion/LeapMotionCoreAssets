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
  public int imageWidth = 1920;
  public int imageHeight = 1080;
  public Camera optionalSyncCam;
  public bool optionalSyncCamFOV = true;
  public bool optionalSyncCamLayers = true;

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
  [HideInInspector]
  public int currFrameIndex = 0;

  // Objects required to record a camera
  private Camera m_camera;
  private RenderTexture m_cameraRenderTexture;
  private RenderTexture m_currentRenderTexture;
  private Texture2D m_cameraTexture2D;
  private Rect m_cameraRect;
  private int m_layersToIgnore = 0; // Bit-array represented in int32. 1 = Ignore. 0 = Do not ignore
  private string m_fileExtension;

  // Queue and Thread required to optimize camera recorder
  private const int QUEUE_LIMIT = 4;
  private const int TEMP_BYTE_LIMIT = 2000000000; // 2GB because FAT32 limit is 4GB
  private const string COUNTDOWN_PREFIX = ".";
  private List<int> m_framesDroppedList;
  private Stack<string> m_tempFilesStack; // We'll write to most recent temp file
  private BackgroundWorker m_logWorker;
  private BackgroundWorker m_tempWorker; // Responsible for save/load temp (raw/buf) data for processing
  private BackgroundWorker m_processWorker; // Responsible for saving processed files
  private Queue<string> m_logQueue;
  private Queue<KeyValuePair<int, byte[]>> m_tempQueue; // We'll process oldest temp (raw/buf) data
  private Queue<KeyValuePair<int, byte[]>> m_processQueue; // We'll process oldest img data
  private enum TempWorkerState
  {
    Save,
    Load
  }

  // Time objects used for countdown and maintaining frames-per-second
  private float m_startCountdownTime = 0.0f;
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
        LogComment("CamRecorderState.Idle");
        m_camera.enabled = false;
        ClearAndStopAll();
        break;
      case CamRecorderState.Countdown:
        m_camera.enabled = true;
        PrepareCamRecorder();
        LogComment("CamRecorderState.Countdown");
        countdownRemaining = Mathf.Max(countdownRemaining, 0.0f);
        m_startCountdownTime = Time.time;
        m_targetTime = m_startCountdownTime + m_targetInterval;
        m_startRecordTime = m_startCountdownTime + countdownRemaining;
        currFrameIndex = -1; // Countdown Frames have negative frame index
        StartWorker(m_tempWorker, m_tempQueue, TempWorkerState.Save);
        break;
      case CamRecorderState.Recording:
        LogComment("CamRecorderState.Recording");
        m_targetTime = m_startRecordTime + m_targetInterval;
        currFrameIndex = 0; // Expect Frames have positive frame index
        break;
      case CamRecorderState.Processing:
        LogComment("CamRecorderState.Processing");
        StopWorker(m_tempWorker);
        StartWorker(m_tempWorker, m_tempQueue, TempWorkerState.Load);
        StartWorker(m_processWorker, m_processQueue);
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
    LogComment("StartRecording Triggered");
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
    LogComment("StopRecording Triggered");
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
    LogComment("StopProcessing Triggered");
    if (m_state == CamRecorderState.Processing)
    {
      LogComment("Frames recorded after countdown: " + framesSucceeded.ToString());
      LogComment("Frames recorded during countdown: " + framesCountdown.ToString());
      LogComment("Frames dropped: " + framesDropped.ToString());
      LogComment("Frames actually recorded: " + framesActual.ToString());
      LogComment("Frames expected to record: " + framesExpect.ToString());
      SetState(CamRecorderState.Idle);
    }
  }

  public bool IsIdling() { return (m_state == CamRecorderState.Idle); }
  public bool IsCountingDown() { return (m_state == CamRecorderState.Countdown); }
  public bool IsRecording() { return (m_state == CamRecorderState.Recording); }
  public bool IsProcessing() { return (m_state == CamRecorderState.Processing); }

  public void AddLayersToIgnore(int layer) { m_layersToIgnore |= (1 << layer); }
  public void RemoveLayersToIgnore(int layer) { m_layersToIgnore &= ~(1 << layer); }
  public void ResetLayersToIgnore() { m_layersToIgnore = 0; }

  public void SetCountdown(float seconds) { countdownRemaining = seconds; }

  private string GetFullPath(string filename) { return directory + "/" + filename; }
  private string GetDataPath(int index)
  {
    string filename = (index >= 0) ? (index).ToString() : COUNTDOWN_PREFIX + (-index).ToString();
    return GetFullPath(filename + m_fileExtension);
  }

  private void LogComment(string log) { QueueEnqueue(m_logQueue, GetCurrentTime() + " : " + log); }
  private void LogError(string log) { QueueEnqueue(m_logQueue, GetCurrentTime() + " [ERR] : " + log); }
  
  private void DropFrame(int frameIndex)
  {
    LogError("Frame#" + frameIndex.ToString() + " dropped");
    framesDropped++;
    m_framesDroppedList.Add(frameIndex);
  }

  private void StopWorker(BackgroundWorker worker)
  {
    worker.CancelAsync();
    while (worker.IsBusy) ;
  }

  private void StartWorker<T>(BackgroundWorker worker, Queue<T> queue, object argument = null)
  {
    queue.Clear();
    worker.RunWorkerAsync(argument);
  }

  private bool QueueIsEmpty<T>(Queue<T> queue)
  {
    lock (((ICollection)queue).SyncRoot)
    {
      return (queue.Count == 0);
    }
  }

  private bool QueueEnqueue<T>(Queue<T> queue, T data)
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

  private bool QueueDequeue<T>(Queue<T> queue, out T data)
  {
    lock (((ICollection)queue).SyncRoot)
    {
      if (queue.Count > 0) 
      {
        data = queue.Dequeue();
        return true;
      } 
      else 
      {
        data = default(T);
        return false;
      }
    }
  }

  private string GetCurrentTime() { return DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss.fff"); }
  private void LogQueueWork(object sender, DoWorkEventArgs e)
  {
    BackgroundWorker worker = (BackgroundWorker)sender;
    StreamWriter writer = new StreamWriter(GetFullPath("log.txt"));
    string log = "";
    while (!worker.CancellationPending)
    {
      try
      {
        if (!QueueDequeue(m_logQueue, out log))
          continue;
        writer.WriteLine(log);
      }
      catch { }
    }
    writer.Close();
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
            if (!QueueDequeue(m_tempQueue, out data))
              continue;

            writer.Write(BitConverter.GetBytes(data.Key)); // Data Index
            writer.Write(BitConverter.GetBytes(data.Value.Length)); // Data Length
            writer.Write(data.Value); // Data (Byte array)
          }
          catch (IOException)
          {
            DropFrame(data.Key);
          }
          if (writer.BaseStream.Length > TEMP_BYTE_LIMIT)
          {
            // Create a new tmep file to save to because it reached max file size
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
            DropFrame(data.Key);
          }
          if (reader.BaseStream.Position >= reader.BaseStream.Length)
          {
            reader.Close();
            File.Delete(GetFullPath(m_tempFilesStack.Pop()));
            break;
          }
        }
        reader.Close();
        if (m_tempFilesStack.Count == 0) // No more temp files to load
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
        if (!QueueDequeue(m_processQueue, out data))
          continue;

        writer = new BinaryWriter(File.Open(GetDataPath(data.Key), FileMode.Create));
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
        DropFrame(data.Key);
      }
    }
  }

  private void SaveCameraTexture(int frameIndex)
  {
    framesExpect++;
    m_currentRenderTexture = RenderTexture.active;
    RenderTexture.active = m_cameraRenderTexture;
    m_cameraTexture2D.ReadPixels(m_cameraRect, 0, 0, false);
    RenderTexture.active = m_currentRenderTexture;
    if (!QueueEnqueue(m_tempQueue, new KeyValuePair<int, byte[]>(frameIndex, m_cameraTexture2D.GetRawTextureData())))
      DropFrame(frameIndex);
  }

  private void SaveBufTexture()
  {
    if (Time.time > m_targetTime)
    {
      SaveCameraTexture(currFrameIndex);
      currFrameIndex--;
      m_targetTime = m_startCountdownTime + m_targetInterval * (Mathf.Abs(currFrameIndex) + 1);
    }
  }

  private void SaveRawTexture()
  {
    duration = Mathf.Max(Time.time - m_startRecordTime, 0.0f);
    if (Time.time > m_targetTime)
    {
      SaveCameraTexture(currFrameIndex);
      currFrameIndex++;
      m_targetTime = m_startRecordTime + m_targetInterval * (Mathf.Abs(currFrameIndex) + 1);
    }
  }

  private void ProcessTextures()
  {
    KeyValuePair<int, byte[]> data = new KeyValuePair<int, byte[]>();
    try
    {
      // 1. If tempWorker stopped and tempQueue is empty, it means no more raw data to load
      // 2. if processQueue is empty, it means processWorker will have no more data to save
      // If both conditions are met, stop process worker
      if (!m_tempWorker.IsBusy && QueueIsEmpty(m_tempQueue) && QueueIsEmpty(m_processQueue))
      {
        StopWorker(m_processWorker);
        return;
      }

      if (!QueueDequeue(m_tempQueue, out data))
        return;
      
      m_cameraTexture2D.LoadRawTextureData(data.Value);
      if (useHighResolution)
        data = new KeyValuePair<int, byte[]>(data.Key, m_cameraTexture2D.EncodeToPNG());
      else
        data = new KeyValuePair<int, byte[]>(data.Key, m_cameraTexture2D.EncodeToJPG());
      while (!QueueEnqueue(m_processQueue, data)) ;
    }
    catch (IOException)
    {
      LogError("ProcessTexture: File cannot be read. Adding data back into queue");
      while (!QueueEnqueue(m_tempQueue, data)) ;
    }
  }

  private void ReplaceFrame(int currIndex, int replaceIndex)
  {
    try
    {
      File.Copy(GetDataPath(replaceIndex), GetDataPath(currIndex));
      LogComment("Detected Frame#" + currIndex.ToString() + " dropped. Replaced with Frame#" + replaceIndex.ToString());
      framesActual++;
    }
    catch (IOException)
    {
      LogError("Failed to copy to fill in for missing texture " + currIndex);
    }
  }

  private void ProcessMissingTextures()
  {
    int[] framesDroppedArray = m_framesDroppedList.ToArray();
    Array.Sort(framesDroppedArray);

    // Initialize the index for countdown and expected frames
    int expectedFrameIndex = 0;
    for (int i = 0; i < framesDroppedArray.Length; ++i)
    {
      if (framesDroppedArray[i] > 0)
      {
        expectedFrameIndex = i;
        break;
      }
    }

    // Fill in countdown frames
    for (int i = expectedFrameIndex - 1; i >= 0; --i)
    {
      int frameIndex = framesDroppedArray[i];
      ReplaceFrame(frameIndex, frameIndex + 1);
    }
    
    // Fill in expected frames
    for (int i = expectedFrameIndex; i < framesDroppedArray.Length; ++i)
    {
      int frameIndex = framesDroppedArray[i];
      ReplaceFrame(frameIndex, frameIndex - 1);
    }
  }

  private void PrepareCamRecorder()
  {
    if (optionalSyncCam != null)
    {
      if (optionalSyncCamFOV)
      {
        m_camera.fieldOfView = optionalSyncCam.fieldOfView;
      }
      if (optionalSyncCamLayers)
      {
        m_camera.clearFlags = optionalSyncCam.clearFlags;
        m_camera.backgroundColor = optionalSyncCam.backgroundColor;
        m_camera.cullingMask = optionalSyncCam.cullingMask;
      }
    }
    m_camera.cullingMask &= ~(m_layersToIgnore);

    framesExpect = 0;
    framesActual = 0;
    framesDropped = 0;
    framesCountdown = 0;
    framesSucceeded = 0;
    m_fileExtension = (useHighResolution) ? ".png" : ".jpg";

    try
    {
      if (!System.IO.Directory.Exists(directory))
        System.IO.Directory.CreateDirectory(directory);
      m_tempFilesStack.Push("stack" + (m_tempFilesStack.Count + 1) + ".tmp");
    }
    catch (IOException)
    {
      LogError("Unable to create directory: " + directory + ". Recording aborted");
      SetState(CamRecorderState.Idle);
      return;
    }

    StartWorker(m_logWorker, m_logQueue);
    LogComment("Frame Rate: " + frameRate.ToString());
    LogComment("High Resolution: " + useHighResolution.ToString());
  }

  private void SetupCamera()
  {
    m_camera = GetComponent<Camera>();
    if (frameRate <= 0)
      frameRate = 30;
    m_targetInterval = 1.0f / (float)frameRate;

    m_cameraRenderTexture = new RenderTexture(imageWidth, imageHeight, 24);
    m_cameraTexture2D = new Texture2D(imageWidth, imageHeight, TextureFormat.RGB24, false);
    m_cameraRect = new Rect(0, 0, imageWidth, imageHeight);
    m_camera.targetTexture = m_cameraRenderTexture;
    directory = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
    SetCountdown(0.0f);
    m_camera.enabled = false;
  }

  private void SetupMultithread()
  {
    m_framesDroppedList = new List<int>();
    m_tempFilesStack = new Stack<string>();
    m_logQueue = new Queue<string>();
    m_tempQueue = new Queue<KeyValuePair<int, byte[]>>();
    m_processQueue = new Queue<KeyValuePair<int, byte[]>>();

    m_tempWorker = new BackgroundWorker();
    m_tempWorker.DoWork += TempQueueWork;
    m_tempWorker.WorkerSupportsCancellation = true;

    m_processWorker = new BackgroundWorker();
    m_processWorker.DoWork += ProcessQueueWork;
    m_processWorker.WorkerSupportsCancellation = true;

    m_logWorker = new BackgroundWorker();
    m_logWorker.DoWork += LogQueueWork;
    m_logWorker.WorkerSupportsCancellation = true;
  }

  private void ClearAndStopAll()
  {
    StopWorker(m_logWorker);
    StopWorker(m_tempWorker);
    StopWorker(m_processWorker);
    m_framesDroppedList.Clear();
    while (m_tempFilesStack.Count > 0)
    {
      File.Delete(GetFullPath(m_tempFilesStack.Pop()));
    }
    m_tempFilesStack.Clear();
    if (
      System.IO.Directory.Exists(directory) &&
      Directory.GetFiles(directory).Length == 0
      )
    {
      Directory.Delete(directory);
    }
  }

  void OnDestroy()
  {
    ClearAndStopAll();
    m_cameraRenderTexture.Release();
  }

  void Awake()
  {
    SetupCamera();
    SetupMultithread();
  }

  void OnPostRender()
  {
    switch (m_state)
    {
      case CamRecorderState.Countdown:
        countdownRemaining = Mathf.Max(m_startRecordTime - Time.time, 0.0f);
        if (countdownRemaining == 0.0f)
          SetState(CamRecorderState.Recording);
        else
          SaveBufTexture();
        break;
      case CamRecorderState.Recording:
        SaveRawTexture();
        break;
      case CamRecorderState.Processing:
        if (!m_processWorker.IsBusy)
        {
          ProcessMissingTextures();
          StopProcessing();
        }
        else
        {
          ProcessTextures();
        }
        break;
      default:
        break;
    }
  }
}