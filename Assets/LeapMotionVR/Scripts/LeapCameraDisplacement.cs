using UnityEngine;
using UnityEngine.VR;
using System;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class LeapCameraDisplacement : MonoBehaviour {

  //Called whenever the center camera is assigned it's final transform for the current frame.
  public static event Action<Transform> OnFinalCenterCamera;
  private static bool _hasDispatchedFinalCenterCameraEvent = false;

  [SerializeField]
  private EyeType _eyeType;

  [SerializeField]
  private bool _overrideEyePosition = true;

  public bool OverrideEyePosition { get { return _overrideEyePosition; } set { _overrideEyePosition = value; } }

  private Camera _cachedCamera;
  private Camera _camera {
    get {
      if (_cachedCamera == null) {
        _cachedCamera = GetComponent<Camera>();
      }
      return _cachedCamera;
    }
  }

  private Matrix4x4 _finalCenterMatrix;
  private LeapDeviceInfo _deviceInfo;
  private int _preRenderIndex = 0;

#if UNITY_EDITOR
  void Reset() {
    _eyeType = new EyeType(gameObject.name);
  }
#endif

  void Start() {
    _deviceInfo = new LeapDeviceInfo(LeapDeviceType.Dragonfly);
  }

  void Update() {
    _hasDispatchedFinalCenterCameraEvent = false;
  }

  void OnPreCull() {
#if UNITY_EDITOR
    if (!Application.isPlaying) {
      return;
    }
#endif

    _preRenderIndex = 0;
    _camera.ResetWorldToCameraMatrix();
    _finalCenterMatrix = _camera.worldToCameraMatrix;

    if (!_hasDispatchedFinalCenterCameraEvent && OnFinalCenterCamera != null) {
      OnFinalCenterCamera(transform);
      _hasDispatchedFinalCenterCameraEvent = true;
    }
  }

  void OnPreRender() {
#if UNITY_EDITOR
    if (!Application.isPlaying) {
      return;
    }
#endif

    _eyeType.BeginCamera();
    _preRenderIndex++;

    Matrix4x4 offsetMatrix;

    if (_overrideEyePosition) {
      offsetMatrix = _finalCenterMatrix;
      Vector3 ipdOffset = (_eyeType.IsLeftEye ? 1 : -1) * transform.right * _deviceInfo.baseline * 0.5f;
      Vector3 forwardOffset = -transform.forward * _deviceInfo.focalPlaneOffset;
      offsetMatrix *= Matrix4x4.TRS(ipdOffset, Quaternion.identity, Vector3.one);
      offsetMatrix *= Matrix4x4.TRS(forwardOffset, Quaternion.identity, Vector3.one);
    } else {
      offsetMatrix = _camera.worldToCameraMatrix;
    }

    _camera.worldToCameraMatrix = offsetMatrix;
  }
}
