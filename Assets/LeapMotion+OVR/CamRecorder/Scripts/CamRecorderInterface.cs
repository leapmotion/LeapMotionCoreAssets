using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class CamRecorderInterface : MonoBehaviour {

  public CamRecorder camRecorder;
  public Canvas startScreen;
  public AudioSource startSound;
  public Text instructionText;
  public Text statusText;
  public Text valueText;
  public float countdown = 5.0f;
  public bool highResolution = false;
  public List<GameObject> hideDuringRecording = new List<GameObject>();

  private int m_hideLayer = 0;

  private string GetStatus()
  {
    return
      "[ " +
      camRecorder.framesSucceeded.ToString() + " | " +
      camRecorder.framesCountdown.ToString() + " | " +
      camRecorder.framesDropped.ToString() + " ] " +
      camRecorder.framesActual.ToString() + "/" +
      camRecorder.framesExpect.ToString();
  }

  void Start()
  {
    m_hideLayer = LayerMask.NameToLayer(""); // Find available layer to use
    for (int i = 0; i < hideDuringRecording.Count; ++i)
    {
      hideDuringRecording[i].layer = m_hideLayer; // Assign all objects to this layer
    }
  }

	void Update () {
    if (
      Input.GetKeyDown(KeyCode.Return) ||
      Input.GetKeyDown(KeyCode.KeypadEnter)
      )
    {
      if (camRecorder.IsIdling())
      {
        startScreen.transform.localPosition = new Vector3(0.0f, 0.0f, camRecorder.GetComponent<Camera>().nearClipPlane + 0.01f);
        camRecorder.useHighResolution = highResolution;
        camRecorder.directory = Application.persistentDataPath + "/" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        camRecorder.SetCountdown(countdown);
        camRecorder.AddLayersToIgnore(m_hideLayer);
        camRecorder.StartRecording();
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
      statusText.text = GetStatus();
      valueText.text = (camRecorder.framesExpect > 0) ? camRecorder.directory : "[ Success | Buffer | Dropped ] / Total";
    }
    else if (camRecorder.IsCountingDown())
    {
      instructionText.text = "'Enter' to End Recording";
      statusText.text = GetStatus();
      valueText.text = "Recording in..." + ((int)camRecorder.countdownRemaining + 1).ToString();
    }
    else if (camRecorder.IsRecording())
    {
      // Flash screen and beep in the first frame
      startScreen.gameObject.SetActive((camRecorder.currFrameIndex == 0));
      startSound.gameObject.SetActive((camRecorder.currFrameIndex == 0));
      
      instructionText.text = "'Enter' to End Recording";
      statusText.text = GetStatus();
      valueText.text = "Recording..." + camRecorder.duration.ToString();
    }
    else if (camRecorder.IsProcessing())
    {
      instructionText.text = "'Enter' to Abort Processing";
      statusText.text = GetStatus();
      valueText.text = "Processing..." + camRecorder.framesActual.ToString() + "/" + camRecorder.framesExpect.ToString();
    }
	}
}
