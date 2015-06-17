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
  public HandController handController;
  [Header("Messages")]
  public GameObject noAlignmentCanvas;
  public GameObject playerRescaleCanvas;
  public GameObject alignedViewsCanvas;
  public GameObject helpMenuCanvas;

  private int demoStage = 0;
	
	// Update is called once per frame
	void Update () {
	  if (!Input.GetKeyDown (queueKey)) {
      return;
    }
    switch (demoStage) {
    case 0:
      helpMenuCanvas.SetActive(false);
      alignment.enabled = false;
      handController.transform.localScale = Vector3.one / rescale.decreaseFactor;
      noAlignmentCanvas.SetActive(true);
      demoStage++;
      break;
    case 1:
      noAlignmentCanvas.SetActive(false);
      handController.transform.localScale = Vector3.one;
      rescale.enabled = true;
      rescale.DecreaseScale();
      playerRescaleCanvas.SetActive(true);
      demoStage++;
      break;
    case 2:
      playerRescaleCanvas.SetActive(false);
      rescale.ResetScale();
      rescale.enabled = false;
      alignment.enabled = true;
      alignedViewsCanvas.SetActive(true);
      demoStage++;
      break;
    default:
      Reset();
      demoStage = 0;
      break;
    }
	}

  public void Reset() {
    noAlignmentCanvas.SetActive (false);
    playerRescaleCanvas.SetActive (false);
    alignedViewsCanvas.SetActive (false);
    helpMenuCanvas.SetActive(true);
    alignment.enabled = true;
    rescale.ResetScale ();
  }
}
