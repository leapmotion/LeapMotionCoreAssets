using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class CamRecorderInterface : ReporterBase {
  [System.NonSerialized]
  public bool m_hideInstructions = false;

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
  public HandController handController;

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

  private string GetStatus() {
    return
      "[ " +
      camRecorder.framesSucceeded.ToString() + " | " +
      camRecorder.framesCountdown.ToString() + " | " +
      camRecorder.framesDropped.ToString() + " ] " +
      camRecorder.framesActual.ToString() + "/" +
      camRecorder.framesExpect.ToString();
  }

  protected override bool StartRecording() {
    startScreen.transform.localPosition = new Vector3(0.0f, 0.0f, camRecorder.GetComponent<Camera>().nearClipPlane + 0.01f);
    camRecorder.useHighResolution = highResolution;
    camRecorder.directory = Application.persistentDataPath + "/" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
    camRecorder.SetCountdown(countdown);
    camRecorder.StartRecording();
    return true;
  }

  protected override bool AbortRecording() {
    camRecorder.StopRecording();
    return true;
  }

  protected override bool StartSaving() {
    return true;
  }

  protected override bool AbortSaving() {
    camRecorder.StopProcessing();
    return true;
  }

  void Start() {
    m_hideLayer = LayerMask.NameToLayer(""); // Find available layer to use
    for (int i = 0; i < hideDuringRecording.Count; ++i) {
      hideDuringRecording[i].layer = m_hideLayer; // Assign all objects to this layer
    }
    camRecorder.AddLayersToIgnore(m_hideLayer);
    showFrameTimeStamp = m_enableFrameTimeStamp;
  }

  void Update() {
    if (camRecorder.IsIdling()) {
      if (m_safetyKey != KeyCode.None) {
        instructionText.text = "Hold '" + m_safetyKey + "' and press '" + m_triggerKey + "' to Start Recording";
      } else {
        instructionText.text = "Press '" + m_triggerKey + "' to Start Recording";
      }
      statusText.text = GetStatus();
      valueText.text = (camRecorder.framesExpect > 0) ? camRecorder.directory : "[ Success | Buffer | Dropped ] / Total";
    }
    else if (camRecorder.IsCountingDown()) {
      instructionText.text = "Press '" + m_triggerKey + "' to End Recording";
      statusText.text = GetStatus();
      valueText.text = "Recording in..." + ((int)camRecorder.countdownRemaining + 1).ToString();
    }
    else if (camRecorder.IsRecording()) {
      // Flash screen and beep in the first frame
      startScreen.gameObject.SetActive((camRecorder.currFrameIndex == 0));
      startSound.gameObject.SetActive((camRecorder.currFrameIndex == 0));

      instructionText.text = "Press '" + m_triggerKey + "' to End Recording";
      statusText.text = GetStatus();
      valueText.text = "Recording..." + camRecorder.duration.ToString();
    }
    else if (camRecorder.IsProcessing()) {
      instructionText.text = "'" + m_triggerKey.ToString() + "' to Abort Processing";
      statusText.text = GetStatus();
      valueText.text = "Processing..." + camRecorder.framesActual.ToString() + "/" + camRecorder.framesExpect.ToString();
    }

    if (showFrameTimeStamp &&
        handController != null) {
      frameTimeStamp.text = handController.GetFrame().Id.ToString();
    }
  }
}