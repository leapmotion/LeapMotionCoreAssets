using UnityEngine;
using System.Collections;

public abstract class ReporterBase : MonoBehaviour {
  protected KeyCode m_safetyKey;
  protected KeyCode m_triggerKey;

  protected enum RecordingState {
    READY,
    RECORDING,
    SAVING,
    REPLAYING
  }
  private RecordingState m_recordingState = RecordingState.READY;

  public void SetKeys(KeyCode safetyKey, KeyCode triggerKey) 
  {
    m_safetyKey = safetyKey;
    m_triggerKey = triggerKey;
  }

  public bool IsReady() { return m_recordingState == RecordingState.READY; }
  public bool IsRecording() { return m_recordingState == RecordingState.RECORDING; }
  public bool IsSaving() { return m_recordingState == RecordingState.SAVING; }
  public bool IsReplaying() { return m_recordingState == RecordingState.REPLAYING; }

  public void TriggerStartRecording() {
    if (!IsReady())
      return;
    if (StartRecording())
      m_recordingState = RecordingState.RECORDING;
  }

  public bool TriggerAbortRecording() {
    if (!IsRecording())
      return false;
    return AbortRecording();
  }

  public void TriggerStartSaving() {
    if (!IsRecording())
      return;
    if (StartSaving())
      m_recordingState = RecordingState.SAVING;
  }

  public bool TriggerAbortSaving() {
    if (!IsSaving())
      return false;
    return AbortSaving();
  }

  public void TriggerStartReplaying() {
    if (!IsSaving())
      return;
    if (StartReplaying())
      m_recordingState = RecordingState.REPLAYING;
  }

  public bool TriggerAbortReplaying() {
    if (!IsReplaying())
      return false;
    return AbortReplaying();
  }

  public void TriggerReset() {
    if (Reset())
      m_recordingState = RecordingState.READY;
  }

  protected virtual bool Reset() { return true; }
  protected abstract bool StartRecording();
  protected abstract bool AbortRecording();
  protected abstract bool StartSaving();
  protected abstract bool AbortSaving();
  protected virtual bool StartReplaying() { return true; }
  protected virtual bool AbortReplaying() { return true; }

  protected virtual void OnEnable() {
    m_recordingState = RecordingState.READY;
  }
}
