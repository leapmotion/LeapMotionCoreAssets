using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;

[System.Serializable]
public struct LMHeadMountedRigConfiguration  {
  [SerializeField]
  private string _configurationName;

  [SerializeField]
  private bool _enableBackgroundQuad;

  [SerializeField]
  private HandModel _leftHandGraphicsModel;

  [SerializeField]
  private HandModel _rightHandGraphicsModel;

  [SerializeField]
  private bool _seperateLeftRightCameras;

  [SerializeField]
  private bool _enableImageRetrievers;

  [SerializeField]
  private int _cameraClearFlags;

  [SerializeField]
  private float _tweenTimewarp;

  [SerializeField]
  private bool _overrideCameraPos;

  public string configurationName { 
    get { return _configurationName; }
    set { _configurationName = value; }
  }
  public bool enableBackgroundQuad { get { return _enableBackgroundQuad; } }
  public HandModel leftHandGraphicsModel { get { return _leftHandGraphicsModel; } }
  public HandModel rightHandGraphicsModel { get { return _rightHandGraphicsModel; } }
  public bool seperateLeftRightCameras { get { return _seperateLeftRightCameras; } }
  public bool enableImageRetrievers { get { return _enableImageRetrievers; } }
  public int cameraClearFlags { get { return _cameraClearFlags; } }
  public float tweenTimewarp { get { return _tweenTimewarp; } }
  public bool overrideEyePos { get { return _overrideCameraPos; } }

  public LMHeadMountedRigConfiguration(
    string name,
    bool backgroundQuad, 
    HandModel leftHandModel, HandModel rightHandModel,
    bool seperateLeftRightCameras, bool enableImageRetrievers, bool centerCamera, int clearFlags,
    float timewarp, bool overrideCameraPos) {
      _configurationName = name;
      _enableBackgroundQuad = backgroundQuad;
      _leftHandGraphicsModel = leftHandModel;
      _rightHandGraphicsModel = rightHandModel;
      _seperateLeftRightCameras = seperateLeftRightCameras;
      _enableImageRetrievers = enableImageRetrievers;
      _cameraClearFlags = clearFlags;
      _tweenTimewarp = timewarp;
      _overrideCameraPos = overrideCameraPos;
  }
}
