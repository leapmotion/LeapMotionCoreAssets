using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TimeWarpStatus : MonoBehaviour {
  public LeapTemporalWarping cameraAlignment;

  protected Text textField;

  void Start () {
    textField = GetComponent<Text> ();
    if (textField == null) {
      gameObject.SetActive(false);
    }
  }
	
	// Update is called once per frame
  void Update () {
    if (cameraAlignment == null) {
      Debug.Log ("TimeWarpStatus requires LeapCameraAlignment reference -> status will be disabled");
      gameObject.SetActive(false);
      return;
    }
    if (!cameraAlignment.isActiveAndEnabled) {
      return;
    }

    //string statusText = "IMAGE LATENCY: " + (cameraAlignment.imageLatency.value / 1000f).ToString("#00.0") + " ms\n";
    //statusText += "LEAP RENDER: " + (cameraAlignment.frameLatency.value / 1000f).ToString ("#00.0") + " ms\n";
    //statusText += "REWIND ADJUST: " + (cameraAlignment.rewindAdjust).ToString ("#00.0") + " frames\n";

    //textField.text = statusText;
	}
}
