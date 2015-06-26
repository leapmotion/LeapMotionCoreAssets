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
  public LeapImageRetriever leftImages;
  public LeapImageRetriever rightImages;
  public HandController handController;

  // FIXME: This should be determined dynamically
  // or should be a fixed size to avoid allocation
  public long historyTime = 1000000; //microseconds

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
      Debug.LogWarning ("NO INTERPOLATION: Using earliest time = " + history [0].leapTime + " > time = " + time);
      return history[0];
    }
    int t = 1;
    while (t < history.Count &&
           history[t].leapTime < time) {
      t++;
    }
    if (!(t < history.Count)) {
      Debug.LogWarning ("NO INTERPOLATION: Using most recent time = " + history[history.Count-1].leapTime + " < time = " + time);
      return history[history.Count-1];
    }
    
    return TransformData.Lerp (history[t-1], history[t], time);
  }

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
        leftImages.transform == null ||
        rightImages.transform == null ||
        leftImages == null ||
        rightImages == null) {
      Debug.Log ("Hand Controller & ImageRetriever references cannot be null");
      return;
    }

    if (!(IsFinite (leftImages.transform.position) && IsFinite (leftImages.transform.rotation) &&
          IsFinite (handController.transform.position) && IsFinite (handController.transform.rotation) &&
          IsFinite (rightImages.transform.position) && IsFinite (rightImages.transform.rotation))) {
      Debug.Log ("Uninitialized transforms -> skip alignment");
      return;
    }

    UpdateHistory ();
    UpdateTimeWarp ();
    UpdateAlignment ();
  }
  
  void UpdateHistory () {
    // ASSUME: Oculus resets relative camera positions in each frame
    // Append latest position & rotation of stereo camera rig
    long now = leftImages.LeapNow ();
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
    float virtualCameraRadius = 0.5f * (rightImages.transform.position - leftImages.transform.position).magnitude;
    if (!(IsFinite (virtualCameraRadius) &&
          virtualCameraRadius > float.Epsilon)) {
      // Unmodified camera positions
      Debug.LogWarning ("Bad virtualCameraRadius = " + virtualCameraRadius);
      return;
    }

    //Debug.Log ("TimeWarp: now = " + history [history.Count - 1].leapTime + " -> warp = " + (history [history.Count - 1].leapTime - leftImages.ImageNow ()));
    long tweenAddition = (long)((1f - tweenTimeWarp) * (float)(history[history.Count-1].leapTime - leftImages.ImageNow ()));
    //Debug.Log ("tweenAddition = " + tweenAddition);

    // HACK:
    tweenAddition = leftImages.LeapNow () - leftImages.ImageNow () - 20000;

    TransformData past = TransformAtTime(leftImages.ImageNow () + tweenAddition);

    // Move Virtual cameras to synchronize position & orientation
    handController.transform.position = past.position;
    handController.transform.rotation = past.rotation;
    rightImages.transform.position = handController.transform.position + virtualCameraRadius * handController.transform.right;
    rightImages.transform.rotation = past.rotation;
    leftImages.transform.position = handController.transform.position - virtualCameraRadius * handController.transform.right;
    leftImages.transform.rotation = past.rotation;
  }

  void UpdateAlignment () {
    Vector3 virtualCameraStereo = rightImages.transform.position - leftImages.transform.position;
    if (!(IsFinite (virtualCameraStereo.magnitude) &&
          virtualCameraStereo.magnitude > float.Epsilon)) {
      // Unmodified camera positions
      Debug.LogWarning ("Bad virtualCameraStereo = " + virtualCameraStereo);
      return;
    }
    
    LeapDeviceInfo device = handController.GetDeviceInfo ();
    if (device.type == LeapDeviceType.Invalid) {
      Debug.LogWarning ("Invalid Leap Device");
      return;
    }
    
    Vector3 addIPD = 0.5f * virtualCameraStereo.normalized * (tweenPosition * device.baseline + (1f - tweenPosition) * virtualCameraStereo.magnitude);
    Vector3 toDevice = tweenPosition * handController.transform.forward * device.focalPlaneOffset;
    handController.transform.position = handController.transform.position + toDevice;
    leftImages.transform.position = handController.transform.position - addIPD;
    rightImages.transform.position = handController.transform.position + addIPD;
  }

  bool IsFinite(float f) {
    return !(float.IsInfinity (f) || float.IsNaN (f));
  }

  bool IsFinite(Vector3 v) {
    return IsFinite (v.x) && IsFinite (v.y) && IsFinite (v.z);
  }

  bool IsFinite(Quaternion q) {
    return IsFinite (q.w) && IsFinite (q.x) && IsFinite (q.y) && IsFinite (q.z);
  }

  bool IsFinite(TransformData t) {
    return IsFinite (t.position) && IsFinite (t.rotation);
  }
}
