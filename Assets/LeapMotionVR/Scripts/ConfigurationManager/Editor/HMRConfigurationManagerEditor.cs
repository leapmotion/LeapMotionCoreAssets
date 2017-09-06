using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(HMRConfigurationManager))]
public class HMRConfigurationManagerEditor : Editor {
  private HMRConfigurationManager _manager;
  private string[] _configNames;

  private LeapVRTemporalWarping _aligner {
    get {
      if (_manager._aligner == null) {
        Debug.LogWarning("Cannot _aligner component on " + _manager.gameObject.name + " is null.");
      }

      return _manager._aligner;
    }
  }

  private GameObject _backgroundQuad {
    get {
      if (_manager._backgroundQuad == null) {
        Debug.LogWarning("The _backgroundQuad field on " + _manager.name + " is null.");
        return null;
      }

      return _manager._backgroundQuad;
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

  private IEnumerable<LeapVRCameraControl> cameraControls {
    get {
      foreach (Camera vrCamera in vrCameras) {
        LeapVRCameraControl displacement = vrCamera.GetComponent<LeapVRCameraControl>();
        if (displacement == null) {
          displacement = vrCamera.gameObject.AddComponent<LeapVRCameraControl>();
        }
        yield return displacement;
      }
    }
  }

  public void OnEnable() {
    _manager = target as HMRConfigurationManager;

    SerializedProperty configArray = serializedObject.FindProperty("_headMountedConfigurations");
    _configNames = new string[configArray.arraySize];
    for (int i = 0; i < configArray.arraySize; i++) {
      _configNames[i] = configArray.GetArrayElementAtIndex(i).FindPropertyRelative("_configurationName").stringValue;
    }
  }

  public override void OnInspectorGUI() {
    serializedObject.Update();

    EditorGUI.BeginChangeCheck();

    SerializedProperty configIndexProp = serializedObject.FindProperty("_configurationIndex");
    configIndexProp.intValue = EditorGUILayout.Popup("Selected Configuration", configIndexProp.intValue, _configNames);

    SerializedProperty configArray = serializedObject.FindProperty("_headMountedConfigurations");

    if (configArray.arraySize == 0) {
      Debug.LogWarning("HMR Configuration Manager has no configurations!");
      return;
    }

    if (configIndexProp.intValue >= configArray.arraySize) {
      Debug.LogWarning("HMR Configuration Index was out of bounds!  Reseting configuration to default.");
      configIndexProp.intValue = 0;
      serializedObject.ApplyModifiedProperties();
      return;
    }

    SerializedProperty configProp = configArray.GetArrayElementAtIndex(configIndexProp.intValue);
    LMHeadMountedRigConfiguration config = LMHeadMountedRigConfiguration.Deserialize(configProp);

    if (EditorGUI.EndChangeCheck()) {
      serializedObject.ApplyModifiedProperties();
      applySelectedConfiguration(config);
    }

    EditorGUILayout.Space();
    if (GUILayout.Button("Reapply Selected Configuration")) {
      applySelectedConfiguration(config);
    }
    EditorGUILayout.Space();

    if (config.ShowHandGraphicField) {
      EditorGUILayout.LabelField("Hands to use (References Hand Controller)", EditorStyles.boldLabel);
      EditorGUI.BeginChangeCheck();
      _manager._handController.leftGraphicsModel = (HandModel)EditorGUILayout.ObjectField("Left Hand Graphics Model", _manager._handController.leftGraphicsModel, typeof(HandModel), true);
      _manager._handController.rightGraphicsModel = (HandModel)EditorGUILayout.ObjectField("Right Hand Graphics Model", _manager._handController.rightGraphicsModel, typeof(HandModel), true);
      if (EditorGUI.EndChangeCheck()) {
        EditorUtility.SetDirty(_manager._handController);
      }
    }
  }

  private void applySelectedConfiguration(LMHeadMountedRigConfiguration config) {
    //Update background quad
    updateGameobject(_backgroundQuad, config.EnableBackgroundQuad);

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
    foreach (LeapVRCameraControl cameraControl in cameraControls) {
      updateValue(cameraControl, cameraControl.OverrideEyePosition, config.OverrideEyePos, v => cameraControl.OverrideEyePosition = v);
    }

    Debug.Log("Switched to configuration: " + config.ConfigurationName);
  }

  private void updateGameobject(GameObject obj, bool active) {
    if (obj.activeSelf != active) {
      obj.SetActive(active);
    }
  }

  private void updateValue<T>(UnityEngine.Object obj, T currValue, T destValue, Action<T> setter) {
    if (!currValue.Equals(destValue)) {
      EditorUtility.SetDirty(obj);
      setter(destValue);
    }
  }
}
