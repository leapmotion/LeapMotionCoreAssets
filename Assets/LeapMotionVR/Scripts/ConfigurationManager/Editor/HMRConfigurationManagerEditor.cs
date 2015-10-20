using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(HMRConfigurationManager))]
public class HMRConfigurationManagerEditor : Editor {
  private HMRConfigurationManager _manager;

  private LeapTemporalWarping _aligner {
    get {
      if (_manager._aligner == null) {
        Debug.LogWarning("Cannot _aligner component on " + _manager.gameObject.name + " is null.");
      }

      return _manager._aligner;
    }
  }

  private Renderer _backgroundQuadRenderer {
    get {
      if (_manager._backgroundQuad == null) {
        Debug.LogWarning("The _backgroundQuad field on " + _manager.name + " is null.");
        return null;
      }

      Renderer backgroundQuadRenderer = _manager._backgroundQuad.GetComponent<Renderer>();

      if (backgroundQuadRenderer == null) {
        Debug.LogWarning("The object " + _manager._backgroundQuad.gameObject.name + " is missing a Renderer.");
        return null;
      }

      return backgroundQuadRenderer;
    }
  }

  private HandController _handController {
    get {
      if (_manager._handController == null) {
        Debug.LogWarning("The _handController field on " + _manager.name + " is null.");
      }

      return _manager._handController;
    }
  }

  private IEnumerable<Camera> vrCameras {
    get{
      foreach (Camera childCamera in _manager.GetComponentsInChildren<Camera>()) {
        if (childCamera.enabled && childCamera.targetTexture == null) {
          yield return childCamera;
        }
      }
    }
  }

  private IEnumerable<LeapImageRetriever> imageRetrievers {
    get {
      foreach (Camera vrCamera in vrCameras) {
        LeapImageRetriever retriever = vrCamera.GetComponent<LeapImageRetriever>();
        if (retriever == null) {
          retriever = vrCamera.gameObject.AddComponent<LeapImageRetriever>();
        }
        yield return retriever;
      }
    }
  }

  private IEnumerable<LeapCameraControl> cameraDisplacements {
    get {
      foreach (Camera vrCamera in vrCameras) {
        LeapCameraControl displacement = vrCamera.GetComponent<LeapCameraControl>();
        if (displacement == null) {
          displacement = vrCamera.gameObject.AddComponent<LeapCameraControl>();
        }
        yield return displacement;
      }
    }
  }

  public void OnEnable() {
    _manager = target as HMRConfigurationManager;
  }

  public override void OnInspectorGUI() {
    serializedObject.Update();
    _manager.validateConfigurationsLabeled();
    EditorGUI.BeginChangeCheck();
    EditorGUILayout.PropertyField(serializedObject.FindProperty("_configuration"), new GUIContent("Selected Configuration"));
    serializedObject.ApplyModifiedProperties();
    if (EditorGUI.EndChangeCheck()) {
      applySelectedConfiguration();
    }
    EditorGUILayout.Space();
    if (GUILayout.Button("Reapply Selected Configuration")) {
      applySelectedConfiguration();
    }
    EditorGUILayout.Space();
    int selectedConfigurationIndex = serializedObject.FindProperty("_configuration").enumValueIndex;
    HMRConfigurationManager.HMRConfiguration selectedConfiguration = (HMRConfigurationManager.HMRConfiguration)selectedConfigurationIndex;

    if (selectedConfiguration == HMRConfigurationManager.HMRConfiguration.VR_WORLD_VR_HANDS) {
      EditorGUILayout.LabelField("Hands to use for VR Hands (References Hand Controller)", EditorStyles.boldLabel);
      EditorGUI.BeginChangeCheck();
      _manager._handController.leftGraphicsModel = (HandModel)EditorGUILayout.ObjectField("Left Hand Graphics Model", _manager._handController.leftGraphicsModel, typeof(HandModel), true);
      _manager._handController.rightGraphicsModel = (HandModel)EditorGUILayout.ObjectField("Right Hand Graphics Model", _manager._handController.rightGraphicsModel, typeof(HandModel), true);
      if (EditorGUI.EndChangeCheck()) {
        EditorUtility.SetDirty(_manager._handController.leftGraphicsModel);
        EditorUtility.SetDirty(_manager._handController.rightGraphicsModel);
      }
    }
  }

  private void applySelectedConfiguration() {
    int selectedConfigurationIndex = serializedObject.FindProperty("_configuration").enumValueIndex;
    SerializedProperty serializedConfiguration = serializedObject.FindProperty("_headMountedConfigurations").GetArrayElementAtIndex((int)selectedConfigurationIndex);
    LMHeadMountedRigConfiguration config = LMHeadMountedRigConfiguration.Deserialize(serializedConfiguration);

    //Update background quad
    updateValue(_backgroundQuadRenderer, _backgroundQuadRenderer.enabled, config.EnableBackgroundQuad, v => _backgroundQuadRenderer.enabled = v);

    //Set graphical models
    updateValue(_handController, _handController.leftGraphicsModel, config.LeftHandGraphicsModel, v => _handController.leftGraphicsModel = v);
    updateValue(_handController, _handController.rightGraphicsModel, config.RightHandGraphicsModel, v => _handController.rightGraphicsModel = v);

    //Enable/Disable image retrievers
    foreach (LeapImageRetriever retriever in imageRetrievers) {
      updateValue(retriever, retriever.enabled, config.EnableImageRetrievers, e => retriever.enabled = e);
    }

    //Update camera clear flags
    foreach (Camera camera in vrCameras) {
      updateValue(camera, camera.clearFlags, config.CameraClearFlags, v => camera.clearFlags = v);
    }

    //Update temporal alignment script
    updateValue(_aligner, _aligner.TweenImageWarping, config.TweenImageWarping, v => _aligner.TweenImageWarping = v);
    updateValue(_aligner, _aligner.TweenRotationalWarping, config.TweenRotationalWarping, v => _aligner.TweenRotationalWarping = v);
    updateValue(_aligner, _aligner.TweenPositionalWarping, config.TweenPositionalWarping, v => _aligner.TweenPositionalWarping = v);
    updateValue(_aligner, _aligner.TemporalSyncMode, config.TemporalSynMode, v => _aligner.TemporalSyncMode = v);

    //Update Override Eye Position
    foreach (LeapCameraControl displacement in cameraDisplacements) {
      updateValue(displacement, displacement.OverrideEyePosition, config.OverrideEyePos, v => displacement.OverrideEyePosition = v);
    }

    Debug.Log("Switched to configuration: " + config.ConfigurationName);
  }

  private void updateValue<T>(UnityEngine.Object obj, T currValue, T destValue, Action<T> setter) {
    if (!currValue.Equals(destValue)) {
      EditorUtility.SetDirty(obj);
      setter(destValue);
    }
  }
}
