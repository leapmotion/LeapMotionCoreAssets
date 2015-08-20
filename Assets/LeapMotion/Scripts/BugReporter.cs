using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using Leap;

using UImage = UnityEngine.UI.Image;

public class BugReporter : ReporterBase {
  public HandController handController;
  public UImage progressStatus;
  public UImage progressBar;
  public Text progressText;
  public Text instructionText;
  public Text savedpathsText;
  public bool saveReplayFrames = false;

  protected Color instructionColor = new Color(1.0f, 0.5f, 0.0f);
  protected Controller leap_controller_;
  protected bool prev_bug_report_state_;
  protected string replayPath;

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

  protected override bool Reset() {
    handController.enableRecordPlayback = true;
    
    progressStatus.fillAmount = 1.0f;
    SetProgressText("READY", Color.green);
    SetInstructionText("PRESS '" + m_safetyKey + "+" + m_triggerKey + "' TO START RECORDING", instructionColor);
    SetSavedPathsText("");
    return true;
  }

  protected override bool StartRecording() {
    leap_controller_.BugReport.BeginRecording();
    handController.Record();

    SetProgressText("RECORDING", Color.yellow);
    SetInstructionText("PRESS '" + m_triggerKey + "' TO END RECORD", instructionColor);
    SetSavedPathsText("");
    return true;
  }

  protected override bool AbortRecording() {
    leap_controller_.BugReport.EndRecording();
    handController.StopRecording();
    return true;
  }

  protected override bool StartSaving() {
    replayPath = (saveReplayFrames) ? handController.FinishAndSaveRecording() : "";
    handController.PlayRecording();

    SetProgressText("SAVING", Color.red);
    SetInstructionText("SAVING", Color.red);
    if (replayPath.Length > 0) {
      SetSavedPathsText("Replay File @ " + replayPath);
    }
    return true;
  }

  protected override bool AbortSaving() {
    return false; // Disallow aborting saving
  }

  protected override bool StartReplaying() {
    SetProgressText("REPLAYING", Color.yellow);
    SetInstructionText("PRESS '" + m_triggerKey + "' TO END REPLAY", instructionColor);
    if (replayPath.Length > 0) {
      SetSavedPathsText("Replay File @ " + replayPath);
    }
    return true;
  }

  protected override bool AbortReplaying() {
    handController.ResetRecording();
    return true;
  }

  private void UpdateGUI()
  {
    float progress = leap_controller_.BugReport.Progress;
    if (leap_controller_.BugReport.IsActive)
    {
      progressStatus.fillAmount = progress;
      if (progress == 1.0f)
      {
        if (TriggerAbortRecording())
          TriggerStartSaving();
      }
    }

    if (leap_controller_.BugReport.IsActive != prev_bug_report_state_ && leap_controller_.BugReport.IsActive == false)
    {
      TriggerStartReplaying();
    }
    prev_bug_report_state_ = leap_controller_.BugReport.IsActive;
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
      return;
    }
  }
	
	// Update is called once per frame
	void Update() {
    if (leap_controller_ == null) {
      leap_controller_ = handController.GetLeapController();
      if (leap_controller_ == null) {
        return;
      }
    }

    UpdateGUI();
	}
}
