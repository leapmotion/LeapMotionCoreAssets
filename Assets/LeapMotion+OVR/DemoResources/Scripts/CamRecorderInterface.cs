using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

[RequireComponent (typeof(Canvas))]
public class CamRecorderInterface : MonoBehaviour {

  public CamRecorder camRecorder;
  public Text instructionText;
  public Text statusText;
  public Text valueText;
  public float countdown = 5.0f;
  public bool highResolution = false;

	void Update () {
    if (
      Input.GetKeyDown(KeyCode.Return) ||
      Input.GetKeyDown(KeyCode.KeypadEnter)
      )
    {
      if (camRecorder.IsIdling())
      {
        if (camRecorder.SetDirectory(Application.persistentDataPath + "/" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")))
        {
          camRecorder.SetCountdown(countdown);
          camRecorder.AddLayerToIgnore(gameObject.layer);
          camRecorder.StartRecording();
        }
      }
      else if (camRecorder.IsRecording() || camRecorder.IsCountingDown())
      {
        camRecorder.StopRecording();
      }
      else if (camRecorder.IsProcessing())
      {
        camRecorder.StopProcessing();
      }
    }

    if (camRecorder.IsIdling())
    {
      instructionText.text = "'Enter' to Start Recording";
      if (camRecorder.framesRecorded > 0)
      {
        statusText.text = camRecorder.passedFrames.ToString() + " successful and " + camRecorder.failedFrames.ToString() + " failed images at";
        valueText.text = camRecorder.directory;
      }
      else
      {
        statusText.text = "";
        valueText.text = "";
      }
    }
    else if (camRecorder.IsCountingDown())
    {
      instructionText.text = "'Enter' to End Recording";
      statusText.text = "Recording in...";
      valueText.text = ((int)camRecorder.countdownRemaining + 1).ToString();
    }
    else if (camRecorder.IsRecording())
    {
      instructionText.text = "Frames-Per-Second: " + camRecorder.frameRate.ToString();
      statusText.text = "Duration: " + camRecorder.duration.ToString();
      valueText.text = "Frames Recorded (Pass/Fail): " + camRecorder.passedFrames.ToString() + "/" + camRecorder.failedFrames.ToString();
    }
    else if (camRecorder.IsProcessing())
    {
      instructionText.text = "'Enter' to Abort Processing";
      statusText.text = "Processing Data...";
      valueText.text = camRecorder.framesProcessed.ToString() + "/" + camRecorder.framesRecorded.ToString();
    }
	}
}
