using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

[RequireComponent (typeof(Canvas))]
public class CamRecorderInterface : MonoBehaviour {

  public CamRecorder camRecorder;
  public Text instructionText;
  public Text statusText;
  public Text valueText;
  public float countdown = 5.0f;
  public float quality = 1.0f;

  private float m_targetTime = 0.0f;
  private bool m_idling = true;

	void Update () {
    if (
      Input.GetKeyDown(KeyCode.Return) ||
      Input.GetKeyDown(KeyCode.KeypadEnter)
      )
    {
      if (camRecorder.IsIdling())
      {
        if (camRecorder.SetDirectory(DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")))
        {
          m_targetTime = Time.time + countdown;
        }
      }
      else if (camRecorder.IsRecording())
      {
        camRecorder.StopRecording();
      }
      else if (camRecorder.IsProcessing())
      {
        camRecorder.StopProcessing();
      }
    }

    if (camRecorder.IsIdling() && m_targetTime == 0.0f)
    {
      instructionText.text = "'Enter' to Start Recording";
      if (camRecorder.frameCount > 0)
      {
        statusText.text = camRecorder.frameCount.ToString() + " images found at";
        valueText.text = camRecorder.directory;
      }
      else
      {
        statusText.text = "";
        valueText.text = "";
      }
    }
    else if (camRecorder.IsIdling() && (Time.time < m_targetTime))
    {
      instructionText.text = "'Enter' to End Recording";
      statusText.text = "Recording in...";
      valueText.text = ((int)(m_targetTime - Time.time) + 1).ToString();
    }
    else if (camRecorder.IsIdling() && (Time.time >= m_targetTime))
    {
      m_targetTime = 0.0f;
      instructionText.text = "";
      statusText.text = "";
      valueText.text = "";
      camRecorder.StartRecording(quality);
    }
    else if (camRecorder.IsRecording())
    {
      instructionText.text = "";
      statusText.text = "";
      valueText.text = "";
    }
    else if (camRecorder.IsProcessing())
    {
      instructionText.text = "'Enter' to Abort Processing";
      statusText.text = "Processing Data...";
      valueText.text = (camRecorder.progress * 100.0f).ToString() + "%";
    }
	}
}
