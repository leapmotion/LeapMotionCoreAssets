using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TouchCubeStatus : MonoBehaviour {
  public LeapCameraAlignment cameraAlignment;
	
	// Update is called once per frame
	void Update () {
    Text textField = GetComponent<Text> ();
    string statusText = "LEAP LATENCY: " + cameraAlignment.imageLatency.value.ToString("#00.0") + " ms\n";
    statusText += "REWIND: " + ((float)cameraAlignment.rewind / 1000f).ToString ("#00.0") + " ms\n";
    statusText += "RENDER: " + (1000f * cameraAlignment.latency.render).ToString ("#00.0") + " ms\n";
    statusText += "TIME WARP: " + (1000f * cameraAlignment.latency.timeWarp).ToString ("#00.0") + " ms\n";
    //statusText += "POST PRESENT: " + (1000f * cameraAlignment.latency.postPresent).ToString ("#00.0") + " ms";
    textField.text = statusText;
	}
}
