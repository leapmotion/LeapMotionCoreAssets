using UnityEngine;
using System.Collections;

public class OVRReset : MonoBehaviour {
  public KeyCode ResetKey;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
    if (Input.GetKeyDown(ResetKey)) {
      OVRManager.display.RecenterPose();
    }
	}
}
