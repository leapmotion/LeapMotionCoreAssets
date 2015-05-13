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
  void LateUpdate() {
    if (handController == null ||
      leftEye == null ||
      rightEye == null) {
      Debug.Log ("Hand Controller & Eye references cannot be null");
      return;
    }

    LeapDeviceInfo device = handController.GetDeviceInfo ();
    if (device.type == LeapDeviceType.Invalid)
      return;

    Vector3 addIPD = Vector3.zero;
    Vector3 toDevice = Vector3.zero;
    addIPD = rightEye.position - leftEye.position;
    // ASSUME: Oculus resets camera positions in each frame
    float oculusIPD = addIPD.magnitude;
    addIPD = 0.5f * addIPD.normalized * (device.baseline - oculusIPD);
    toDevice = centerEye.forward * device.focalPlaneOffset;
    
    if ((leftEye.position - addIPD + toDevice).x != float.NaN &&
        (rightEye.position + addIPD + toDevice).x != float.NaN) {
      
      leftEye.localPosition = leftEye.position - addIPD + toDevice;
      rightEye.localPosition = rightEye.position + addIPD + toDevice;
    }
  }
}
