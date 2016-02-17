using UnityEngine;
using UnityEngine.VR;
using UnityEngine.SceneManagement;
using Leap;

public class VisualizerManager : MonoBehaviour {
  public GameObject m_PCVisualizer = null;
  public GameObject m_VRVisualizer = null;
  public UnityEngine.UI.Text m_modeText;
  public UnityEngine.UI.Text m_warningText;

  private Controller leap_controller_ = null;
  private bool m_leapConnected = false;

  void Awake()
  {
    Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, false);
    leap_controller_ = new Controller();
  }

  void Start()
  {
    m_leapConnected = leap_controller_.IsConnected;
    if (m_leapConnected)
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
    }
  }

  void Update()
  {
    if (m_leapConnected)
      return;

    if (leap_controller_.IsConnected)
    {
      SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
  }
}
