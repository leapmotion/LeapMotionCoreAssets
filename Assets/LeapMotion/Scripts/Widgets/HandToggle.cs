using UnityEngine;
using System.Collections;

public class HandToggle : MonoBehaviour {

  HandController hand_controller_;
  private bool show_hand_ = true;

	// Use this for initialization
	void Start () {
    hand_controller_ = GetComponent<HandController>();
	}
	
	// Update is called once per frame
	void Update () {
    if (Input.GetKeyDown(KeyCode.H))
    {
      show_hand_ = !show_hand_;
    }
    HandModel[] models = hand_controller_.GetAllGraphicsHands();
    foreach (HandModel model in models)
    {
      Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
      foreach (Renderer renderer in renderers)
      {
        renderer.enabled = show_hand_;
      }
    }
	}
}
