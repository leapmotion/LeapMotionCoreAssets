using UnityEngine;
using System.Collections;

public abstract class ReporterBase : MonoBehaviour {
  protected enum RecordingState {
    READY,
    RECORDING,
    SAVING,
    REPLAYING
  }
  protected RecordingState m_recordingState = RecordingState.READY;

  protected KeyCode m_safetyKey;
  protected KeyCode m_triggerKey;

  public void SetKeys(KeyCode safetyKey, KeyCode triggerKey) 
  {
    m_safetyKey = safetyKey;
    m_triggerKey = triggerKey;
  }

  public bool IsReady() { return m_recordingState == RecordingState.READY; }
  public bool IsRecording() { return m_recordingState == RecordingState.RECORDING; }
  public bool IsSaving() { return m_recordingState == RecordingState.SAVING; }
  public bool IsReplaying() { return m_recordingState == RecordingState.REPLAYING; }

  public abstract bool StartRecording();
  public abstract bool AbortRecording();
  public virtual bool StartSaving() { return true; }
  public virtual bool AbortSaving() { return true; }
  public virtual bool StartReplaying() { return true; }
  public virtual bool AbortReplaying() { return true; }

  protected virtual void OnEnable() {
    m_recordingState = RecordingState.READY;
  }
}
