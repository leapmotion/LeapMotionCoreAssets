using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap;

public class LeapCameraAlignment : MonoBehaviour {
  [Range(0,1)]
  public float tweenPosition = 1f;
  [Range(0,1)]
  public float tweenTimeWarp = 1f;
  [Range(0,1)]
  public float tweenForward = 1f;

  [Header("Alignment Targets")]
  public LeapImageRetriever leftImages;
  public LeapImageRetriever rightImages;
  public HandController handController;
  
  //DEBUG
  public float ovrLatency = 0;
  private SmoothedFloat leapLatencySmoothed;
  public float latencyDelay = 1f;
  public float leapLatency {
    get {
      return leapLatencySmoothed.value;
    }
  }
  private long frameTime = 0;
  private long lastFrameTime = 0;
  public KeyCode moreRewind = KeyCode.LeftArrow;
  public KeyCode lessRewind = KeyCode.RightArrow;
  public long rewind = 0;

  // FIXME: This should be determined dynamically
  // or should be a fixed size to avoid allocation
  public long historyTime = 1000000; //microseconds
  
  [HideInInspector]
  public SmoothedFloat imageLatency;
  [SerializeField]
  private float imageLatencyDelay = 1f;

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
    imageLatency = new SmoothedFloat () {
      delay = imageLatencyDelay
    };

    leapLatencySmoothed = new SmoothedFloat ();
    leapLatencySmoothed.delay = latencyDelay;
  }
	
  // FIXME: This should be attached to cameras & use OnPreCull
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
      Debug.LogWarning ("Hand Controller & ImageRetriever references cannot be null");
      return;
    }

    if (!(IsFinite (leftImages.transform.position) && IsFinite (leftImages.transform.rotation) &&
          IsFinite (handController.transform.parent.position) && IsFinite (handController.transform.parent.rotation) &&
          IsFinite (rightImages.transform.position) && IsFinite (rightImages.transform.rotation))) {
      Debug.LogWarning ("Uninitialized transforms -> skip alignment");
      return;
    }

    //DEBUG
    if (Input.GetKeyDown (moreRewind)) {
      rewind += 1000; //ms
    }
    if (Input.GetKeyDown (lessRewind)) {
      rewind -= 1000; //ms
    }
    if (Input.GetKeyDown (KeyCode.Alpha0)) {
      tweenTimeWarp = 0f;
    }
    if (Input.GetKeyDown (KeyCode.Alpha1)) {
      tweenTimeWarp = 1f;
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
    //Debug.Log ("LeapNow(micro) = " + now + " - ImageNow(micro) = " + leftImages.ImageNow () + " = Latency(micro) = " + (now - leftImages.ImageNow ()));
    
    // Reduce history length
    while (history.Count > 0 &&
           historyTime < now - history [0].leapTime) {
      //Debug.Log ("Removing oldest from history.Count = " + history.Count);
      history.RemoveAt(0);
    }
  }
  
  void UpdateTimeWarp () {
    if (OVRManager.display != null) {
      ovrLatency = OVRManager.display.latency.render;
    } else {
      ovrLatency = 0f;
    }
    lastFrameTime = frameTime;
    frameTime = leftImages.LeapNow ();
    if (lastFrameTime > 0) {
      leapLatencySmoothed.Update ((float)(frameTime - lastFrameTime) / 1000f, Time.deltaTime);
    }
    
    float virtualCameraRadius = 0.5f * (rightImages.transform.position - leftImages.transform.position).magnitude;
    if (!(IsFinite (virtualCameraRadius) &&
          virtualCameraRadius > float.Epsilon)) {
      // Unmodified camera positions
      Debug.LogWarning ("Bad virtualCameraRadius = " + virtualCameraRadius);
      return;
    }

    long imageDiff = history [history.Count - 1].leapTime - leftImages.ImageNow ();
    imageLatency.Update ((float)imageDiff / 1000f, Time.deltaTime);
    Debug.Log ("OVR rewindTime adjust = " + (long)(ovrLatency * 2e6));
    Debug.Log ("LEAP rewindTime adjust = " + 2 * (imageDiff + (frameTime - lastFrameTime)));
    long rewindTime = leftImages.ImageNow () - 2 * (imageDiff + (frameTime - lastFrameTime)) - rewind;
    //long imageTime = leftImages.ImageNow () - (long)(ovrLatency * 2e6) - rewind;
    long tweenAddition = (long)((1f - tweenTimeWarp) * (float)(history[history.Count-1].leapTime - rewindTime));
    TransformData past = TransformAtTime(rewindTime + tweenAddition);

    // Move Virtual cameras to synchronize position & orientation
    handController.transform.parent.position = past.position;
    handController.transform.parent.rotation = past.rotation;
    rightImages.transform.position = handController.transform.parent.position + virtualCameraRadius * handController.transform.parent.right;
    rightImages.transform.rotation = past.rotation;
    leftImages.transform.position = handController.transform.parent.position - virtualCameraRadius * handController.transform.parent.right;
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
    Vector3 toDevice = tweenPosition * handController.transform.parent.forward * device.focalPlaneOffset * tweenForward;
    handController.transform.parent.position = handController.transform.parent.position + toDevice;
    leftImages.transform.position = handController.transform.parent.position - addIPD;
    rightImages.transform.position = handController.transform.parent.position + addIPD;
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
