using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap;

public class LeapCameraAlignment : MonoBehaviour {
  [Range(0,1)]
  public float tweenPosition = 1f;
  [Range(0,1)]
  public float tweenTimeWarp = 1f;

  [Header("Alignment Targets")]
  public HandController handController;
  public Transform leftEye;
  public Transform centerEye;
  public Transform rightEye;

  public long testLag = 20000;
  public long historyTime = 2000000;

  protected struct TransformData {
    public long leapTime; // microseconds
    public Vector3 position; //meters
    public Quaternion rotation;
    
    public static TransformData Lerp(TransformData from, TransformData to, long time) {
      if (from.leapTime == to.leapTime) {
        return from;
      }
      float fraction = (float)(time - from.leapTime) / (float)(to.leapTime - from.leapTime);
      return new TransformData () {
        leapTime = time, 
        position = Vector3.Lerp (from.position, to.position, fraction), 
        rotation = Quaternion.Slerp (from.rotation, to.rotation, fraction)
      };
    }
  }
  protected List<TransformData> history;
  
  /// <summary>
  /// Estimates the transform at the specified time
  /// </summary>
  /// <returns>
  /// A transform with leapTime == time only if interpolation was possible
  /// </returns>
  protected TransformData TransformAtTime(long time) {
    if (history.Count < 1) {
      Debug.LogWarning ("NO HISTORY!");
      return new TransformData () {
        leapTime = 0,
        position = Vector3.zero,
        rotation = Quaternion.identity
      };
    }
    if (history [0].leapTime > time) {
      Debug.LogWarning ("NO INTERPOLATION: history.First = " + history [0].leapTime + " > time = " + time);
      return history[0];
    }
    int t = 1;
    while (t < history.Count &&
           history[t].leapTime < time) {
      t++;
    }
    if (!(t < history.Count)) {
      Debug.LogWarning ("NO INTERPOLATION: history.Last = " + history[history.Count-1].leapTime + " < time = " + time);
      return history[history.Count-1];
    }
    
    return TransformData.Lerp (history[t-1], history[t], time);
  }

  // Use this for initialization
  void Start () {
    if (handController == null) {
      Debug.LogWarning ("TransformHistory REQUIRES a reference to a HandController");
      return;
    }
    history = new List<TransformData> ();
  }
	
	// IMPORTANT: This method MUST be called after
  // OVRManager.LateUpdate
  // NOTE: Call order is determined by activation order...
  // Use EnableUpdateOrdering script to ensure correct call order
  void LateUpdate() {
    if (handController == null ||
        leftEye == null ||
        rightEye == null) {
      Debug.Log ("Hand Controller & Eye references cannot be null");
      return;
    }

    UpdateHistory ();
    UpdateTimeWarp ();
    UpdateAlignment ();
  }
  
  void UpdateHistory () {
    // ASSUME: Oculus resets relative camera positions in each frame
    // Append latest position & rotation to head
    long now = handController.GetLeapController ().Now ();
    history.Add (new TransformData () {
      leapTime = now,
      position = transform.position,
      rotation = transform.rotation
    });
    //Debug.Log ("Last Index Time = " + history[history.Count-1].leapTime + " =? " + now);
    
    // Reduce history length
    while (history.Count > 0 &&
           historyTime < now - history [0].leapTime) {
      //Debug.Log ("Removing oldest from history.Count = " + history.Count);
      history.RemoveAt(0);
    }
  }
  
  void UpdateTimeWarp () {
    // ASSUME: Oculus resets relative camera positions in each frame
    float virtualCameraRadius = 0.5f * (rightEye.position - leftEye.position).magnitude;
    if (virtualCameraRadius < float.Epsilon)
      // Unmodified camera positions
      return;
    
    //TODO : Use timestamp of images
    // This requires that the image timestamps are available OR that the time stamps
    // can be predicted
    // LEAP_EXPORT int64_t Controller::now() const;
    // LEAP_EXPORT int64_t Image::timestamp() const;
    //TODO : Apply tweening to virtual camera lag
    TransformData past = TransformAtTime(handController.GetLeapController ().Now () - testLag);
    centerEye.position = past.position;
    centerEye.rotation = past.rotation;
    rightEye.position = centerEye.position + virtualCameraRadius * centerEye.right;
    rightEye.rotation = past.rotation;
    leftEye.position = centerEye.position - virtualCameraRadius * centerEye.right;
    leftEye.rotation = past.rotation;
  }

  void UpdateAlignment () {
    if (HasNaN (leftEye.position) ||
        HasNaN (centerEye.position) ||
        HasNaN (rightEye.position))
      // Uninitialized transforms
      return;

    Vector3 oculusIPD = rightEye.position - leftEye.position;
    if (oculusIPD.magnitude < float.Epsilon)
      // Unmodified camera positions
      return;
    
    LeapDeviceInfo device = handController.GetDeviceInfo ();
    if (device.type == LeapDeviceType.Invalid)
      return;
    
    Vector3 addIPD = 0.5f * oculusIPD.normalized * (tweenPosition * device.baseline + (1f - tweenPosition) * oculusIPD.magnitude);
    Vector3 toDevice = tweenPosition * centerEye.forward * device.focalPlaneOffset;
    centerEye.position = centerEye.position + toDevice;
    leftEye.position = centerEye.position - addIPD;
    rightEye.position = centerEye.position + addIPD;
  }

  bool HasNaN(Vector3 v) {
    return float.IsNaN (v.x) || float.IsNaN (v.y) || float.IsNaN (v.z);
  }

  bool HasNaN(Quaternion q) {
    return float.IsNaN (q.w) || float.IsNaN (q.x) || float.IsNaN (q.y) || float.IsNaN (q.z);
  }
}
