using UnityEngine;
using System.Collections;
using VRWidgets;

public class SliderDemoBasic : SliderBase 
{
  public GameObject graphics;
  public GameObject activeBar = null;

  public override void SliderPressed()
  {
    Debug.Log("HandleReleased");
  }

  public override void SliderReleased()
  {
    Debug.Log("HandlePressed");
  }

  private void UpdateActiveBar()
  {
    if (activeBar)
    {
      Vector3 activeBarPosition = activeBar.transform.localPosition;
      activeBarPosition.x = (transform.localPosition.x + lowerLimit.transform.localPosition.x) / 2.0f;
      activeBar.transform.localPosition = activeBarPosition;
      Vector3 activeBarScale = activeBar.transform.localScale;
      activeBarScale.x = Mathf.Abs(transform.localPosition.x - lowerLimit.transform.localPosition.x);
      activeBar.transform.localScale = activeBarScale;
    }
  }

  public override void UpdatePosition(Vector3 displacement)
  {
    base.UpdatePosition(displacement);
    UpdateActiveBar();
  }

  public override void Awake()
  {
    base.Awake();
    UpdateActiveBar();
  }
	
	public override void Update () 
  {
    base.Update();
    graphics.transform.localPosition = GetPosition();
	}
}
