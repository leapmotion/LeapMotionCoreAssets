using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using Leap;

using UImage = UnityEngine.UI.Image;

public class BugReporter : MonoBehaviour {
  public bool m_interfaceEnabled = false;
  public KeyCode unlockStart = KeyCode.LeftShift;
  public KeyCode changeState = KeyCode.Tab;

  public HandController handController;
  public UImage progressStatus;
  public UImage progressBar;
  public Text progressText;
  public Text instructionText;
  public Text savedpathsText;
  public CamRecorderInterface synchronizeRecorder;
  public bool saveReplayFrames = false;

  protected Color instructionColor = new Color(1.0f, 0.5f, 0.0f);

  protected Controller leap_controller_;

  protected float prev_bug_report_progress_;
  protected bool prev_bug_report_state_;

  protected bool saving_triggered_ = false;

  protected string replayPath;

  protected enum BugReportState
  {
    READY,
    RECORDING,
    SAVING,
    REPLAYING,
  };

  protected BugReportState bug_report_state_ = BugReportState.READY;

  public bool InterfaceEnabled {
    get {
      return m_interfaceEnabled;
    }
    set {
      progressStatus.gameObject.SetActive(value);
      progressBar.gameObject.SetActive(value);
      progressText.gameObject.SetActive(value);
      instructionText.gameObject.SetActive(value);
      if (savedpathsText != null) {
        savedpathsText.gameObject.SetActive(value);
      }
    }
  }

  protected void SetProgressText(string text, Color color)
  {
    if (progressText == null)
      return;

    progressText.text = text;
    progressText.color = color;
  }

  protected void SetInstructionText(string text, Color color)
  {
    if (instructionText == null)
      return;

    instructionText.text = text;
    instructionText.color = color;
  }

  protected void SetSavedPathsText(string text)
  {
    if (savedpathsText == null)
      return;

    savedpathsText.text = text;
  }

  private void HandleKeyInputs()
  {
    if (bug_report_state_ == BugReportState.READY) {
      if ((unlockStart == KeyCode.None || Input.GetKey (unlockStart)) &&
          Input.GetKeyDown (changeState)) {
        InterfaceEnabled = true;
      } 
    }

    if ((unlockStart == KeyCode.None || Input.GetKey(unlockStart) || bug_report_state_ != BugReportState.READY) &&
        Input.GetKeyDown(changeState))
    {
      switch (bug_report_state_)
      {
        case BugReportState.READY:
          RecordingTriggered();
          break;
        case BugReportState.RECORDING:
          leap_controller_.BugReport.EndRecording();
          SavingTriggered();
          break;
        case BugReportState.SAVING:
          ReplayTriggered();
          break;
        case BugReportState.REPLAYING:
          ReadyTriggered();
          break;
        default:
          break;
      }
    }
  }
  
  private void ReadyTriggered()
  {
    handController.ResetRecording();
    handController.StopRecording();
    if (synchronizeRecorder != null &&
        synchronizeRecorder.camRecorder != null) {
      synchronizeRecorder.m_hideInstructions = false;
      synchronizeRecorder.InterfaceEnabled = synchronizeRecorder.m_interfaceEnabled;
      synchronizeRecorder.showFrameTimeStamp = synchronizeRecorder.m_enableFrameTimeStamp;
      synchronizeRecorder.camRecorder.StopRecording();
    }
    InterfaceEnabled = m_interfaceEnabled;
    progressStatus.fillAmount = 1.0f;
    SetProgressText("READY", Color.green);
    SetInstructionText("PRESS '" + unlockStart + "' + '" + changeState + "' TO START RECORDING", instructionColor);
    SetSavedPathsText ("");
    bug_report_state_ = BugReportState.READY;
  }

  private void RecordingTriggered()
  {
    saving_triggered_ = false;
    leap_controller_.BugReport.BeginRecording();
    handController.ResetRecording();
    handController.Record();
    if (synchronizeRecorder != null &&
        synchronizeRecorder.camRecorder != null) {
      synchronizeRecorder.m_hideInstructions = true;
      synchronizeRecorder.InterfaceEnabled = true;
      synchronizeRecorder.showFrameTimeStamp = true;
      synchronizeRecorder.camRecorder.directory = Application.persistentDataPath + "/" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
      synchronizeRecorder.camRecorder.StartRecording();
    }
    SetProgressText("RECORDING", Color.yellow);
    SetInstructionText("PRESS '" + changeState + "' TO END RECORD", instructionColor);
    SetSavedPathsText ("");
    bug_report_state_ = BugReportState.RECORDING;
  }

  private void SavingTriggered()
  {
    if (saving_triggered_)
      return;

    if (saveReplayFrames) {
      replayPath = handController.FinishAndSaveRecording ();
    } else {
      handController.StopRecording ();
      replayPath = "";
    }
    handController.PlayRecording();
    if (synchronizeRecorder != null &&
        synchronizeRecorder.camRecorder != null) {
      synchronizeRecorder.camRecorder.StopRecording();
    }
    SetProgressText("SAVING", Color.red);
    SetInstructionText("SAVING", Color.red);
    if (replayPath.Length > 0) {
      SetSavedPathsText("Replay File @ " + replayPath);
    }
    saving_triggered_ = true;
  }
  
  private void ReplayTriggered()
  {
    SetProgressText("REPLAYING", Color.yellow);
    SetInstructionText("PRESS '" + changeState + "' TO END REPLAY", instructionColor);
    if (replayPath.Length > 0) {
      SetSavedPathsText("Replay File @ " + replayPath);
    }
    bug_report_state_ = BugReportState.REPLAYING;
  }

  private void UpdateGUI()
  {
    float progress = leap_controller_.BugReport.Progress;
    if (leap_controller_.BugReport.IsActive)
    {
      progressStatus.fillAmount = progress;
      if (progress == 1.0f)
      {
        SavingTriggered();
      }
    }

    if (leap_controller_.BugReport.IsActive != prev_bug_report_state_ && leap_controller_.BugReport.IsActive == false)
    {
      ReplayTriggered();
    }
  }

  void Init()
  {
    handController.enableRecordPlayback = true;

    ReadyTriggered();

    prev_bug_report_progress_ = leap_controller_.BugReport.Progress;
    prev_bug_report_state_ = leap_controller_.BugReport.IsActive;

    InterfaceEnabled = m_interfaceEnabled;
  }

  void Start()
  {
    if (handController == null)
    {
      Debug.LogWarning("HandController reference is null. Bug Recording -> Inactive");
      gameObject.SetActive(false);
      return;
    }
    leap_controller_ = handController.GetLeapController();
    if (leap_controller_ == null)
    {
      Debug.LogWarning("Leap Controller was not found. Bug Recording -> Blocked until found");
      InterfaceEnabled = false;
      return;
    }
    Init();
  }
	
	// Update is called once per frame
	void Update() {
    if (leap_controller_ == null) {
      leap_controller_ = handController.GetLeapController();
      if (leap_controller_ != null) {
        InterfaceEnabled = m_interfaceEnabled;
        Init();
      } else {
        return;
      }
    }

    HandleKeyInputs();
    UpdateGUI();

    prev_bug_report_progress_ = leap_controller_.BugReport.Progress;
    prev_bug_report_state_ = leap_controller_.BugReport.IsActive;
	}
}
