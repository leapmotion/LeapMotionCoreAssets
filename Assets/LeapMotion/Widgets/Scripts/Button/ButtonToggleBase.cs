using UnityEngine;
using System.Collections;

namespace LMWidgets
{
  public abstract class ButtonToggleBase : ButtonBase
  {
    protected bool m_toggleState;

    public abstract void ButtonTurnsOn();
    public abstract void ButtonTurnsOff();

    public override void ButtonReleased()
    {
    }

    public override void ButtonPressed()
    {
      if (m_toggleState == false)
        ButtonTurnsOn();
      else
        ButtonTurnsOff();
      m_toggleState = !m_toggleState;
    }
  }
}
