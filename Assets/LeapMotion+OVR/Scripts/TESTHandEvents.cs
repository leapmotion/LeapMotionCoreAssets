using UnityEngine;
using System.Collections;

public class TESTHandEvents : MonoBehaviour {
  public HandController handController;

	// Use this for initialization
	void Start () {
    if (handController == null) {
      handController = GameObject.FindObjectOfType<HandController>();
    }
    if (handController == null) {
      enabled = false;
      return;
    }
    handController.onCreateHand += onCreateHand;
    handController.onDestroyHand += onDestroyHand;
	}
	
  public void onCreateHand(HandModel hand) {
    Debug.Log("onCreateHand ID = " + hand.LeapID() + " for model " + hand.name);
  }

  
  public void onDestroyHand(HandModel hand) {
    Debug.Log("onDestroyHand ID = " + hand.LeapID() + " for model " + hand.name);
  }
}
