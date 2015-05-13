using UnityEngine;
using System.Collections;

public class LeapCameraAlignment : MonoBehaviour {
  public HandController handController;
  public Transform leftEye;
  public Transform rightEye;
  public Transform centerEye;
	
	// IMPORTANT: This method MUST be called after
  // OVRManager.LateUpdate
  // Call order is determined by hierarchy,
  // with parents called before children.
	void LateUpdate () {
	
	}

//  void CameraAlignment() {
//    Vector3 addIPD = Vector3.zero;
//    Vector3 toDevice = Vector3.zero;
//    addIPD = rightEye.position - leftEye.position;
//    float oculusIPD = addIPD.magnitude;
//    addIPD = 0.5f * addIPD.normalized * (DeviceIPDValue - oculusIPD);
//    toDevice = centerEye.forward * DeviceDisplace;
//    
//    if ((leftEye.position - addIPD + toDevice).x != float.NaN &&
//        (rightEye.position + addIPD + toDevice).x != float.NaN) {
//      
//      leftEye.localPosition = leftEye.position - addIPD + toDevice;
//      rightEye.localPosition = rightEye.position + addIPD + toDevice;
//      centerEye.localPosition = 0.5f * (leftEye.position + rightEye.position);
//    }
//  }
}
