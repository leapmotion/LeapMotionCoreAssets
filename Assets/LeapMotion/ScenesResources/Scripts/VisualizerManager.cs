using UnityEngine;
using UnityEngine.VR;

public class VisualizerManager : MonoBehaviour {
  public GameObject m_PCVisualizer = null;
  public GameObject m_VRVisualizer = null;
  public UnityEngine.UI.Text m_modeText;
  public UnityEngine.UI.Text m_warningText;

  void Awake()
  {
    if (VRDevice.isPresent)
    {
      m_PCVisualizer.gameObject.SetActive(false);
      m_VRVisualizer.gameObject.SetActive(true);
      m_modeText.text = "VR Mode";
      m_warningText.text = "";
    }
    else
    {
      m_VRVisualizer.gameObject.SetActive(false);
      m_PCVisualizer.gameObject.SetActive(true);
      m_modeText.text = "Desktop Mode";
      m_warningText.text = "Orion is built for virtual reality and performs best when head-mounted";
    }
    Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, false);
  }
}
