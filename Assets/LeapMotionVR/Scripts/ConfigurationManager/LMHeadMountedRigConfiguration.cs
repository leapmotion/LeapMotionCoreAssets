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
  private bool _showHandGraphicField;

  [SerializeField]
  private HandModel _leftHandGraphicsModel;

  [SerializeField]
  private HandModel _rightHandGraphicsModel;

  [SerializeField]
  private bool _enableImageRetrievers;

  [SerializeField]
  private CameraClearFlags _cameraClearFlags;

  [Range(0, 1)]
  [SerializeField]
  private float _tweenImageWarping;

  [Range(0, 1)]
  [SerializeField]
  private float _tweenRotationalWarping;

  [Range(0, 1)]
  [SerializeField]
  private float _tweenPositionalWarping;

  [SerializeField]
  private LeapVRTemporalWarping.SyncMode _temporalSyncMode;

  [SerializeField]
  private bool _overrideEyePos;

  public string ConfigurationName { get { return _configurationName; } set { _configurationName = value; } }
  public bool EnableBackgroundQuad { get { return _enableBackgroundQuad; } }
  public bool ShowHandGraphicField { get { return _showHandGraphicField; } }
  public HandModel LeftHandGraphicsModel { get { return _leftHandGraphicsModel; } }
  public HandModel RightHandGraphicsModel { get { return _rightHandGraphicsModel; } }
  public bool EnableImageRetrievers { get { return _enableImageRetrievers; } }
  public CameraClearFlags CameraClearFlags { get { return _cameraClearFlags; } }
  public float TweenImageWarping { get { return _tweenImageWarping; } }
  public float TweenRotationalWarping { get { return _tweenRotationalWarping; } }
  public float TweenPositionalWarping { get { return _tweenPositionalWarping; } }
  public LeapVRTemporalWarping.SyncMode TemporalSynMode { get { return _temporalSyncMode; } }
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
    config._tweenImageWarping = property.FindPropertyRelative("_tweenImageWarping").floatValue;
    config._tweenRotationalWarping = property.FindPropertyRelative("_tweenRotationalWarping").floatValue;
    config._tweenPositionalWarping = property.FindPropertyRelative("_tweenPositionalWarping").floatValue;
    config._temporalSyncMode = (LeapVRTemporalWarping.SyncMode)property.FindPropertyRelative("_temporalSyncMode").intValue;
    config._overrideEyePos = property.FindPropertyRelative("_overrideEyePos").boolValue;
    return config;
  }
#endif
}
