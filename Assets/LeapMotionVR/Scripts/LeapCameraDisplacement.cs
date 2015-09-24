using UnityEngine;
using UnityEngine.VR;
using System;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class LeapCameraDisplacement : MonoBehaviour {

  [SerializeField]
  private LeapImageRetriever.EYE _eye = LeapImageRetriever.EYE.RIGHT;

  [SerializeField]
  private bool _overrideIPD = true;

  [SerializeField]
  private bool _pushForward = true;

  public bool OverrideIPD { get { return _overrideIPD; } set { _overrideIPD = value; } }
  public bool PushForward { get { return _pushForward; } set { _pushForward = value; } }

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
    string lowercaseName = gameObject.name.ToLower();
    if (lowercaseName.Contains("left")) {
      _eye = LeapImageRetriever.EYE.LEFT;
    } else if (lowercaseName.Contains("right")) {
      _eye = LeapImageRetriever.EYE.RIGHT;
    } else {
      _eye = LeapImageRetriever.EYE.LEFT_TO_RIGHT;
    }
  }
#endif

  void Start() {
    _deviceInfo = new LeapDeviceInfo(LeapDeviceType.Dragonfly);
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
  }

  void OnPreRender() {
#if UNITY_EDITOR
    if (!Application.isPlaying) {
      return;
    }
#endif

    bool isLeft;
    if (_eye == LeapImageRetriever.EYE.LEFT) {
      isLeft = true;
    } else if (_eye == LeapImageRetriever.EYE.RIGHT) {
      isLeft = false;
    } else if (_eye == LeapImageRetriever.EYE.LEFT_TO_RIGHT) {
      isLeft = _preRenderIndex == 0;
    } else if (_eye == LeapImageRetriever.EYE.RIGHT_TO_LEFT) {
      isLeft = _preRenderIndex == 1;
    } else {
      throw new Exception("Unexpected EYE " + _eye);
    }
    _preRenderIndex++;

    Matrix4x4 offsetMatrix;

    if (_overrideIPD) {
      offsetMatrix = _finalCenterMatrix;
      Vector3 ipdOffset = (isLeft ? 1 : -1) * transform.right * _deviceInfo.baseline * 0.5f;
      offsetMatrix *= Matrix4x4.TRS(ipdOffset, Quaternion.identity, Vector3.one);
    } else {
      offsetMatrix = _camera.worldToCameraMatrix;
    }

    if (_pushForward) {
      Vector3 forwardOffset = -transform.forward * _deviceInfo.focalPlaneOffset;
      offsetMatrix *= Matrix4x4.TRS(forwardOffset, Quaternion.identity, Vector3.one);
    }

    _camera.worldToCameraMatrix = offsetMatrix;
  }
}
