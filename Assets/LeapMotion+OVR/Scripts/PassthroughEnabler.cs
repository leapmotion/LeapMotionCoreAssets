using UnityEngine;
using System.Collections;

public class PassthroughEnabler : MonoBehaviour {

  public GameObject passthroughLeft;
  public GameObject passthroughRight;

  private HandController controller_;

	// Use this for initialization
	void Start () {
    controller_ = GetComponent<HandController>();
	}
	
	// Update is called once per frame
	void Update () {
    if (controller_.GetAllGraphicsHands().Length > 0)
    {
      passthroughLeft.SetActive(false);
      passthroughRight.SetActive(false);
    }
    else
    {
      passthroughLeft.SetActive(true);
      passthroughRight.SetActive(true);
    }
	}
}
