using UnityEngine;
using System.Collections;
using UnityEngine.VR;

public class VRVisualizerManager : MonoBehaviour {
  public Camera m_VRCamera;
  public Camera m_promptCamera;

	// Use this for initialization
	void Awake () {
    if (VRDevice.isPresent)
    {
      m_VRCamera.gameObject.SetActive(true);
      m_promptCamera.gameObject.SetActive(false);
    } else
    {
      m_VRCamera.gameObject.SetActive(false);
      m_promptCamera.gameObject.SetActive(true);
    }

	}


}
