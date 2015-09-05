using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TouchCubeHelpMenu : MonoBehaviour
{
  public GameObject trackingController;
  public GameObject overlayImage;

  // Update is called once per frame
  void Update () {
    Text textField = GetComponent<Text> ();
    if (!textField) {
      return;
    }

    string trackingControllerToState = "ON";
    if (trackingController.activeSelf) {
      trackingControllerToState = "OFF";
    }
    string overlayImageToState = "ON";
    if (overlayImage.activeSelf) {
      overlayImageToState = "OFF";
    }

    textField.text = "H : Help Hide/Show" 
      + "\nR : Reset Virtual Cameras"
      + "\nRet. : Cycle Space-Time Alignment"
      + "\nT : Tracked Skeleton " + trackingControllerToState
      + "\nO : Overlay Image Mode " + overlayImageToState;
  }
}
