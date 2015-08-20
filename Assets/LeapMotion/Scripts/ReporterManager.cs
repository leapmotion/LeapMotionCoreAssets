using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ReporterManager : MonoBehaviour {
  public bool enableOnStart = false;
  public KeyCode safetyKey = KeyCode.LeftShift;
  public KeyCode triggerKey = KeyCode.Tab;

  protected enum RecordingState {
    INACTIVE,
    READY,
    RECORDING,
    SAVING,
    REPLAYING
  };
  protected RecordingState m_managerState = RecordingState.INACTIVE;

  private List<ReporterBase> m_reporters;

  private bool OnTrigger() {
    return Input.GetKeyDown(triggerKey);
  }

  private bool OnSafetyTrigger() {
    return (Input.GetKeyDown(triggerKey) && Input.GetKey(safetyKey));
  }

  void Start() {
    m_reporters = new List<ReporterBase>();
    ReporterBase[] reporters = GetComponentsInChildren<ReporterBase>();
    // Acquire all enabled ReporterBase in children
    foreach (ReporterBase reporter in reporters) {
      if (reporter.gameObject.activeSelf) {
        m_reporters.Add(reporter);
      }
    }
    // If it's not enabled on start, set all to inactive
    if (!enableOnStart) {
      foreach (ReporterBase reporter in m_reporters) {
        reporter.gameObject.SetActive(false);
      }
    } else {
      m_managerState = RecordingState.READY;
    }
  }

  void Update() {
    switch (m_managerState) {
      case RecordingState.INACTIVE:
        if (OnSafetyTrigger()) {
          foreach (ReporterBase reporter in m_reporters) {
            reporter.gameObject.SetActive(true);
          }
          m_managerState = RecordingState.READY;
        }
        break;
      case RecordingState.READY:
        if (OnSafetyTrigger()) {
          bool success = true;
          foreach (ReporterBase reporter in m_reporters) {
            if (reporter.IsReady()) {
              if (!reporter.StartRecording())
                success = false;
            }
          }
          if (success)
            m_managerState = RecordingState.RECORDING;
        }
        break;
      case RecordingState.RECORDING:
        if (OnTrigger()) {
          bool success = true;
          foreach (ReporterBase reporter in m_reporters) {
            if (reporter.IsRecording()) {
              if (!reporter.AbortRecording() || !reporter.StartSaving())
                success = false;
            }
          }
          if (success)
            m_managerState = RecordingState.SAVING;
        }
        break;
      case RecordingState.SAVING:
        if (OnTrigger()) {
          bool success = true;
          foreach (ReporterBase reporter in m_reporters) {
            if (reporter.IsSaving()) {
              if (!reporter.AbortSaving() || !reporter.StartReplaying())
                success = false;
            }
          }
          if (success)
            m_managerState = RecordingState.REPLAYING;
        }
        break;
      case RecordingState.REPLAYING:
        if (OnTrigger()) {
          bool success = true;
          foreach (ReporterBase reporter in m_reporters) {
            if (reporter.IsReplaying())
              if (!reporter.AbortReplaying())
                success = false;
          }
          if (success)
            m_managerState = RecordingState.READY;
        }
        break;
      default:
        break;
    }
  }
}
