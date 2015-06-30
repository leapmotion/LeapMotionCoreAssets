using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Leap;

using UImage = UnityEngine.UI.Image;

public class BugReporter : MonoBehaviour {

  public HandController handController;
  public UImage progressStatus;
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
    if (Input.GetKeyDown(KeyCode.Z))
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
    SetInstructionText("PRESS 'Z' TO END REPLAY", instructionColor);
    bug_report_state_ = BugReportState.REPLAYING;
  }

  private void RecordingTriggered()
  {
    saving_triggered_ = false;
    leap_controller_.BugReport.BeginRecording();
    handController.ResetRecording();
    handController.Record();
    SetProgressText("RECORDING", Color.yellow);
    SetInstructionText("PRESS 'Z' TO END RECORD", instructionColor);
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
    SetInstructionText("PRESS 'Z' TO START RECORD", instructionColor);
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

  bool Init()
  {
    leap_controller_ = handController.GetLeapController();
    if (leap_controller_ == null)
    {
      Debug.LogWarning("Leap Controller is not found. Bug Reporting will not operate properly");
      return false;
    }

    handController.enableRecordPlayback = true;

    ReadyTriggered();

    prev_bug_report_progress_ = leap_controller_.BugReport.Progress;
    prev_bug_report_state_ = leap_controller_.BugReport.IsActive;

    return true;
  }

  void Start()
  {
    if (handController == null)
    {
      Debug.LogWarning("HandController not specified. Bug Recording will not operate properly");
    }
    else
    {
      Init();
    }
  }
	
	// Update is called once per frame
	void Update() {
    if (Input.GetKeyDown(KeyCode.Escape))
    {
      Application.Quit();
    }

    if (handController == null)
    {
      return;
    }
    else if (leap_controller_ == null)
    {
      if (!Init())
        return;
    }

    HandleKeyInputs();
    UpdateGUI();

    prev_bug_report_progress_ = leap_controller_.BugReport.Progress;
    prev_bug_report_state_ = leap_controller_.BugReport.IsActive;
	}
}
