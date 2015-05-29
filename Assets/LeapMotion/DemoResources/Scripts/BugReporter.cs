using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Leap;

using UImage = UnityEngine.UI.Image;

public class BugReporter : MonoBehaviour {

  public HandController handController;
  public UImage progressStatus;
  public Text progressText;

  protected Controller leap_controller_;

  protected int prev_frame_id_;
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

  protected void SetText(string text, Color color)
  {
    if (progressText == null)
      return;

    progressText.text = text;
    progressText.color = color;
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
          break;
        case BugReportState.REPLAYING:
          handController.ResetRecording();
          handController.StopRecording();
          bug_report_state_ = BugReportState.READY;
          break;
        default:
          break;
      }
    }
  }

  private void RecordingTriggered()
  {
    saving_triggered_ = false;
    leap_controller_.BugReport.BeginRecording();
    handController.ResetRecording();
    handController.Record();
    SetText("RECORDING", Color.yellow);
    bug_report_state_ = BugReportState.RECORDING;
  }

  private void SavingTriggered()
  {
    if (saving_triggered_)
      return;

    handController.StopRecording();
    handController.PlayRecording();
    SetText("SAVING", Color.red);
    saving_triggered_ = true;
  }

  private void DefaultTriggered()
  {
    bug_report_state_ = BugReportState.REPLAYING;
    progressStatus.fillAmount = 1.0f;
    SetText("READY", Color.green);
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
      DefaultTriggered();
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

    prev_frame_id_ = (int)leap_controller_.Frame().Id;
    prev_bug_report_progress_ = leap_controller_.BugReport.Progress;
    prev_bug_report_state_ = leap_controller_.BugReport.IsActive;
  }
	
	// Update is called once per frame
	void Update() {
    if (handController == null || leap_controller_ == null)
      return;

    HandleKeyInputs();
    UpdateGUI();

    prev_frame_id_ = (int)leap_controller_.Frame().Id;
    prev_bug_report_progress_ = leap_controller_.BugReport.Progress;
    prev_bug_report_state_ = leap_controller_.BugReport.IsActive;
	}
}
