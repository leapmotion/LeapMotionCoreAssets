using UnityEngine;
using System.Collections;

public class VisualizerControls : MonoBehaviour {
  void Start()
  {
    Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, false);
  }

	// Update is called once per frame
	void Update () {
	  if (Input.GetKeyDown(KeyCode.Escape))
    {
      Application.Quit();
    }
	}
}
