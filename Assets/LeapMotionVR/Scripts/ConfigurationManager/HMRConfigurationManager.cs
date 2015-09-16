using UnityEngine;
using System.Collections;

public class HMRConfigurationManager : MonoBehaviour {
  public enum HMRConfiguration {
    VR_WORLD_VR_HANDS = 0,
    VR_WORLD_AR_HANDS = 1,
    AR_WORLD_AR_HANDS = 2
  }

  [SerializeField]
  private HMRConfiguration _configuration;

  [SerializeField]
  private LMHeadMountedRigConfiguration[] _headMountedConfigurations;

  public GameObject _backgroundQuad;
  public HandController _handController;
  public Camera _leftCamera;
  public Camera _rightCamera;
  public Camera _centerCamera;
  public LeapCameraAlignment _aligner;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

  public void validateConfigurationsLabeled() {
    validateEnoughConfigurations();
    string[] enumNames = System.Enum.GetNames(typeof(HMRConfiguration));

    for (int i = 0; i < _headMountedConfigurations.Length; i++) {
      if (_headMountedConfigurations[i].configurationName == enumNames[i]) { continue; }
      _headMountedConfigurations[i].configurationName = enumNames[i];
    }
  }

  private void validateEnoughConfigurations() {
    string[] enumNames = System.Enum.GetNames(typeof(HMRConfiguration));

    if (enumNames.Length > _headMountedConfigurations.Length) {
      LMHeadMountedRigConfiguration[] configs = new LMHeadMountedRigConfiguration[enumNames.Length];
      System.Array.Copy(_headMountedConfigurations, configs, _headMountedConfigurations.Length);
      _headMountedConfigurations = configs;
    }
  }
}
