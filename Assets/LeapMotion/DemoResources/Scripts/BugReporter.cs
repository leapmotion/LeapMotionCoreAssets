using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Leap;

using UImage = UnityEngine.UI.Image;

public class BugReporter : MonoBehaviour {

  public HandController handController;
  public UImage progress;
  public Text progressText;

  protected Controller leap_controller_;

  protected int prev_frame_id_;
  protected float prev_bug_report_progress_;
  protected bool prev_bug_report_state_;

  protected void SetText(string text, Color color)
  {
    if (progressText == null)
      return;

    progressText.text = text;
    progressText.color = color;
  }

  private void HandleKeyInputs()
  {
    if (Input.GetKeyDown(KeyCode.S))
    {
      leap_controller_.BugReport.BeginRecording();
    }
    else if (Input.GetKeyDown(KeyCode.E))
    {
      leap_controller_.BugReport.EndRecording();
    }
  }

  private void UpdateGUI()
  {
    if (leap_controller_.BugReport.IsActive)
    {
      progress.fillAmount = leap_controller_.BugReport.Progress;
    }

    // If report is active and frame is changing and progress is constant, then the report is being saved
    if (prev_frame_id_ != (int)leap_controller_.Frame().Id) {  
      if (leap_controller_.BugReport.IsActive) {
        if (prev_bug_report_progress_ == leap_controller_.BugReport.Progress) 
        {
          SetText("SAVING", Color.red);
        } else {
          SetText("RECORDING", Color.yellow);
        }
      }
      else
      {
        SetText("READY", Color.green);
      }
    }

    if (prev_bug_report_state_ != leap_controller_.BugReport.IsActive)
    {
      if (leap_controller_.BugReport.IsActive) // Just turned active
      {
        handController.ResetRecording();
        handController.Record();
      }
      else // Just turned inactive
      {
        progress.fillAmount = 1.0f;
        handController.StopRecording();
        handController.PlayRecording();
      }
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
