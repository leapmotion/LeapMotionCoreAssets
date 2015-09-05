using UnityEngine;
using UnityEngine.VR;
using System.Collections;
using System.Collections.Generic;
using Leap;

/// <summary>
/// Implements spatial alignment of cameras and synchronization with images
/// </summary>
public class LeapCameraAlignment : MonoBehaviour {
  [SerializeField]
  protected LeapImageRetriever imageRetriever;
  [SerializeField]
  protected HandController handController;

  [Header("Alignment Targets")]
  [SerializeField]
  protected Transform leftCamera;
  [SerializeField]
  protected Transform rightCamera;
  [SerializeField]
  protected Transform centerCamera;
  [HideInInspector]
  public List<LeapImageBasedMaterial> warpedImages;

  [Header("Counter-Aligned Targets")]
  public List<Transform> counterAligned;

  [Header("Alignment Settings")]

  [Range(0,2)]
  public float tweenRewind = 0f;
  [Range(0,2)]
  public float tweenTimeWarp = 0f;
  [Range(0,2)]
  public float tweenPosition = 1f;
  [Range(0,2)]
  public float tweenForward = 1f;
  
  // Manual Time Alignment
  [SerializeField]
  protected KeyCode unlockHold = KeyCode.RightShift;
  [SerializeField]
  protected KeyCode moreRewind = KeyCode.LeftArrow;
  [SerializeField]
  protected KeyCode lessRewind = KeyCode.RightArrow;
  [System.NonSerialized]
  public float rewindAdjust = 0f; //Frame fraction

  // Spatial recalibration
  [SerializeField]
  protected KeyCode recenter = KeyCode.R;

  // Automatic Time Alignment
  public float latencySmoothing = 1f; //State delay in seconds
  [System.NonSerialized]
  public SmoothedFloat frameLatency;
  [System.NonSerialized]
  public SmoothedFloat imageLatency;

  // HACK: Non-peripheral devices sometimes self-identify as peripherals
  public bool overrideDeviceType = false;
  public LeapDeviceType overrideDeviceTypeWith = LeapDeviceType.Invalid;

  protected enum VRCameras {
    NONE = 0,
    CENTER = 1,
    LEFT_RIGHT = 2
  }
  protected VRCameras hasCameras = VRCameras.NONE;

  protected struct UserEyeAlignment {
    public bool use;
    public float ipd;
    public float eyeDepth;
    public float eyeHeight;
  }

  protected UserEyeAlignment eyeAlignment;

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

  private long lastFrame = 0;
  private long maxLatency = 200000; //microseconds
  protected List<TransformData> history;
  
  protected LeapDeviceInfo deviceInfo;
  
  /// <summary>
  /// Estimates the transform of this gameObject at the specified time
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
    if (history [0].leapTime >= time) {
      // Expect this when using LOW LATENCY image retrieval, which can yield negative latency estimates due to incorrect clock synchronization
      //if (history [0].leapTime > time) Debug.LogWarning("NO INTERPOLATION: Using earliest time = " + history[0].leapTime + " > time = " + time);
      return history[0];
    }
    int t = 1;
    while (t < history.Count &&
           history[t].leapTime <= time) {
      t++;
    }
    if (!(t < history.Count)) {
      // Expect this for initial frames which will have a very low frame rate
      if (history[history.Count - 1].leapTime < time) Debug.LogWarning("NO INTERPOLATION: Using most recent time = " + history[history.Count - 1].leapTime + " < time = " + time);
      return history[history.Count-1];
    }
    
