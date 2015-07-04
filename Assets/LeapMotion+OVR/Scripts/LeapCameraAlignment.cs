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
  
  public float latencySmoothing = 1f; //State delay in seconds
  public SmoothedFloat leapLatency;
  public SmoothedFloat imageLatency;

  [Header("Alignment Targets")]
  public LeapImageRetriever leftImages;
  public LeapImageRetriever rightImages;
  public HandController handController;
  
  [Header("Target History (micro-seconds)")]
  public long maxLatency = 100000; //microseconds

  LeapDeviceInfo deviceInfo;
  private long timeFrame = 0;
  private long lastFrame = 0;
  private Vector3 virtualCameraStereo;
  
  //DEBUG
  public float ovrLatency = 0;
  public KeyCode moreRewind = KeyCode.LeftArrow;
  public KeyCode lessRewind = KeyCode.RightArrow;
  public long rewindAdjust = 0;

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
      enabled = false;
      return;
    }
    
    deviceInfo = handController.GetDeviceInfo ();
    if (deviceInfo.type == LeapDeviceType.Invalid) {
      Debug.LogWarning ("Invalid Leap Device");
      enabled = false;
      return;
    }

    history = new List<TransformData> ();
    imageLatency = new SmoothedFloat () {
      delay = latencySmoothing
    };
    leapLatency = new SmoothedFloat () {
      delay = latencySmoothing
    };
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

    virtualCameraStereo = rightImages.transform.position - leftImages.transform.position;
    if (!(IsFinite (virtualCameraStereo.magnitude) &&
          virtualCameraStereo.magnitude > float.Epsilon)) {
      // Unmodified camera positions
      Debug.LogWarning ("Bad virtualCameraStereo = " + virtualCameraStereo);
      return;
    }

    //DEBUG
    if (Input.GetKeyDown (moreRewind)) {
      rewindAdjust += 1000; //ms
    }
    if (Input.GetKeyDown (lessRewind)) {
      rewindAdjust -= 1000; //ms
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
    lastFrame = timeFrame;
    timeFrame = leftImages.LeapNow ();
    history.Add (new TransformData () {
      leapTime = timeFrame,
      position = transform.position,
      rotation = transform.rotation
    });
    //Debug.Log ("Last Index Time = " + history[history.Count-1].leapTime + " =? " + timeFrame);
    // NOTE: history.Add can be retrieved as history[history.Count-1]
    
    // Reduce history length
    while (history.Count > 0 &&
           maxLatency < timeFrame - history [0].leapTime) {
      //Debug.Log ("Removing oldest from history.Count = " + history.Count);
      history.RemoveAt(0);
    }
  }
  
  void UpdateTimeWarp () {
    // DEBUG
    if (OVRManager.display != null) {
      ovrLatency = OVRManager.display.latency.render;
    } else {
      ovrLatency = 0f;
    }

    long deltaFrame = timeFrame - lastFrame;
    long deltaImage = timeFrame - leftImages.ImageNow ();
    if (2 * (deltaFrame + deltaImage) < maxLatency) {
      imageLatency.Update ((float)deltaImage, Time.deltaTime);
      leapLatency.Update ((float)(deltaFrame + deltaImage), Time.deltaTime);
      //Debug.Log ("Leap deltaTime = " + ((float)deltaTime / 1000f) + " ms");
      //Debug.Log ("Unity deltaTime = " + (Time.deltaTime * 1000f) + " ms");
      // RESULT: Leap & Unity deltaTime measurements are consistent.
      // Leap deltaTime will be used, since it references the same clock as images.
    } else {
      // High latency during initial frames
      Debug.LogWarning ("Maximum latency exceeded: " + ((float)deltaFrame / 1000f) + " ms");
      leapLatency.value = ((float) maxLatency) / 1000f;
      leapLatency.reset = true;
    }

    long rewindTime = leftImages.ImageNow () - 2 * (long)(leapLatency.value) - rewindAdjust;
    long tweenAddition = (long)((1f - tweenTimeWarp) * (float)(timeFrame - rewindTime));
    TransformData past = TransformAtTime(rewindTime + tweenAddition);

    // Move Virtual cameras to synchronize position & orientation
    float virtualCameraRadius = 0.5f * virtualCameraStereo.magnitude;
    handController.transform.parent.position = past.position;
    handController.transform.parent.rotation = past.rotation;
    rightImages.transform.position = handController.transform.parent.position + virtualCameraRadius * handController.transform.parent.right;
    rightImages.transform.rotation = past.rotation;
    leftImages.transform.position = handController.transform.parent.position - virtualCameraRadius * handController.transform.parent.right;
    leftImages.transform.rotation = past.rotation;
  }

  void UpdateAlignment () {
    Vector3 addIPD = 0.5f * virtualCameraStereo.normalized * (tweenPosition * deviceInfo.baseline + (1f - tweenPosition) * virtualCameraStereo.magnitude);
    Vector3 toDevice = tweenPosition * handController.transform.parent.forward * deviceInfo.focalPlaneOffset * tweenForward;
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
