using UnityEngine;
using System.Collections;

public class PassthroughEnabler : MonoBehaviour {

  public GameObject passthroughLeft;
  public GameObject passthroughRight;

  private HandController controller_;

  private bool show_passthrough_ = true;

	// Use this for initialization
	void Start () {
    controller_ = GetComponent<HandController>();
	}
	
	// Update is called once per frame
	void Update () {
    if (Input.GetKeyDown(KeyCode.P))
    {
      show_passthrough_ = !show_passthrough_;
      if (show_passthrough_)
      {
        passthroughLeft.SetActive(true);
        passthroughRight.SetActive(true);
        transform.localScale = Vector3.one * 1.6f;
        transform.localPosition = Vector3.zero;
      }
      else
      {
        passthroughLeft.SetActive(false);
        passthroughRight.SetActive(false);
        transform.localScale = Vector3.one;
        transform.localPosition = new Vector3(0.0f, 0.0f, 0.08f);
      }
    }
	}
}
