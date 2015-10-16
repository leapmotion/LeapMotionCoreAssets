using UnityEngine;
using UnityEngine.VR;
using System;
using System.Collections;
using System.Collections.Generic;
using Leap;

/// <summary>
/// Implements spatial alignment of cameras and synchronization with images
/// </summary>
public class LeapTemporalWarping : MonoBehaviour {

  private const long MAX_LATENCY = 200000;

  public enum WarpedAnchor {
    CENTER,
    LEFT,
    RIGHT,
  }

  protected struct TransformData {
    public long leapTime; // microseconds
    public Vector3 localPosition; //meters
    public Quaternion localRotation; //magic

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

  private LeapDeviceInfo deviceInfo;

  private List<TransformData> history = new List<TransformData>();
  private long rewindAdjust = 0; //Microseconds

  public float TweenTimeWarp {
    get {
      return tweenTimeWarp;
    }
    set {
      tweenTimeWarp = Mathf.Clamp01(value);
    }
  }

  private bool tryLatestImageTimestamp(out long timestamp) {
    using (ImageList list = HandController.Main.GetFrame().Images) {
      if (list.Count > 0) {
        using (Image image = list[0]) {
          timestamp = image.Timestamp;
          return true;
        }
      } else {
        timestamp = 0;
        return false;
      }
    }
  }

  /// <summary>
  /// Provides the position of a Leap Anchor at a given Leap Time.  Cannot extrapolate.
  /// </summary>
  public void GetWarpedTransform(WarpedAnchor anchor, out Vector3 rewoundLocalPosition, out Quaternion rewoundLocalRotation, long leapTime) {
    TransformData past = TransformAtTime(leapTime);

    // Rewind position and rotation
    rewoundLocalRotation = past.localRotation;
    rewoundLocalPosition = past.localPosition + past.localRotation * Vector3.forward * deviceInfo.focalPlaneOffset;

    switch (anchor) {
      case WarpedAnchor.CENTER: return;
      case WarpedAnchor.LEFT:
        rewoundLocalPosition += past.localRotation * Vector3.left * deviceInfo.baseline * 0.5f;
        return;
      case WarpedAnchor.RIGHT:
        rewoundLocalPosition += past.localRotation * Vector3.right * deviceInfo.baseline * 0.5F;
        return;
      default:
        throw new Exception("Unexpected Rewind Type " + anchor);
    }
  }

  
  public bool TryGetWarpedTransform(WarpedAnchor anchor, out Vector3 rewoundLocalPosition, out Quaternion rewoundLocalRotation) {
    long timestamp;
    if (tryLatestImageTimestamp(out timestamp)) {
      GetWarpedTransform(anchor, out rewoundLocalPosition, out rewoundLocalRotation, timestamp - rewindAdjust);
      return true;
    }
    rewoundLocalPosition = Vector3.zero;
    rewoundLocalRotation = Quaternion.identity;
    return false;
  }

  protected void Start() {
    if (HandController.Main == null) {
      Debug.LogWarning("Camera alignment requires an active main HandController in the scene -> enabled = false");
      enabled = false;
      return;
    }

    LeapCameraDisplacement.OnFinalCenterCamera += onFinalCenterCamera;

    deviceInfo = HandController.Main.GetDeviceInfo();
    if (deviceInfo.type == LeapDeviceType.Invalid) {
      Debug.LogWarning("Invalid Leap Device -> enabled = false");
      enabled = false;
      return;
    }
  }

  protected void Update() {
    if (Input.GetKeyDown(recenter)) {
      InputTracking.Recenter();
    }

    // Manual Time Alignment
    if (allowManualTimeAlignment) {
      if (unlockHold == KeyCode.None || Input.GetKey(unlockHold)) {
        if (Input.GetKeyDown(moreRewind)) {
          rewindAdjust += 1000;
        }
        if (Input.GetKeyDown(lessRewind)) {
          rewindAdjust -= 1000;
        }
      }
    }
  }

  protected void LateUpdate() {
    updateTimeWarp(InputTracking.GetLocalRotation(VRNode.CenterEye));
  }

  private void onFinalCenterCamera(Transform centerCamera) {
    updateHistory();
    updateTimeWarp(InputTracking.GetLocalRotation(VRNode.CenterEye));
  }

  private void updateHistory() {
    long leapNow = HandController.Main.GetLeapController().Now();
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

  private void updateTimeWarp(Quaternion centerEyeRotation) {
    //Get the transform at the time when the latest image was captured
    long rewindTime;
    if (!tryLatestImageTimestamp(out rewindTime)) {
      return;
    }

    TransformData past = TransformAtTime(rewindTime);

    //Apply only a rotation ~ assume all objects are infinitely distant
    Quaternion referenceRotation = Quaternion.Slerp(centerEyeRotation, past.localRotation, tweenTimeWarp);
    Quaternion rotateImageToNow = centerEyeRotation * Quaternion.Inverse(referenceRotation);

    Matrix4x4 ImageToNow = Matrix4x4.TRS(Vector3.zero, rotateImageToNow, Vector3.one);

    Shader.SetGlobalMatrix("_LeapGlobalViewerImageToNow", ImageToNow);

    transform.rotation = referenceRotation;
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
