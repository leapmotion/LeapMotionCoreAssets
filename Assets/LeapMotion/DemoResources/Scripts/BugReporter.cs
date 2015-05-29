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

  protected Color colorCharcoal = new Color(0.125f, 0.125f, 0.125f);

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
    SetInstructionText("PRESS 'Z' TO END REPLAY", colorCharcoal);
    bug_report_state_ = BugReportState.REPLAYING;
  }

  private void RecordingTriggered()
  {
    saving_triggered_ = false;
    leap_controller_.BugReport.BeginRecording();
    handController.ResetRecording();
    handController.Record();
    SetProgressText("RECORDING", Color.yellow);
    SetInstructionText("PRESS 'Z' TO END RECORD", colorCharcoal);
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
    SetInstructionText("PRESS 'Z' TO START RECORD", colorCharcoal);
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

  void Start()
  {
    if (handController == null)
    {
      Debug.LogWarning("HandController not specified. Bug Recording will not operate properly");
    }
    else
    {
      leap_controller_ = handController.GetLeapController();
    }

    ReadyTriggered();

    prev_bug_report_progress_ = leap_controller_.BugReport.Progress;
    prev_bug_report_state_ = leap_controller_.BugReport.IsActive;
  }
	
	// Update is called once per frame
	void Update() {
    if (handController == null || leap_controller_ == null)
      return;

    HandleKeyInputs();
    UpdateGUI();

    prev_bug_report_progress_ = leap_controller_.BugReport.Progress;
    prev_bug_report_state_ = leap_controller_.BugReport.IsActive;
	}
}
