using UnityEngine;
using System.Collections;

public class HMRConfigurationManager : MonoBehaviour {

  [SerializeField]
  private int _configurationIndex;

  [SerializeField]
  private LMHeadMountedRigConfiguration[] _headMountedConfigurations;

  public GameObject _backgroundQuad;
  public HandController _handController;
  public LeapVRTemporalWarping _aligner;
}
