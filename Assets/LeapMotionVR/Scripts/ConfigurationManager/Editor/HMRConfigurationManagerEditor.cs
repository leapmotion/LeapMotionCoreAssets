using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(HMRConfigurationManager))]
public class HMRConfigurationManagerEditor : Editor {
  private LeapCameraAlignment _cachedAligner;

  private LeapCameraAlignment _aligner {
    get {
      if (_cachedAligner == null) {
        _cachedAligner = ((HMRConfigurationManager)target).GetComponentInChildren<LeapCameraAlignment>();

        if (_cachedAligner == null) {
          throw new UnityEngine.MissingComponentException("Cannot find LeapCameraAlignment component in children of " + ((HMRConfigurationManager)target).gameObject.name);
        }
      }

      return _cachedAligner;
    }
  }

  private GameObject _backgroundQuad {
    get {
      GameObject backgroundQuad = ((HMRConfigurationManager)target)._backgroundQuad; 

      if (backgroundQuad == null) {
        throw new System.NullReferenceException("The _backgroundQuad field on " + ((HMRConfigurationManager)target).gameObject.name + " is null.");
      }

      return backgroundQuad;
    }
  }

  private HandController _handController {
    get {
      HandController handController = ((HMRConfigurationManager)target)._handController;

      if (handController == null) {
        throw new System.NullReferenceException("The _handController field on " + ((HMRConfigurationManager)target).gameObject.name + " is null.");
      }

      return handController;
    }
  }

  private Camera getCameraObjectForEye(UnityEngine.VR.VRNode cameraNode) {
    Camera camera;

    switch (cameraNode) {
      case UnityEngine.VR.VRNode.CenterEye:
      case UnityEngine.VR.VRNode.Head:
        camera = ((HMRConfigurationManager)target)._centerCamera;
        break;
      case UnityEngine.VR.VRNode.LeftEye:
        camera = ((HMRConfigurationManager)target)._leftCamera;
        break;
      case UnityEngine.VR.VRNode.RightEye:
        camera = ((HMRConfigurationManager)target)._rightCamera;
        break;
      default:
        throw new System.ArgumentOutOfRangeException("No understoof VRNode provided.");
    }

    if (camera == null) {
      throw new System.NullReferenceException("The camera reference for the " + cameraNode.ToString() + "is missing on " + ((HMRConfigurationManager)target).gameObject.name);
    }

    return camera;
  }

  private LeapImageRetriever getImageRetreiverForEye(UnityEngine.VR.VRNode eyeNode) {
    Camera cameraForEye = getCameraObjectForEye(eyeNode);
    LeapImageRetriever imageRetrieverForEye = cameraForEye.gameObject.GetComponent<LeapImageRetriever>();

    if (cameraForEye == null) {
      throw new System.NullReferenceException("Could not resolve the camera for the given eye: " + eyeNode.ToString());
    }

    if (imageRetrieverForEye == null) {
      throw new UnityEngine.MissingComponentException("Could not find LeapImageRetriever component adjacent to camera on " + cameraForEye.gameObject.name + " for the given eye: " + eyeNode.ToString());
    }

    return imageRetrieverForEye;
  }

