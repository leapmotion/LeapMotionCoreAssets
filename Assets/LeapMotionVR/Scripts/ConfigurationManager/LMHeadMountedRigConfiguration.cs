using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Serialization;
using System.Collections;

[System.Serializable]
public struct LMHeadMountedRigConfiguration {
  [SerializeField]
  private string _configurationName;

  [SerializeField]
  private bool _enableBackgroundQuad;

  [SerializeField]
  private HandModel _leftHandGraphicsModel;

  [SerializeField]
  private HandModel _rightHandGraphicsModel;

  [SerializeField]
  private bool _enableImageRetrievers;

  [SerializeField]
  private CameraClearFlags _cameraClearFlags;

  [SerializeField]
  private float _tweenTimewarp;

  [SerializeField]
  private bool _overrideEyePos;

  public string ConfigurationName { get { return _configurationName; } set { _configurationName = value; } }
  public bool EnableBackgroundQuad { get { return _enableBackgroundQuad; } }
  public HandModel LeftHandGraphicsModel { get { return _leftHandGraphicsModel; } }
  public HandModel RightHandGraphicsModel { get { return _rightHandGraphicsModel; } }
  public bool EnableImageRetrievers { get { return _enableImageRetrievers; } }
  public CameraClearFlags CameraClearFlags { get { return _cameraClearFlags; } }
  public float TweenTimewarp { get { return _tweenTimewarp; } }
  public bool OverrideEyePos { get { return _overrideEyePos; } }

#if UNITY_EDITOR
  public static LMHeadMountedRigConfiguration Deserialize(SerializedProperty property) {
    LMHeadMountedRigConfiguration config = new LMHeadMountedRigConfiguration();
    config._configurationName = property.FindPropertyRelative("_configurationName").stringValue;
    config._enableBackgroundQuad = property.FindPropertyRelative("_enableBackgroundQuad").boolValue;
    config._leftHandGraphicsModel = property.FindPropertyRelative("_leftHandGraphicsModel").objectReferenceValue as HandModel;
    config._rightHandGraphicsModel = property.FindPropertyRelative("_rightHandGraphicsModel").objectReferenceValue as HandModel;
    config._enableImageRetrievers = property.FindPropertyRelative("_enableImageRetrievers").boolValue;
    config._cameraClearFlags = (CameraClearFlags)property.FindPropertyRelative("_cameraClearFlags").intValue;
    config._tweenTimewarp = property.FindPropertyRelative("_tweenTimewarp").floatValue;
    config._overrideEyePos = property.FindPropertyRelative("_overrideEyePos").boolValue;
    return config;
  }
#endif
}
