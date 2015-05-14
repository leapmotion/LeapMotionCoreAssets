using UnityEngine;
using System.Collections;

public class LeapCameraAlignment : MonoBehaviour {
  public HandController handController;
  public Transform leftEye;
  public Transform centerEye;
  public Transform rightEye;

  [HideInInspector]
  public float tween = 1.0f;
	
	// IMPORTANT: This method MUST be called after
  // OVRManager.LateUpdate
  // NOTE: Call order is determined by enabling...
  // Use ExecutionOrdering script to ensure correct call order
  void LateUpdate() {
    if (handController == null ||
      leftEye == null ||
      rightEye == null) {
      Debug.Log ("Hand Controller & Eye references cannot be null");
      return;
    }

    if (HasNaN (leftEye.position) ||
        HasNaN (centerEye.position) ||
        HasNaN (rightEye.position))
      // Uninitialized transforms
      return;

    // ASSUME: Oculus resets camera positions in each frame
    Vector3 oculusIPD = rightEye.position - leftEye.position;
    if (oculusIPD.magnitude < float.Epsilon)
      // Unmodified camera positions
      return;

    LeapDeviceInfo device = handController.GetDeviceInfo ();
    if (device.type == LeapDeviceType.Invalid)
      return;

    Vector3 addIPD = 0.5f * oculusIPD.normalized * (device.baseline - oculusIPD.magnitude) * tween;
    Vector3 toDevice = centerEye.forward * device.focalPlaneOffset * tween;
    leftEye.position = leftEye.position - addIPD + toDevice;
    rightEye.position = rightEye.position + addIPD + toDevice;
    centerEye.position = 0.5f * (leftEye.position + rightEye.position);
  }

  bool HasNaN(Vector3 v) {
    return float.IsNaN (v.x) || float.IsNaN (v.y) || float.IsNaN (v.z);
  }
}
