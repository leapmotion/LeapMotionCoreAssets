using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Demo queues for TouchCube scene. Names are hard-coded -> replace with Mecanim scripting
/// </summary>
/// <remarks>
/// Scene 0: Correct Alignment + Navigation Instructions (press space)
/// Scene 1: No Alignment
/// Scene 2: Player rescaling
/// Scene 3: Normal Use + Help Menu
/// </remarks>
public class TouchCubeQueues : MonoBehaviour {
  public KeyCode queueKey = KeyCode.Return;
  public LeapVRTemporalWarping alignment;
  [Header("Messages")]
  public GameObject helpMenuCanvas;
  public GameObject noAlignmentCanvas;
  public GameObject alignedViewsCanvas;
  public GameObject warpImagesCanvas;

  private LeapVRCameraControl[] _cameraCorrectionScripts;
  private int demoStage = 0;

  void Start () {
    _cameraCorrectionScripts = FindObjectsOfType<LeapVRCameraControl>();
    ResetState ();
  }
	
	// Update is called once per frame
	void Update () {
	  if (!Input.GetKeyDown (queueKey)) {
      return;
    }
    switch (demoStage) {
    case 0: // No Alignment
      helpMenuCanvas.SetActive(false);
      alignment.TweenImageWarping = 0f;
      setOverridePos(false);
      noAlignmentCanvas.SetActive(true);
      demoStage++;
      break;
    case 1: // IPD Alignment
      noAlignmentCanvas.SetActive(false);
      setOverridePos(true);
      setOverridePos(true);
      alignedViewsCanvas.SetActive(true);
      demoStage++;
      break;
    case 2: // IPD Alignment + temporal warping
      alignedViewsCanvas.SetActive(false);
      alignment.TweenImageWarping = 1f;
      warpImagesCanvas.SetActive(true);
      demoStage++;
      break;
    default:
      ResetState();
      demoStage = 0;
      break;
    }
	}

  private void setOverridePos(bool overridePos) {
    foreach (var correction in _cameraCorrectionScripts) {
      correction.OverrideEyePosition = overridePos;;
    }
  }

  public void ResetState() {
    helpMenuCanvas.SetActive(true);
    noAlignmentCanvas.SetActive (false);
    alignedViewsCanvas.SetActive (false);
    warpImagesCanvas.SetActive (false);
    alignment.TweenImageWarping = 1f;
    setOverridePos(true);
  }
}