  public override void OnInspectorGUI() {
    HMRConfigurationManager configManager = (HMRConfigurationManager)target;
    configManager.validateConfigurationsLabeled();
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
      configManager._handController.leftGraphicsModel = (HandModel)EditorGUILayout.ObjectField("Left Hand Graphics Model", configManager._handController.leftGraphicsModel, typeof(HandModel), false);
      configManager._handController.rightGraphicsModel = (HandModel)EditorGUILayout.ObjectField("Right Hand Graphics Model", configManager._handController.rightGraphicsModel, typeof(HandModel), false);
      if (EditorGUI.EndChangeCheck()) {
        EditorUtility.SetDirty(configManager._handController.leftGraphicsModel);
        EditorUtility.SetDirty(configManager._handController.rightGraphicsModel);
      }
      
    }
  }

  private void applySelectedConfiguration() {
    int selectedConfigurationIndex = serializedObject.FindProperty("_configuration").enumValueIndex;
    SerializedProperty serializedConfiguration = serializedObject.FindProperty("_headMountedConfigurations").GetArrayElementAtIndex((int)selectedConfigurationIndex);
    LMHeadMountedRigConfiguration configuration = deserializeHeadMountedRig(serializedConfiguration);

    setBackgroundQuadEnabled(configuration.enableBackgroundQuad);
    setGraphicsModels(configuration.leftHandGraphicsModel, configuration.rightHandGraphicsModel);
    setLeftAndRightCamerasEnabled(configuration.enableLeftAndRightCameras);
    setLeftAndRightImageRetrieversEnabled(configuration.enableLeftAndRightImageRetrievers);
    setCenterCameraEnabled(configuration.enableCenterCamera);
    setCameraClearFlags((CameraClearFlags)configuration.cameraClearFlags);
    setTimewarp(configuration.tweenTimewarp);
    setPosition(configuration.tweenPosition);
    setForward(configuration.tweenForward);
    Debug.Log("Switched to configuration: " + configuration.configurationName);
  }

  private void setBackgroundQuadEnabled(bool enabled) {
    Renderer backgroundQuadRenderer = _backgroundQuad.GetComponent<Renderer>();
    LeapImageBasedMaterial backgroundQuadMatrialScript = _backgroundQuad.GetComponent<LeapImageBasedMaterial>();

    if (backgroundQuadRenderer == null) {
      throw new UnityEngine.MissingComponentException("The object " + _backgroundQuad.gameObject.name + " is missing a " + backgroundQuadRenderer.GetType().ToString() + " component.");
    }

    if (backgroundQuadMatrialScript == null) {
      throw new UnityEngine.MissingComponentException("The object " + _backgroundQuad.gameObject.name + " is missing a " + backgroundQuadMatrialScript.GetType().ToString() + " component.");
    }

    _backgroundQuad.GetComponent<Renderer>().enabled = enabled;
    _backgroundQuad.GetComponent<LeapImageBasedMaterial>().enabled = enabled;

    EditorUtility.SetDirty(_backgroundQuad);
  }

  private void setGraphicsModels(HandModel leftHandGraphicsModel, HandModel rightHandGraphicsModel) {
    _handController.leftGraphicsModel = leftHandGraphicsModel;
    _handController.rightGraphicsModel = rightHandGraphicsModel;
    EditorUtility.SetDirty(_handController);
  }

  private void setLeftAndRightCamerasEnabled(bool enabled) {
    Camera left = getCameraObjectForEye(UnityEngine.VR.VRNode.LeftEye);
    Camera right = getCameraObjectForEye(UnityEngine.VR.VRNode.RightEye);

    left.enabled = enabled;
    EditorUtility.SetDirty(left);
    
    right.enabled = enabled;
    EditorUtility.SetDirty(right);
  }

  private void setLeftAndRightImageRetrieversEnabled(bool enabled) {
    LeapImageRetriever left = getImageRetreiverForEye(UnityEngine.VR.VRNode.LeftEye);
    LeapImageRetriever right = getImageRetreiverForEye(UnityEngine.VR.VRNode.RightEye);  
    
    left.enabled = enabled;
    right.enabled = enabled;

    EditorUtility.SetDirty(left);
    EditorUtility.SetDirty(right);
  }

  private void setCenterCameraEnabled(bool enabled) {
    Camera center = getCameraObjectForEye(UnityEngine.VR.VRNode.CenterEye);
    center.enabled = enabled;
    EditorUtility.SetDirty(center);
  }

  private void setCameraClearFlags(CameraClearFlags cameraClearFlags) {
    Camera left = getCameraObjectForEye(UnityEngine.VR.VRNode.LeftEye);
    Camera center = getCameraObjectForEye(UnityEngine.VR.VRNode.CenterEye);
    Camera right = getCameraObjectForEye(UnityEngine.VR.VRNode.RightEye);

    left.clearFlags = cameraClearFlags;
    center.clearFlags = cameraClearFlags;
    right.clearFlags = cameraClearFlags;

    EditorUtility.SetDirty(left);
    EditorUtility.SetDirty(center);
    EditorUtility.SetDirty(right);

  }

  private void setTimewarp(float value) {
    _aligner.tweenTimeWarp = value;
    EditorUtility.SetDirty(_aligner);
  }

  private void setPosition(float value) {
    _aligner.tweenPosition = value;
    EditorUtility.SetDirty(_aligner);
  }

  private void setForward(float value) {
    _aligner.tweenForward = value;
    EditorUtility.SetDirty(_aligner);
  }

  private LMHeadMountedRigConfiguration deserializeHeadMountedRig(SerializedProperty headMountedRigProperty) {
    string configurationName = headMountedRigProperty.FindPropertyRelative("_configurationName").stringValue;
    bool enableBackgroundQuad = headMountedRigProperty.FindPropertyRelative("_enableBackgroundQuad").boolValue;
    HandModel leftHandGraphicsModel = headMountedRigProperty.FindPropertyRelative("_leftHandGraphicsModel").objectReferenceValue as HandModel;
    HandModel rightHandGraphicsModel = headMountedRigProperty.FindPropertyRelative("_rightHandGraphicsModel").objectReferenceValue as HandModel;
    bool enableLeftAndRightCameras = headMountedRigProperty.FindPropertyRelative("_enableLeftAndRightCameras").boolValue;
    bool enableLeftAndRightImageRetrievers = headMountedRigProperty.FindPropertyRelative("_enableLeftAndRightImageRetrievers").boolValue;
    bool enableCenterCamera = headMountedRigProperty.FindPropertyRelative("_enableCenterCamera").boolValue;
    CameraClearFlags clearFlags = (CameraClearFlags)headMountedRigProperty.FindPropertyRelative("_cameraClearFlags").intValue;
    float tweenTimewarp = headMountedRigProperty.FindPropertyRelative("_tweenTimewarp").floatValue;
    float tweenPosition = headMountedRigProperty.FindPropertyRelative("_tweenPosition").floatValue;
    float tweenForward = headMountedRigProperty.FindPropertyRelative("_tweenForward").floatValue;


    Debug.Log("deserilized clear flag: " + clearFlags.ToString() + " : " + (int)clearFlags);

    return new LMHeadMountedRigConfiguration(
      configurationName,
      enableBackgroundQuad,
      leftHandGraphicsModel, rightHandGraphicsModel,
      enableLeftAndRightCameras, enableLeftAndRightImageRetrievers,
      enableCenterCamera, (int)clearFlags,
      tweenTimewarp, tweenPosition, tweenForward);
  }
}
