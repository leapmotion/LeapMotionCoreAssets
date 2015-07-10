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
  public Camera optionalSyncCamera;

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
  private int m_layersToIgnore; // Bit-array represented in int32. 1 = Ignore. 0 = Do not ignore
  private int m_originalCullingMask;
  private string m_fileExtension;

  // Queue and Thread required to optimize camera recorder
  private const int QUEUE_LIMIT = 4;
  private const int TEMP_BYTE_LIMIT = 2000000000; // 2GB because FAT32 limit is 4GB
  private const string COUNTDOWN_PREFIX = ".";
  private const string CORRUPTED_SUFFIX = "-";
  private List<int> m_framesDroppedList;
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
        m_camera.enabled = false;
        ClearAll();
        break;
      case CamRecorderState.Countdown:
        m_camera.enabled = true;
        PrepareCamRecorder();
        countdownRemaining = Mathf.Max(countdownRemaining, 0.0f);
        m_startCountdownTime = Time.time;
        m_targetTime = m_startCountdownTime + m_targetInterval;
        m_startRecordTime = m_startCountdownTime + countdownRemaining;
        currFrameIndex = -1; // Countdown Frames have negative frame index
        m_tempQueue.Clear(); // Prepare to fill it with save raw data
        m_tempWorker.RunWorkerAsync(TempWorkerState.Save);
        break;
      case CamRecorderState.Recording:
        m_camera.cullingMask = m_originalCullingMask & ~(m_layersToIgnore);
        m_targetTime = m_startRecordTime + m_targetInterval;
        currFrameIndex = 0; // Expect Frames have positive frame index
        break;
      case CamRecorderState.Processing:
        m_camera.cullingMask = m_originalCullingMask;
        StopWorker(m_tempWorker);
        m_tempQueue.Clear(); // Prepare to fill it with load raw data
        m_tempWorker.RunWorkerAsync(TempWorkerState.Load);
        m_processQueue.Clear(); // Prepare to fill it with save image data
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

  private string GetDataPath(int index, bool isCorrupt = false)
  {
    string filename = (index >= 0) ? (index).ToString() : COUNTDOWN_PREFIX + (-index).ToString();
    if (isCorrupt)
      filename += CORRUPTED_SUFFIX;
    if (index >= 0)
      return GetFullPath(filename + m_fileExtension);
    else
      return GetFullPath(filename + m_fileExtension);
  }
  private string GetFullPath(string filename) { return directory + "/" + filename; }
  private void DropFrame(int frameIndex)
  {
    framesDropped++;
    m_framesDroppedList.Add(frameIndex);
  }

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

  /// <summary>
  /// Returns false if queue count is already over the limit
  /// </summary>
  /// <param name="queue"></param>
  /// <param name="data"></param>
  /// <returns></returns>
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

  /// <summary>
  /// Returns false if Queue is empty
  /// </summary>
  /// <param name="queue"></param>
  /// <param name="data"></param>
  /// <returns></returns>
  private bool QueueDequeue(Queue<KeyValuePair<int, byte[]>> queue, out KeyValuePair<int, byte[]> data)
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
        data = new KeyValuePair<int, byte[]>();
        return false;
      }
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
    BinaryWriter GetFullPath;
    KeyValuePair<int, byte[]> data = new KeyValuePair<int, byte[]>();
    while (!worker.CancellationPending)
    {
      try
      {
        if (!QueueDequeue(m_processQueue, out data))
          continue;

        GetFullPath = new BinaryWriter(File.Open(GetDataPath(data.Key), FileMode.Create));
        GetFullPath.Write(data.Value);
        GetFullPath.Close();
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
    m_currentRenderTexture = RenderTexture.active;
    RenderTexture.active = m_cameraRenderTexture;
    m_cameraTexture2D.ReadPixels(m_cameraRect, 0, 0, false);
    RenderTexture.active = m_currentRenderTexture;
    KeyValuePair<int, byte[]> data = new KeyValuePair<int, byte[]>(frameIndex, m_cameraTexture2D.GetRawTextureData());
    framesExpect++;
    if (!QueueEnqueue(m_tempQueue, data))
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
      Debug.LogError("ProcessTexture: File cannot be read. Adding data back into queue");
      while (!QueueEnqueue(m_tempQueue, data)) ;
    }
  }

  private void ProcessMissingTextures()
  {
    int[] framesDroppedArray = m_framesDroppedList.ToArray();
    Array.Sort(framesDroppedArray);

    // Initialize the index for countdown and expected frames
    int expectedFrameIndex = 0;
    int prevFrameIndex = 0;
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
      int index = framesDroppedArray[i];
      try
      {
        if (prevFrameIndex == index + 1)
          File.Copy(GetDataPath(index + 1, true), GetDataPath(index, true));
        else
          File.Copy(GetDataPath(index + 1), GetDataPath(index, true));
      }
      catch (IOException)
      {
        Debug.LogError("Failed to copy to fill in for missing texture " + index);
      }
      prevFrameIndex = index;
    }
    
    // Fill in expected frames
    for (int i = expectedFrameIndex; i < framesDroppedArray.Length; ++i)
    {
      int index = framesDroppedArray[i];
      try
      {
        if (prevFrameIndex == index - 1)
          File.Copy(GetDataPath(index - 1, true), GetDataPath(index, true));
        else
          File.Copy(GetDataPath(index - 1), GetDataPath(index, true));
      }
      catch (IOException)
      {
        Debug.LogError("Failed to copy to fill in for missing texture " + index);
      }
      prevFrameIndex = index;
    }
  }

  private void PrepareCamRecorder()
  {
    if (optionalSyncCamera != null)
    {
      m_camera.fieldOfView = optionalSyncCamera.fieldOfView;
      m_camera.rect = optionalSyncCamera.rect;
      m_originalCullingMask = optionalSyncCamera.cullingMask;
    }
    else
    {
      m_originalCullingMask = m_camera.cullingMask;
    }

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
      Debug.LogError("Unable to create directory: " + directory);
    }
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
    ResetLayerToIgnore();
    m_camera.enabled = false;
  }

  private void SetupMultithread()
  {
    m_framesDroppedList = new List<int>();
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

  private void ClearAll()
  {
    StopWorker(m_tempWorker);
    StopWorker(m_processWorker);
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
    m_tempQueue.Clear();
    m_processQueue.Clear();
    m_framesDroppedList.Clear();
  }

  void OnDestroy()
  {
    ClearAll();
    m_cameraRenderTexture.Release();
  }

  void Start()
  {
    SetupCamera();
    SetupMultithread();
    ClearAll();
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
          // Stop processing if process worker stopped working
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