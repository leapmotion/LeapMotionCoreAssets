using UnityEngine;
using UnityEngine.VR;
using System;
using System.Collections;
using System.Collections.Generic;
using Leap;

/// <summary>
/// Implements spatial alignment of cameras and synchronization with images
/// </summary>
public class LeapCameraAlignment : MonoBehaviour {

  private const long MAX_LATENCY = 200000;

  public enum RewindAnchor {
    CENTER,
    LEFT,
    RIGHT,
  }

  protected struct UserEyeAlignment {
    public bool use;
    public float ipd;
    public float eyeDepth;
    public float eyeHeight;
  }

  protected struct TransformData {
    public long leapTime; // microseconds
    public Vector3 localPosition; //meters
    public Quaternion localRotation;

    public static TransformData Lerp(TransformData from, TransformData to, long time) {
      if (from.leapTime == to.leapTime) {
        return from;
      }
      float fraction = (float)(time - from.leapTime) / (float)(to.leapTime - from.leapTime);
      return new TransformData() {
        leapTime = time,
        localPosition = Vector3.Lerp(from.localPosition, to.localPosition, fraction),
        localRotation = Quaternion.Slerp(from.localRotation, to.localRotation, fraction)
      };
    }
  }

  // Spatial recalibration
  [SerializeField]
  private KeyCode recenter = KeyCode.R;

  [Tooltip("Transforms that should be counter-rotated to match with the Time warp that is applied.  These should be direct children of the VR tracking space.")]
  [SerializeField]
  private Transform[] counterWarped;

  [Tooltip("Allows smooth enabling or disabling of the Time-warp feature.  Feature is completely enabled at 1, and completely disabled at 0.")]
  [Range(0, 1)]
  [SerializeField]
  private float tweenTimeWarp = 0f;

  // Manual Time Alignment
  [Tooltip("Allow manual adjustment of the rewind time.")]
  [SerializeField]
  private bool allowManualTimeAlignment;

  [SerializeField]
  private KeyCode unlockHold = KeyCode.RightShift;

  [SerializeField]
  private KeyCode moreRewind = KeyCode.LeftArrow;

  [SerializeField]
  private KeyCode lessRewind = KeyCode.RightArrow;

  private HandController handController;
  private LeapDeviceInfo deviceInfo;
  private UserEyeAlignment eyeAlignment;

  private List<TransformData> history = new List<TransformData>();
  private long rewindAdjust = 0; //Miliseconds

  private long getLatestImageTimestamp() {
    using (ImageList list = handController.GetFrame().Images) {
      if (list.Count > 0) {
        using (Image image = list[0]) {
          return image.Timestamp;
        }
      } else {
        Debug.LogWarning("Could not find any images!");
        return 0;
      }
    }
  }

  /// <summary>
  /// Provides the position of a Leap Anchor at the time the current Images were taken.
  /// </summary>
  public void GetRewoundTransform(RewindAnchor anchor, out Vector3 rewoundLocalPosition, out Quaternion rewoundLocalRotation) {
    TransformData past = TransformAtTime(getLatestImageTimestamp() - rewindAdjust);

    // Rewind position and rotation
    rewoundLocalRotation = past.localRotation;
    rewoundLocalPosition = past.localPosition + past.localRotation * Vector3.forward * deviceInfo.focalPlaneOffset;

    switch (anchor) {
      case RewindAnchor.CENTER: return;
      case RewindAnchor.LEFT:
        rewoundLocalPosition += past.localRotation * Vector3.left * deviceInfo.baseline * 0.5f;
        return;
      case RewindAnchor.RIGHT:
        rewoundLocalPosition += past.localRotation * Vector3.right * deviceInfo.baseline * 0.5F;
        return;
      default:
        throw new Exception("Unexpected Rewind Type " + anchor);
    }
  }

  protected void Start() {
    HandController[] allControllers = FindObjectsOfType<HandController>();
    foreach (HandController controller in allControllers) {
      if (controller.isActiveAndEnabled) {
        handController = controller;
        break;
      }
    }

    if (handController == null) {
      Debug.LogWarning("Camera alignment requires an active HandController in the scene -> enabled = false");
      enabled = false;
      return;
    }

    deviceInfo = handController.GetDeviceInfo();
    if (deviceInfo.type == LeapDeviceType.Invalid) {
      Debug.LogWarning("Invalid Leap Device -> enabled = false");
      enabled = false;
      return;
    }

    disallowPeripheralTimewarp();
  }

  protected void Update() {
    if (Input.GetKeyDown(recenter)) {
      InputTracking.Recenter();
    }

    // Manual Time Alignment
    if (allowManualTimeAlignment) {
      if (unlockHold == KeyCode.None || Input.GetKey(unlockHold)) {
        if (Input.GetKeyDown(moreRewind)) {
          rewindAdjust += 1;
        }
        if (Input.GetKeyDown(lessRewind)) {
          rewindAdjust -= 1;
        }
      }
    }
  }

  //We use LateUpdate because it is the last time we can modify the transforms of objects
  protected void LateUpdate() {
    UpdateHistory();
    UpdateTimeWarp();
  }

  private void UpdateHistory() {
    // Add current position and rotation to history
    // NOTE: history.Add can be retrieved as history[history.Count-1]
    long lastFrame = 0;
    if (history.Count >= 1) {
      lastFrame = history[history.Count - 1].leapTime;
    }

    long leapNow = handController.GetLeapController().Now();

    history.Add(new TransformData() {
      leapTime = leapNow,
      localPosition = InputTracking.GetLocalPosition(VRNode.CenterEye),
      localRotation = InputTracking.GetLocalRotation(VRNode.CenterEye)
    });

    // Reduce history length
    while (history.Count > 0 &&
           MAX_LATENCY < leapNow - history[0].leapTime) {
      history.RemoveAt(0);
    }
  }

  private void UpdateTimeWarp() {
    long latestTime = history[history.Count - 1].leapTime;
    long rewindTime = getLatestImageTimestamp() - rewindAdjust;
    long lerpedTime = longLerp(latestTime, rewindTime, tweenTimeWarp);
    TransformData past = TransformAtTime(lerpedTime);

    // Apply only a rotation ~ assume all objects are infinitely distant
    Quaternion rotateImageToNow = InputTracking.GetLocalRotation(VRNode.CenterEye) * Quaternion.Inverse(past.localRotation);
    Matrix4x4 ImageToNow = Matrix4x4.TRS(Vector3.zero, rotateImageToNow, Vector3.one);

    Shader.SetGlobalMatrix("_LeapGlobalViewerImageToNow", ImageToNow);

    // Counter-rotate objects to align with Time Warp
    foreach (Transform child in counterWarped) {
      child.localRotation = Quaternion.Inverse(rotateImageToNow);
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

  /// <summary>
  /// Estimates the transform of this gameObject at the specified time
  /// </summary>
  /// <returns>
  /// A transform with leapTime == time only if interpolation was possible
  /// </returns>
  private TransformData TransformAtTime(long time) {
    if (history.Count == 0) {
      return new TransformData() {
        leapTime = 0,
        localPosition = Vector3.zero,
        localRotation = Quaternion.identity
      };
    }

    if (history[0].leapTime >= time) {
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
      return history[history.Count - 1];
    }

    return TransformData.Lerp(history[t - 1], history[t], time);
  }

  private long longLerp(long a, long b, float percent) {
    return a + (long)((b - a) * percent);
  }
}
