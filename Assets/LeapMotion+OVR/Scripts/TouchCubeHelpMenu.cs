using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TouchCubeHelpMenu : MonoBehaviour {
  public LeapCameraAlignment cameraAlignment;
  
  // Update is called once per frame
  void Update () {
    Text textField = GetComponent<Text> ();
    if (!textField) {
      return;
    }

    string cameraAlignmentToState = "ON";
    if (cameraAlignment.enabled) {
      cameraAlignmentToState = "OFF";
    }
    string helpText = "H : Help Hide/Show" 
      + "\nR : Reset Virtual Cameras"
      + "\nRet. : Cycle IPD Alignment";
    if (!cameraAlignment.enabled) {
      helpText += 
        "\n+ : Increase Virtual IPD"
      + "\n- : Reduce Virtual IPD";
    }
    helpText += 
        "\nX : Use Time Warping"
      + "\nA : Turn IPD Alignment " + cameraAlignmentToState
      + "\nO : Overlay Image Modes";
    textField.text = helpText;
  }
}
