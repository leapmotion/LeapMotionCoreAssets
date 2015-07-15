using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TouchCubeStatus : MonoBehaviour {
  public LeapCameraAlignment cameraAlignment;
	
	// Update is called once per frame
	void Update () {
    Text textField = GetComponent<Text> ();
    string statusText = "IMAGE LATENCY: " + (cameraAlignment.imageLatency.value / 1000f).ToString("#00.0") + " ms\n";
    statusText += "LEAP RENDER: " + (cameraAlignment.frameLatency.value / 1000f).ToString ("#00.0") + " ms\n";
    statusText += "REWIND ADJUST: " + (cameraAlignment.rewindAdjust).ToString ("#00.0") + " frames\n";
//    if (OVRManager.display != null) {
//      float ovrLatency = OVRManager.display.latency.render;
//      statusText += "OVR RENDER: " + (OVRManager.display.latency.render * 1000f).ToString ("#00.0") + " ms\n";
//      statusText += "TIME WARP: " + (OVRManager.display.latency.timeWarp * 1000f).ToString ("#00.0") + " ms\n";
//      statusText += "POST PRESENT: " + (OVRManager.display.latency.postPresent * 1000f).ToString ("#00.0") + " ms";
//    }
    textField.text = statusText;
	}
}
