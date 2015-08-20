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
            reporter.SetKeys(safetyKey, triggerKey);
            reporter.TriggerReset();
          }
        }
        break;
      case RecordingState.READY:
        if (OnSafetyTrigger()) {
          foreach (ReporterBase reporter in m_reporters) {
            reporter.TriggerStartRecording();
          }
        }
        break;
      case RecordingState.RECORDING:
        if (OnTrigger()) {
          foreach (ReporterBase reporter in m_reporters) {
            if (reporter.TriggerAbortRecording())
              reporter.TriggerStartSaving();
          }
        }
        break;
      case RecordingState.SAVING:
        if (OnTrigger()) {
          foreach (ReporterBase reporter in m_reporters) {
            if (reporter.TriggerAbortSaving())
              reporter.TriggerStartReplaying();
          }
        }
        break;
      case RecordingState.REPLAYING:
        if (OnTrigger()) {
          foreach (ReporterBase reporter in m_reporters) {
            if (reporter.TriggerAbortReplaying())
              reporter.TriggerReset();
          }
        }
        break;
      default:
        break;
    }

    // Propagate to the next state only if all reporters are no longer same with manager state. This allows manager the wait for last reporter
    bool changeState = true;
    switch (m_managerState) {
      case RecordingState.INACTIVE:
        foreach (ReporterBase reporter in m_reporters) {
          if (!reporter.gameObject.activeSelf)
            changeState = false;
        }
        if (changeState) m_managerState = RecordingState.READY;
        break;
      case RecordingState.READY:
        foreach (ReporterBase reporter in m_reporters) {
          if (reporter.IsReady())
            changeState = false;
        }
        if (changeState) m_managerState = RecordingState.RECORDING;
        break;
      case RecordingState.RECORDING:
        foreach (ReporterBase reporter in m_reporters) {
          if (reporter.IsRecording())
            changeState = false;
        }
        if (changeState) m_managerState = RecordingState.SAVING;
        break;
      case RecordingState.SAVING:
        foreach (ReporterBase reporter in m_reporters) {
          if (reporter.IsSaving())
            changeState = false;
        }
        if (changeState) m_managerState = RecordingState.REPLAYING;
        break;
      case RecordingState.REPLAYING:
        foreach (ReporterBase reporter in m_reporters) {
          if (reporter.IsReplaying())
            changeState = false;
        }
        if (changeState) m_managerState = RecordingState.READY;
        break;
      default:
        break;
    }
  }
}
