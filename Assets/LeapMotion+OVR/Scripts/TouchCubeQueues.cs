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
  public LeapCameraAlignment alignment;
  public CompensatedRescale rescale;
  public GameObject headMount;
  [Header("Messages")]
  public GameObject helpMenuCanvas;
  public GameObject noAlignmentCanvas;
  public GameObject playerRescaleCanvas;
  public GameObject alignedViewsCanvas;
  public GameObject rewindViewsCanvas;
  public GameObject warpImagesCanvas;

  private int demoStage = 0;

  void Start () {
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
      alignment.tweenRewind = 0f;
      alignment.tweenTimeWarp = 0f;
      alignment.tweenTimeWarp = 0f;
      alignment.tweenPosition = 0f;
      headMount.transform.localScale = Vector3.one / rescale.decreaseFactor;
      noAlignmentCanvas.SetActive(true);
      demoStage++;
      break;
    case 1: // Player Rescale
      noAlignmentCanvas.SetActive(false);
      headMount.transform.localScale = Vector3.one;
      rescale.enabled = true;
      rescale.DecreaseScale();
      playerRescaleCanvas.SetActive(true);
      demoStage++;
      break;
    case 2: // IPD Alignment
      playerRescaleCanvas.SetActive(false);
      rescale.ResetScale();
      rescale.enabled = false;
      alignment.tweenPosition = 1f;
      alignedViewsCanvas.SetActive(true);
      demoStage++;
      break;
    case 3: // IPD Alignment + Rewind
      alignedViewsCanvas.SetActive(false);
      alignment.tweenRewind = 1f;
      rewindViewsCanvas.SetActive(true);
      demoStage++;
      break;
    case 4: // IPD Alignment + TimeWarp
      rewindViewsCanvas.SetActive(false);
      alignment.tweenRewind = 0f;
      alignment.tweenTimeWarp = 1f;
      warpImagesCanvas.SetActive(true);
      demoStage++;
      break;
    default:
      ResetState();
      demoStage = 0;
      break;
    }
	}

  public void ResetState() {
    helpMenuCanvas.SetActive(true);
    noAlignmentCanvas.SetActive (false);
    playerRescaleCanvas.SetActive (false);
    alignedViewsCanvas.SetActive (false);
    rewindViewsCanvas.SetActive (false);
    warpImagesCanvas.SetActive (false);
    alignment.tweenRewind = 0f;
    alignment.tweenTimeWarp = 1f;
    alignment.tweenPosition = 1f;
    rescale.enabled = false;
  }
}