    return TransformData.Lerp (history[t-1], history[t], time);
  }

  /// <summary>
  /// Rewinds position of target relative to most recent point in history
  /// </summary>
  /// <remarks>
  /// This method applies the same time difference logic using for time alignment,
  /// but ignores the tweening settings
  /// </remarks>
  /// <param name="isLeftCenterRight">
  /// Applies a left camera alignment if < 0,
  /// applies a right camera alignment if > 0, 
  /// and applies no alignment if == 0.
  /// </param>
  public void RelativeRewind(Transform target, int isLeftCenterRight = 0) {
    TransformData past = TransformAtTime(imageRetriever.ImageNow () - (long)(rewindAdjust*frameLatency.value));
    
    // Rewind position and rotation
    target.rotation = past.rotation;
    target.position = past.position + past.rotation * Vector3.forward * deviceInfo.focalPlaneOffset;

    if (isLeftCenterRight < 0) {
      // Apply the left camera alignment
      target.position += past.rotation * Vector3.left * deviceInfo.baseline * 0.5f;
    }
    if (isLeftCenterRight > 0) {
      // Apply the right camera alignment
      target.position += past.rotation * Vector3.right * deviceInfo.baseline * 0.5f;
    }
  }

  void Start () {
    if (handController == null ||
        imageRetriever == null) {
      Debug.LogWarning ("HandController and ImageRetriever references cannot be null -> enabled = false");
      enabled = false;
      return;
    }

    hasCameras = VRCameras.NONE;
    if (centerCamera != null) {
      Camera center = centerCamera.GetComponent<Camera>();
      if (center != null && center.isActiveAndEnabled) {
        hasCameras = VRCameras.CENTER;
      }
    }
    if (hasCameras == VRCameras.NONE) {
      Camera left = leftCamera.GetComponent<Camera>();
      Camera right = rightCamera.GetComponent<Camera>();
      if (left != null && left.isActiveAndEnabled &&
          right != null && right.isActiveAndEnabled) {
        hasCameras = VRCameras.LEFT_RIGHT;
      }
    }
    if (hasCameras == VRCameras.NONE) {
      Debug.LogWarning ("Either a central Camera for both eyes, or a Left and Right cameras must be referenced -> enabled = false");
      enabled = false;
      return;
    }

    if (transform.parent == null) {
      Debug.LogWarning ("Alignment requires a parent object to define the location of the player in the world. enabled -> false");
      enabled = false;
      return;
    }

    if (transform != leftCamera.parent ||
        transform != centerCamera.parent ||
        transform != rightCamera.parent) {
      Debug.LogWarning ("LeapCameraAlignment must be a component of the parent of the camera tranasforms -> enabled = false");
      enabled = false;
      return;
    }

    deviceInfo = (overrideDeviceType) ? new LeapDeviceInfo(overrideDeviceTypeWith) : handController.GetDeviceInfo ();
    if (deviceInfo.type == LeapDeviceType.Invalid) {
      Debug.LogWarning ("Invalid Leap Device -> enabled = false");
      enabled = false;
      return;
    }

    if (VRDevice.isPresent &&
        VRSettings.loadedDevice == VRDeviceType.Oculus) {
      eyeAlignment = new UserEyeAlignment() {
        use = true,
        ipd = OVRPlugin.ipd,
        eyeDepth = OVRPlugin.eyeDepth,
        eyeHeight = OVRPlugin.eyeHeight
      };
      Debug.Log ("Unity VR Support with Oculus");
    } else {
      eyeAlignment = new UserEyeAlignment() {
        use = false,
        ipd = 0f,
        eyeDepth = 0f,
        eyeHeight = 0f
      };
      Debug.Log ("Two-camera stereoscopic alignment");
    }
    
    history = new List<TransformData> ();
    imageLatency = new SmoothedFloat () {
      delay = latencySmoothing
    };
    frameLatency = new SmoothedFloat () {
      delay = latencySmoothing
    };
  }

  void Update() {
    if (unlockHold == KeyCode.None ||
        Input.GetKey (unlockHold)) {
      // Manual Time Alignment
      if (Input.GetKeyDown (moreRewind)) {
        rewindAdjust += 0.1f;
      }
      if (Input.GetKeyDown (lessRewind)) {
        rewindAdjust -= 0.1f;
      }
    }
    
    if (Input.GetKeyDown (recenter)) {
      InputTracking.Recenter();
    }
  }
	
	// IMPORTANT: This method MUST be called after OVRManager.LateUpdate.
  // FIXME Use EnableUpdateOrdering script to ensure correct call order -> Declare relative script ordering
  void LateUpdate() {
    if (!(IsFinite (leftCamera.position) && IsFinite (leftCamera.rotation) &&
          IsFinite (centerCamera.position) && IsFinite (centerCamera.rotation) &&
          IsFinite (rightCamera.position) && IsFinite (rightCamera.rotation))) {
      // Uninitialized camera positions
      Debug.LogWarning ("Uninitialized transforms -> skip alignment");
      return;
    }

    // IMPORTANT: UpdateHistory must happen first, before any transforms are modified.
    UpdateHistory ();

    // IMPORTANT: UpdateAlignment must precede UpdateTimeWarp,
    // since UpdateTimeWarp applies warping relative current positions
    UpdateAlignment ();
    UpdateTimeWarp ();
  }
  
  void UpdateHistory () {
    if (eyeAlignment.use) {
      // Revert the tracking space transform
      transform.localPosition = Vector3.zero;
      transform.localRotation = Quaternion.identity;
      transform.localScale = Vector3.one;
    }
    
    // Add current position and rotation to history
    // NOTE: history.Add can be retrieved as history[history.Count-1]
    if (history.Count >= 1) {
      lastFrame = history [history.Count - 1].leapTime;
    } else {
      lastFrame = 0;
    }
    long timeFrame = imageRetriever.LeapNow ();
    switch (hasCameras) {
    case VRCameras.CENTER:
      history.Add (new TransformData () {
        leapTime = timeFrame,
        position = centerCamera.position,
        rotation = centerCamera.rotation
      });
      break;
    case VRCameras.LEFT_RIGHT:
      history.Add (new TransformData () {
        leapTime = timeFrame,
        position = Vector3.Lerp (leftCamera.position, rightCamera.position, 0.5f), 
        rotation = Quaternion.Slerp (leftCamera.rotation, rightCamera.rotation, 0.5f)
      });
      break;
    default: //case VRCameras.NONE:
      history.Add (new TransformData () {
        leapTime = timeFrame,
        position = Vector3.zero,
        rotation = Quaternion.identity
      });
      break;
    }

    // Update smoothed averages of latency and frame rate
    long deltaFrame = timeFrame - lastFrame;
    long deltaImage = timeFrame - imageRetriever.ImageNow ();
    if (deltaFrame + deltaImage < maxLatency) {
      frameLatency.Update ((float)deltaFrame, Time.deltaTime);
      imageLatency.Update ((float)deltaImage, Time.deltaTime);
      //Debug.Log ("Leap deltaTime = " + ((float)deltaTime / 1000f) + " ms");
      //Debug.Log ("Unity deltaTime = " + (Time.deltaTime * 1000f) + " ms");
      // RESULT: Leap & Unity deltaTime measurements are consistent within error tolerance.
      // Leap deltaTime will be used, since it references the same clock as images.
    } else {
      // Expect high latency during initial frames or after pausing
      //Debug.Log("Maximum latency exceeded: " + ((float)(deltaFrame + deltaImage) / 1000f) + " ms -> reset latency estimates");
      frameLatency.value = 0f;
      imageLatency.value = 0f;
      frameLatency.reset = true;
      imageLatency.reset = true;
    }
    
    // Reduce history length
    while (history.Count > 0 &&
           maxLatency < timeFrame - history [0].leapTime) {
      //Debug.Log ("Removing oldest from history.Count = " + history.Count);
      history.RemoveAt(0);
    }
  }

  void ApplyRescale(float rescale) {
    // Rescale this object, thereby rescaling the virtual camera separation
    transform.localScale = Vector3.one * rescale;

    // Move this object to compensate for the rescaling of head movement
    Vector3 cameraScaledPosition = Vector3.zero;
    switch (hasCameras) {
    case VRCameras.CENTER:
      cameraScaledPosition = centerCamera.position;
      break;
    case VRCameras.LEFT_RIGHT:
      cameraScaledPosition = Vector3.Lerp (leftCamera.position, rightCamera.position, 0.5f) - transform.position;
      break;
    default: //case VRCameras.NONE:
      break;
    }
    cameraScaledPosition = transform.parent.InverseTransformVector(cameraScaledPosition);
    transform.localPosition = cameraScaledPosition * (1f / rescale - 1f);

    // Apply the inverse scale to child objects such as the hand controller
    Vector3 counterScale = Vector3.one / rescale;
    foreach (Transform child in counterAligned) {
      child.localScale = counterScale;
    }
  }
  
  void UpdateAlignment () {
    long latestTime = history [history.Count - 1].leapTime;
    long rewindTime = imageRetriever.ImageNow () - (long)frameLatency.value - (long)(rewindAdjust*frameLatency.value);
    long tweenAddition = (long)((1f - tweenRewind) * (float)(latestTime - rewindTime));
    TransformData past = TransformAtTime(rewindTime + tweenAddition);

    if (!eyeAlignment.use) {
      // Derive eye alignment from the left & right cameras
      Vector3 virtualBaseline = leftCamera.position - rightCamera.position;
      if (!(IsFinite (virtualBaseline) &&
        virtualBaseline.magnitude > float.Epsilon)) {
        // Unmodified camera positions
        Debug.LogWarning ("Bad camera separation = " + virtualBaseline + " -> skip alignment");
        eyeAlignment.ipd = 0f;
        return;
      }
      eyeAlignment.ipd = virtualBaseline.magnitude;
    }
    
    float separate = (tweenPosition * deviceInfo.baseline + (1f - tweenPosition) * eyeAlignment.ipd);
    float forward = tweenPosition * tweenForward * deviceInfo.focalPlaneOffset;
    
    if (!eyeAlignment.use) {
      // Move Virtual cameras to align position & orientation
      centerCamera.rotation = past.rotation;
      centerCamera.position = past.position + centerCamera.forward * forward;
      leftCamera.position = centerCamera.position - centerCamera.right * separate * 0.5f;
      leftCamera.rotation = past.rotation;
      rightCamera.position = centerCamera.position + centerCamera.right * separate * 0.5f;
      rightCamera.rotation = past.rotation;
    } else {
      // Rescale and apply compensating displacement
      ApplyRescale(separate / eyeAlignment.ipd);

      // Rewind
      TransformData latestTransform = history[history.Count - 1];
      Quaternion rewindRotate = past.rotation * Quaternion.Inverse(latestTransform.rotation);
      Vector3 rewindDisplace = latestTransform.position - rewindRotate*latestTransform.position;
      rewindDisplace += past.position - latestTransform.position;
      Debug.Log ("rewindRotation = " + rewindRotate);
      Debug.Log ("rewindDisplacement = " + rewindDisplace);

      // PROBLEM: None of the methods below work
      // GUESS: The local transform is updated relative to this...
      // therefore this transformation needs to be inverted.
      // GOAL: The local transformations should be recorded relative to the PARENT
      // so that the compensating motion is NOT included in the history
      // QUESTION: How were transformations defined in the old system? Relative to the parent?

      transform.localRotation = transform.parent.rotation*rewindRotate*Quaternion.Inverse(transform.parent.rotation);
      transform.localPosition += transform.parent.InverseTransformVector(rewindDisplace);

      //transform.rotation = rewindRotate * transform.rotation;
      //transform.position = rewindDisplace + transform.position;

      //FIXME: The past adjustment must be applied to this transform
      switch (hasCameras) {
      case VRCameras.CENTER:
        // Shift forward
        transform.position += centerCamera.forward * forward;

        // Align non-tracked cameras
        leftCamera.position = centerCamera.position - centerCamera.right * separate * 0.5f;
        leftCamera.rotation = centerCamera.rotation;
        rightCamera.position = centerCamera.position + centerCamera.right * separate * 0.5f;
        rightCamera.rotation = centerCamera.rotation;
        break;
      case VRCameras.LEFT_RIGHT:
        Vector3 centerPosition = Vector3.Lerp(leftCamera.position, rightCamera.position, 0.5f);
        Quaternion centerRotation = Quaternion.Slerp(leftCamera.rotation, rightCamera.rotation, 0.5f);

        // Shift forward
        Vector3 moveForward = centerRotation * Vector3.forward * forward;
        transform.position += moveForward;

        // Align non-tracked camera
        centerCamera.position = centerPosition + moveForward;
        centerCamera.rotation = centerRotation;
        break;
      }
    }
  }
  
  void UpdateTimeWarp () {
    long latestTime = history [history.Count - 1].leapTime;
    long rewindTime = imageRetriever.ImageNow () - (long)(rewindAdjust*frameLatency.value);
    long tweenAddition = (long)((1f - tweenTimeWarp) * (float)(latestTime - rewindTime));
    TransformData past = TransformAtTime(rewindTime + tweenAddition);

    // Apply only a rotation ~ assume all objects are infinitely distant
    Quaternion rotateImageToNow = centerCamera.rotation * Quaternion.Inverse(past.rotation);
    Matrix4x4 ImageToNow = Matrix4x4.TRS (Vector3.zero, centerCamera.rotation * Quaternion.Inverse(past.rotation), Vector3.one);
    
    foreach (LeapImageBasedMaterial image in warpedImages) {
      image.GetComponent<Renderer>().material.SetMatrix("_ViewerImageToNow", ImageToNow);
    }
    centerCamera.localRotation = Quaternion.Inverse(rotateImageToNow) * centerCamera.localRotation;
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
