using UnityEngine;
using System.Collections;
using VRWidgets;

public class ButtonDemoBasic : ButtonBase 
{
  public GameObject graphics;

  public override void ButtonReleased()
  {
    Debug.Log("Released");
  }

  public override void ButtonPressed()
  {
    Debug.Log("Pressed");
  }

  public override void Update()
  {
    base.Update();
    graphics.transform.localPosition = GetPosition();
  }
}
