using UnityEngine;
using System.Collections;

public class LeapCameraAlignment : MonoBehaviour {
  public HandController handController;
  public Transform leftEye;
  public Transform centerEye;
  public Transform rightEye;

  //[HideInInspector]
  [Range(0,1)]
  public float tween = 1f;

  // TEST: Virtual Camera Lag
  public TransformHistory history;
  public float lag = 0f;
  public float lagStep = 0.01f;
  public KeyCode hold = KeyCode.LeftShift;
  public KeyCode increaseLag = KeyCode.Equals;
  public KeyCode decreaseLag = KeyCode.Minus;
  public KeyCode zeroLag = KeyCode.Alpha0;

  void Update() {
    if (!Input.GetKey (hold)) {
      return;
    }
    if (Input.GetKeyDown (increaseLag)) {
      lag += lagStep;
    }
    if (Input.GetKeyDown (decreaseLag)) {
      lag -= lagStep;
      if (lag < 0f) {
        lag = 0f;
      }
    }
    if (Input.GetKeyDown (zeroLag)) {
      lag = 0f;
    }
  }
	
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

    // TEST: Lag the virtual cameras
    TransformHistory.TransformData past = history.TransformAtTime(Time.time - lag);
    centerEye.position = past.position;
    centerEye.rotation = past.rotation;
    rightEye.position = centerEye.position + 0.5f * oculusIPD;
    rightEye.rotation = past.rotation;
    leftEye.position = centerEye.position - 0.5f * oculusIPD;
    leftEye.rotation = past.rotation;
    Debug.Log ("Now = " + handController.GetLeapController ().Now ().ToString());

    Vector3 addIPD = 0.5f * oculusIPD.normalized * (tween * device.baseline + (1f - tween) * oculusIPD.magnitude);
    Vector3 toDevice = tween * centerEye.forward * device.focalPlaneOffset;
    centerEye.position = centerEye.position + toDevice;
    leftEye.position = centerEye.position - addIPD;
    rightEye.position = centerEye.position + addIPD;
  }

  bool HasNaN(Vector3 v) {
    return float.IsNaN (v.x) || float.IsNaN (v.y) || float.IsNaN (v.z);
  }
}
