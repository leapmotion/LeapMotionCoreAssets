using UnityEngine;
using System.Collections;
using UnityEngine.VR;

public class VisualizerManager : MonoBehaviour {
  public GameObject PCVisualizer = null;
  public GameObject VRVisualizer = null;

  void Awake()
  {
    if (VRDevice.isPresent)
    {
      PCVisualizer.gameObject.SetActive(false);
      VRVisualizer.gameObject.SetActive(true);
    } else
    {
      VRVisualizer.gameObject.SetActive(false);
      PCVisualizer.gameObject.SetActive(true);
    }
  }

  void Start()
  {
    Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, false);
  }
}
