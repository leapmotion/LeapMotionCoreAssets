using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TouchCubeStatus : MonoBehaviour {
  public LeapCameraAlignment cameraAlignment;
	
	// Update is called once per frame
	void Update () {
    Text textField = GetComponent<Text> ();
    string statusText = "IMAGE LATENCY: " + (cameraAlignment.imageLatency.value / 1000f).ToString("#00.0") + " ms\n";
    statusText += "LEAP RENDER: " + (cameraAlignment.leapLatency.value / 1000f).ToString ("#00.0") + " ms\n";
    statusText += "OVR RENDER: " + (cameraAlignment.ovrLatency * 1000f).ToString ("#00.0") + " ms\n";
    statusText += "REWIND ADJUST: " + ((float)cameraAlignment.rewindAdjust / 1000f).ToString ("#00.0") + " ms\n";
    //statusText += "TIME WARP: " + (1000f * cameraAlignment.latency.timeWarp).ToString ("#00.0") + " ms\n";
    //statusText += "POST PRESENT: " + (1000f * cameraAlignment.latency.postPresent).ToString ("#00.0") + " ms";
    textField.text = statusText;
	}
}
