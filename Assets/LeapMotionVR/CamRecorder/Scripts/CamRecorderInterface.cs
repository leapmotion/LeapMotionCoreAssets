using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class CamRecorderInterface : MonoBehaviour {
  public bool m_interfaceEnabled = false;
  [System.NonSerialized]
  public bool m_hideInstructions = false;
  public KeyCode unlockStart = KeyCode.LeftShift;
  public KeyCode changeState = KeyCode.Z;

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

  public bool m_enableFrameTimeStamp = true;
  public Text frameTimeStamp;

  public bool showFrameTimeStamp {
    get {
      if (frameTimeStamp != null) {
        return frameTimeStamp.isActiveAndEnabled;
      }
      return false;
    }
    set {
      if (frameTimeStamp != null) {
        frameTimeStamp.enabled = true;
        frameTimeStamp.transform.parent.gameObject.SetActive(value);
      }
    }
  }

  public bool InterfaceEnabled {
    get {
      return (
        (instructionText.gameObject.activeInHierarchy || m_hideInstructions) &&
        statusText.gameObject.activeInHierarchy &&
        valueText.gameObject.activeInHierarchy
      );
    }
    set {
      instructionText.gameObject.SetActive(value && !m_hideInstructions);
      statusText.gameObject.SetActive(value);
      valueText.gameObject.SetActive(value);
    }
  }

  private string GetStatus() {
    return
      "[ " +
      camRecorder.framesSucceeded.ToString() + " | " +
      camRecorder.framesCountdown.ToString() + " | " +
      camRecorder.framesDropped.ToString() + " ] " +
      camRecorder.framesActual.ToString() + "/" +
      camRecorder.framesExpect.ToString();
  }

  void Start() {
    m_hideLayer = LayerMask.NameToLayer(""); // Find available layer to use
    for (int i = 0; i < hideDuringRecording.Count; ++i) {
      hideDuringRecording[i].layer = m_hideLayer; // Assign all objects to this layer
    }
    camRecorder.AddLayersToIgnore(m_hideLayer);
    InterfaceEnabled = m_interfaceEnabled;
    showFrameTimeStamp = m_enableFrameTimeStamp;
  }

  void Update() {
    if (camRecorder.IsIdling ()) {
      if ((unlockStart == KeyCode.None || Input.GetKey (unlockStart)) &&
        Input.GetKeyDown (changeState)) {
        InterfaceEnabled = true;
      } 
    }

    if (Input.GetKeyDown(changeState) && InterfaceEnabled) {
      if (camRecorder.IsIdling()) {
        startScreen.transform.localPosition = new Vector3(0.0f, 0.0f, camRecorder.GetComponent<Camera>().nearClipPlane + 0.01f);
        camRecorder.useHighResolution = highResolution;
        camRecorder.directory = Application.persistentDataPath + "/" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        camRecorder.SetCountdown(countdown);
        camRecorder.StartRecording();
      }
      else if (camRecorder.IsRecording() || camRecorder.IsCountingDown()) {
        camRecorder.StopRecording();
      }
      else if (camRecorder.IsProcessing()) {
        camRecorder.StopProcessing();
      }
    }

    if (camRecorder.IsIdling()) {
      if (unlockStart != KeyCode.None) {
        instructionText.text = "Hold '" + unlockStart + "' and press '" + changeState + "' to Start Recording";
      } else {
        instructionText.text = "Press '" + changeState + "' to Start Recording";
      }
      statusText.text = GetStatus();
      valueText.text = (camRecorder.framesExpect > 0) ? camRecorder.directory : "[ Success | Buffer | Dropped ] / Total";
    }
    else if (camRecorder.IsCountingDown()) {
      instructionText.text = "Press '" + changeState + "' to End Recording";
      statusText.text = GetStatus();
      valueText.text = "Recording in..." + ((int)camRecorder.countdownRemaining + 1).ToString();
    }
    else if (camRecorder.IsRecording()) {
      // Flash screen and beep in the first frame
      startScreen.gameObject.SetActive((camRecorder.currFrameIndex == 0));
      startSound.gameObject.SetActive((camRecorder.currFrameIndex == 0));

      instructionText.text = "Press '" + changeState + "' to End Recording";
      statusText.text = GetStatus();
      valueText.text = "Recording..." + camRecorder.duration.ToString();
    }
    else if (camRecorder.IsProcessing()) {
      instructionText.text = "'" + changeState.ToString() + "' to Abort Processing";
      statusText.text = GetStatus();
      valueText.text = "Processing..." + camRecorder.framesActual.ToString() + "/" + camRecorder.framesExpect.ToString();
    }

    if (showFrameTimeStamp &&
        HandController.Main != null) {
          frameTimeStamp.text = HandController.Main.GetFrame().Id.ToString();
    }
  }
}