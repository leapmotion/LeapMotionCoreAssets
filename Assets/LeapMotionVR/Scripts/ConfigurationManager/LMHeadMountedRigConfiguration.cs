using UnityEngine;
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
  private bool _enableLeftAndRightCameras;

  [SerializeField]
  private bool _enableLeftAndRightImageRetrievers;
  
  [SerializeField]
  private bool _enableCenterCamera;

  [SerializeField]
  private int _cameraClearFlags;

  [SerializeField]
  private float _tweenTimewarp;

  [SerializeField]
  private float _tweenPosition;

  [SerializeField]
  private float _tweenForward;

  public string configurationName { 
    get { return _configurationName; }
    set { _configurationName = value; }
  }
  public bool enableBackgroundQuad { get { return _enableBackgroundQuad; } }
  public HandModel leftHandGraphicsModel { get { return _leftHandGraphicsModel; } }
  public HandModel rightHandGraphicsModel { get { return _rightHandGraphicsModel; } }
  public bool enableLeftAndRightCameras { get { return _enableLeftAndRightCameras; } }
  public bool enableLeftAndRightImageRetrievers { get { return _enableLeftAndRightImageRetrievers; } }
  public bool enableCenterCamera { get { return _enableCenterCamera; } }
  public int cameraClearFlags { get { return _cameraClearFlags; } }
  public float tweenTimewarp { get { return _tweenTimewarp; } }
  public float tweenPosition { get { return _tweenPosition; } }
  public float tweenForward { get { return _tweenForward; } }

  public LMHeadMountedRigConfiguration(
    string name,
    bool backgroundQuad, 
    HandModel leftHandModel, HandModel rightHandModel,
    bool leftAndRightCameras, bool leftAndRightImageRetrievers, bool centerCamera, int clearFlags,
    float timewarp, float position, float forward) {
      _configurationName = name;
      _enableBackgroundQuad = backgroundQuad;
      _leftHandGraphicsModel = leftHandModel;
      _rightHandGraphicsModel = rightHandModel;
      _enableLeftAndRightCameras = leftAndRightCameras;
      _enableLeftAndRightImageRetrievers = leftAndRightImageRetrievers;
      _enableCenterCamera = centerCamera;
      _cameraClearFlags = clearFlags;
      _tweenTimewarp = timewarp;
      _tweenPosition = position;
      _tweenForward = forward;
  }
}
