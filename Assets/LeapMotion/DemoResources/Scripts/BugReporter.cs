using UnityEngine;
using UnityEngine.UI;
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

  protected Color instructionColor = new Color(1.0f, 0.5f, 0.0f);

  protected Controller leap_controller_;

  protected float prev_bug_report_progress_;
  protected bool prev_bug_report_state_;

  protected bool saving_triggered_ = false;

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
      m_interfaceEnabled = value;
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

  private void HandleKeyInputs()
  {
    if (bug_report_state_ == BugReportState.READY) {
      if ((unlockStart == KeyCode.None || Input.GetKey (unlockStart)) &&
          Input.GetKeyDown (changeState)) {
        InterfaceEnabled = true;
      } 
    } else {
      InterfaceEnabled = m_interfaceEnabled;
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

  private void ReplayTriggered()
  {
    SetProgressText("REPLAYING", Color.yellow);
    SetInstructionText("PRESS '" + changeState + "' TO END REPLAY", instructionColor);
    bug_report_state_ = BugReportState.REPLAYING;
  }

  private void RecordingTriggered()
  {
    saving_triggered_ = false;
    leap_controller_.BugReport.BeginRecording();
    handController.ResetRecording();
    handController.Record();
    SetProgressText("RECORDING", Color.yellow);
    SetInstructionText("PRESS '" + changeState + "' TO END RECORD", instructionColor);
    bug_report_state_ = BugReportState.RECORDING;
  }

  private void SavingTriggered()
  {
    if (saving_triggered_)
      return;

    handController.StopRecording();
    handController.PlayRecording();
    SetProgressText("REPLAYING", Color.yellow);
    SetInstructionText("SAVING", Color.red);
    saving_triggered_ = true;
  }

  private void ReadyTriggered()
  {
    handController.ResetRecording();
    handController.StopRecording();
    progressStatus.fillAmount = 1.0f;
    SetProgressText("READY", Color.green);
    SetInstructionText("PRESS '" + changeState + "' TO START RECORD", instructionColor);
    bug_report_state_ = BugReportState.READY;
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
      Debug.LogWarning("Leap Controller was not found. Bug Recording -> Disabled until found");
      return;
    }
    Init();
  }
	
	// Update is called once per frame
	void Update() {
    if (leap_controller_ == null) {
      leap_controller_ = handController.GetLeapController();
      if (leap_controller_ != null) {
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
