using UnityEngine;
using UnityEngine.VR;
using System.Collections;
using System.Collections.Generic;
using Leap;

/// <summary>
/// Implements spatial alignment of cameras and synchronization with images
/// </summary>
public class LeapCameraAlignment : MonoBehaviour {
  protected LeapImageRetriever imageRetriever;
  protected HandController handController;

  // Spatial recalibration
  [Header("HMD Realignment")]
  [SerializeField]
  protected KeyCode recenter = KeyCode.R;

  [AdvancedModeOnly]
  public Transform[] rewoundTransforms;

  [Header("Alignment Settings (Advanced Mode)")]

  [Range(0,1)]
  [AdvancedModeOnly]
  public float tweenTimeWarp = 0f;
  
  // Manual Time Alignment
  [SerializeField]
  [AdvancedModeOnly]
  private bool _allowManualTimeAlignment;
  [SerializeField]
  [AdvancedModeOnly]
  protected KeyCode unlockHold = KeyCode.RightShift;
  [SerializeField]
  [AdvancedModeOnly]
  protected KeyCode moreRewind = KeyCode.LeftArrow;
  [SerializeField]
  [AdvancedModeOnly]
  protected KeyCode lessRewind = KeyCode.RightArrow;
  //[System.NonSerialized]
  public float rewindAdjust = 0f; //Frame fraction

  [System.Serializable]
  public class AdvancedOptions {
    public int advancedOptionSecretInt = 100;
  }

  [SerializeField]
  private AdvancedOptions advancedOptions;

  // Automatic Time Alignment
  [AdvancedModeOnly]
  public float latencySmoothing = 1f; //State delay in seconds
  [System.NonSerialized]
  public SmoothedFloat frameLatency;
  [System.NonSerialized]
  public SmoothedFloat imageLatency;

  // HACK: Non-peripheral devices sometimes self-identify as peripherals
  [AdvancedModeOnly]
  public bool overrideDeviceType = false;
  [AdvancedModeOnly]
  public LeapDeviceType overrideDeviceTypeWith = LeapDeviceType.Invalid;

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

  [System.NonSerialized]
  public List<LeapImageBasedMaterial> warpedImages;
  private long lastFrame = 0;
  private long maxLatency = 200000; //microseconds
  protected List<TransformData> history;
  
  protected LeapDeviceInfo deviceInfo;
  protected Quaternion _lateUpdateRotation;

  private long _latestImageTimestamp {
    get {
      if (imageRetriever != null) {
        return imageRetriever.ImageNow();
      } else if (handController != null) {
        ImageList images = handController.GetFrame().Images;
        if (images.Count > 0) {
          return images[0].Timestamp;
        }
      }

      Debug.LogWarning("Could not calculate valid timestamp. Returning 0.");
      return 0;
    }
  }
  
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
      //if (history[history.Count - 1].leapTime < time) Debug.LogWarning("NO INTERPOLATION: Using most recent time = " + history[history.Count - 1].leapTime + " < time = " + time);
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
    TransformData past = TransformAtTime(_latestImageTimestamp - (long)(rewindAdjust * frameLatency.value));
    
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

  void Awake () {
    warpedImages = new List<LeapImageBasedMaterial>();

    history = new List<TransformData> ();
    imageLatency = new SmoothedFloat () {
      delay = latencySmoothing
    };

    frameLatency = new SmoothedFloat () {
      delay = latencySmoothing
    };

    LeapCameraCorrection.OnCameraFinalTransform += OnCameraFinalTransform;
  }

  void Start () {
    HandController[] allControllers = FindObjectsOfType<HandController> ();
    foreach (HandController controller in allControllers) {
      if (controller.isActiveAndEnabled) {
        handController = controller;
      }
    }
    if (handController == null) {
      Debug.LogWarning ("Camera alignment requires an active HandController in the scene -> enabled = false");
      enabled = false;
      return;
    }

    LeapImageRetriever[] allRetrievers = FindObjectsOfType<LeapImageRetriever> ();
    foreach (LeapImageRetriever retriever in allRetrievers) {
      if (retriever.isActiveAndEnabled) {
        imageRetriever = retriever;
      }
    }

    if (transform.parent == null) {
      Debug.LogWarning ("Alignment requires a parent object to define the location of the player in the world. enabled -> false");
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
  }

  void Update() {
    disallowPeripheralTimewarp();

    if (_allowManualTimeAlignment) {
      if (unlockHold == KeyCode.None ||
          Input.GetKey(unlockHold)) {
        // Manual Time Alignment
        if (Input.GetKeyDown(moreRewind)) {
          rewindAdjust += 0.1f;
        }
        if (Input.GetKeyDown(lessRewind)) {
          rewindAdjust -= 0.1f;
        }
      }
    }
    
    if (Input.GetKeyDown (recenter)) {
      InputTracking.Recenter();
    }
  }

  /// <summary>
  /// Temporary solution until timecodes on peripheral is fixed.
  /// </summary>
  private void disallowPeripheralTimewarp() {
    DeviceList devices = handController.GetLeapController().Devices;
    if (devices.Count > 0 && devices[0].Type == Device.DeviceType.TYPE_PERIPHERAL) {
      tweenTimeWarp = 0;
    }
  }

  void LateUpdate() {
    long rewindTime = _latestImageTimestamp - (long)(rewindAdjust * frameLatency.value);
    TransformData past = TransformAtTime(rewindTime);

    _lateUpdateRotation = past.rotation;

    foreach (Transform t in rewoundTransforms) {
      t.transform.position = past.position;
      t.transform.rotation = past.rotation;
    }
  }

  void OnCameraFinalTransform(Transform centerTransform) {

    // IMPORTANT: UpdateHistory must happen first, before any transforms are modified.
    UpdateHistory (centerTransform);

    // IMPORTANT: UpdateAlignment must precede UpdateTimeWarp,
    // since UpdateTimeWarp applies warping relative current positions
    UpdateTimeWarp(centerTransform);
  }
  
  void UpdateHistory (Transform centerTransform) {
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

    long timeFrame = handController.GetLeapController().Now();
    history.Add (new TransformData () {
        leapTime = timeFrame,
        position = centerTransform.position,
        rotation = centerTransform.rotation
    });

    // Update smoothed averages of latency and frame rate
    long deltaFrame = timeFrame - lastFrame;
    long deltaImage = timeFrame - _latestImageTimestamp;
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

  void UpdateTimeWarp (Transform centerTransform) {
    // Apply only a rotation ~ assume all objects are infinitely distant
    Quaternion rotateImageToNow = centerTransform.rotation * Quaternion.Inverse(_lateUpdateRotation);
    //Quaternion rotateImageToNow = _lateUpdateRotation * Quaternion.Inverse(centerTransform.rotation);
    Matrix4x4 ImageToNow = Matrix4x4.TRS(Vector3.zero, rotateImageToNow, Vector3.one);
    
    foreach (LeapImageBasedMaterial image in warpedImages) {
      image.GetComponent<Renderer>().material.SetMatrix("_ViewerImageToNow", ImageToNow);
    }
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
