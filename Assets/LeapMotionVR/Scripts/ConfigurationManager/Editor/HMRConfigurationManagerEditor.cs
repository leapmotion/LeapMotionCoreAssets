using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(HMRConfigurationManager))]
public class HMRConfigurationManagerEditor : Editor {
  private LeapTemporalWarping _cachedTemporalWarping;
  private HMRConfigurationManager _manager;

  private LeapTemporalWarping _aligner {
    get {
      if (_cachedTemporalWarping == null) {
        _cachedTemporalWarping = _manager.GetComponentInChildren<LeapTemporalWarping>();

        if (_cachedTemporalWarping == null) {
          throw new UnityEngine.MissingComponentException("Cannot find LeapCameraAlignment component in children of " + _manager.gameObject.name);
        }
      }

      return _cachedTemporalWarping;
    }
  }

  private GameObject _backgroundQuad {
    get {
      GameObject backgroundQuad = _manager._backgroundQuad; 

      if (backgroundQuad == null) {
        throw new System.NullReferenceException("The _backgroundQuad field on " + ((HMRConfigurationManager)target).gameObject.name + " is null.");
      }

      return backgroundQuad;
    }
  }

  private HandController _handController {
    get {
      HandController handController = _manager._handController;

      if (handController == null) {
        throw new System.NullReferenceException("The _handController field on " + ((HMRConfigurationManager)target).gameObject.name + " is null.");
      }

      return handController;
    }
  }

  private IEnumerable<Camera> vrCameras {
    get{
      foreach (Camera childCamera in _manager.GetComponentsInChildren<Camera>()) {
        if (childCamera.enabled && childCamera.targetTexture != null) {
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

  private IEnumerable<LeapCameraDisplacement> cameraDisplacements {
    get {
      foreach (Camera vrCamera in vrCameras) {
        LeapCameraDisplacement retriever = vrCamera.GetComponent<LeapCameraDisplacement>();
        if (retriever == null) {
          retriever = vrCamera.gameObject.AddComponent<LeapCameraDisplacement>();
        }
        yield return retriever;
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
    LMHeadMountedRigConfiguration configuration = LMHeadMountedRigConfiguration.Deserialize(serializedConfiguration);

    setBackgroundQuadEnabled(configuration.EnableBackgroundQuad);
    setGraphicsModels(configuration.LeftHandGraphicsModel, configuration.RightHandGraphicsModel);
    setImageRetrieversEnabled(configuration.EnableImageRetrievers);
    setCameraClearFlags(configuration.CameraClearFlags);
    setTimewarp(configuration.TweenTimewarp);
    setOverrideEyes(configuration.OverrideEyePos);

    Debug.Log("Switched to configuration: " + configuration.ConfigurationName);
  }

  private void setBackgroundQuadEnabled(bool enabled) {
    Renderer backgroundQuadRenderer = _backgroundQuad.GetComponent<Renderer>();

    if (backgroundQuadRenderer == null) {
      Debug.LogWarning("The object " + _backgroundQuad.gameObject.name + " is missing a Renderer.");
      return;
    }

    updateValue(backgroundQuadRenderer, backgroundQuadRenderer.enabled, enabled, v => backgroundQuadRenderer.enabled = v);
  }

  private void setGraphicsModels(HandModel leftHandGraphicsModel, HandModel rightHandGraphicsModel) {
    updateValue(_handController, _handController.leftGraphicsModel, leftHandGraphicsModel, v => _handController.leftGraphicsModel = v);
    updateValue(_handController, _handController.rightGraphicsModel, rightHandGraphicsModel, v => _handController.rightGraphicsModel = v);
  }

  private void setImageRetrieversEnabled(bool enabled) {
    foreach (LeapImageRetriever retriever in imageRetrievers) {
      updateValue(retriever, retriever.enabled, enabled, e => retriever.enabled = e);
    }
  }

  private void setCameraClearFlags(CameraClearFlags cameraClearFlags) {
    foreach (Camera camera in vrCameras) {
      updateValue(camera, camera.clearFlags, cameraClearFlags, v => camera.clearFlags = v);
    }
  }

  private void setTimewarp(float value) {
    updateValue(_aligner, _aligner.TweenTimeWarp, value, v => _aligner.TweenTimeWarp = v);
  }

  private void setOverrideEyes(bool overrideEyes) {
    foreach (LeapCameraDisplacement displacement in cameraDisplacements) {
      updateValue(displacement, displacement.OverrideEyePosition, overrideEyes, v => displacement.OverrideEyePosition = v);
    }
  }

  private void updateValue<T>(UnityEngine.Object obj, T currValue, T destValue, Action<T> setter) {
    if (!currValue.Equals(destValue)) {
      EditorUtility.SetDirty(obj);
      setter(destValue);
    }
  }
}
