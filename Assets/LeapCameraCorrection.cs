using UnityEngine;
using UnityEngine.VR;
using System;
using System.Linq;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class LeapCameraCorrection : MonoBehaviour {

  public static event Action<Transform> OnCameraFinalTransform;

  [SerializeField]
  private LeapImageRetriever.EYE _eye = LeapImageRetriever.EYE.RIGHT;

  [SerializeField]
  private bool _overrideIPD = false;

  [SerializeField]
  private bool _pushForward = false;

  private Camera _cachedCamera;
  private Camera _camera {
    get {
      if (_cachedCamera == null) {
        _cachedCamera = GetComponent<Camera>();
      }
      return _cachedCamera;
    }
  }

  private bool _hasLaunchedFinalTransformEvent = false;
  private Matrix4x4 _finalCenterMatrix;
  private LeapDeviceInfo _deviceInfo;

#if UNITY_EDITOR
  void Reset() {
    _eye = gameObject.name.ToLower().Contains("left") ? LeapImageRetriever.EYE.LEFT : LeapImageRetriever.EYE.RIGHT;
  }

  void OnValidate() {
    foreach (LeapCameraCorrection corrector in FindObjectsOfType<LeapCameraCorrection>()) {
      corrector._overrideIPD = _overrideIPD;
      corrector._pushForward = _pushForward;
      UnityEditor.EditorUtility.SetDirty(corrector);
    }
  }
#endif

  void Awake() {
    HandController controller = FindObjectsOfType<HandController>().FirstOrDefault(h => h.isActiveAndEnabled);
    if (controller == null) {
      enabled = false;
      return;
    }

    _deviceInfo = controller.GetDeviceInfo();
  }

  void Update() {
    if (Input.GetKeyDown(KeyCode.A)) {
      _overrideIPD = !_overrideIPD;
      _pushForward = !_pushForward;
    }

    _hasLaunchedFinalTransformEvent = false;
  }

  void OnPreCull() {
#if UNITY_EDITOR
    if (!Application.isPlaying) {
      return;
    }
#endif

    _camera.ResetWorldToCameraMatrix();
    _finalCenterMatrix = _camera.worldToCameraMatrix;

    if (!_hasLaunchedFinalTransformEvent && OnCameraFinalTransform != null) {
      OnCameraFinalTransform(transform);
      _hasLaunchedFinalTransformEvent = true;
    }
  }

  void OnPreRender() {
#if UNITY_EDITOR
    if (!Application.isPlaying) {
      return;
    }
#endif

    Matrix4x4 offsetMatrix;
    
    if(_overrideIPD){
      offsetMatrix = _finalCenterMatrix;
      Vector3 ipdOffset = (_eye == LeapImageRetriever.EYE.RIGHT ? -1 : 1) * transform.right * _deviceInfo.baseline * 0.5f;
      offsetMatrix *= Matrix4x4.TRS(ipdOffset, Quaternion.identity, Vector3.one);
    } else{
      offsetMatrix = _camera.worldToCameraMatrix;
    }

    if (_pushForward) {
      Vector3 forwardOffset = -transform.forward * _deviceInfo.focalPlaneOffset;
      offsetMatrix *= Matrix4x4.TRS(forwardOffset, Quaternion.identity, Vector3.one);
    }

    _camera.worldToCameraMatrix = offsetMatrix;
  }
}
