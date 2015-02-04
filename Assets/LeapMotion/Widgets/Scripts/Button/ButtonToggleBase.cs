using UnityEngine;
using System.Collections;

namespace LMWidgets
{
  public abstract class ButtonToggleBase : ButtonBase
  {
    [SerializeField]
    protected DataBinderBool m_dataBinder;

    protected bool m_toggleState;

    public abstract void ButtonTurnsOn();
    public abstract void ButtonTurnsOff();


    protected override void Start() {
      if ( m_dataBinder != null ) {
        m_dataBinder.DataChangedHandler += onDataChanged; // Listen for changes in external data
        setButtonState(m_dataBinder.GetCurrentData()); // Initilize widget value
      }
    }

    private void onDataChanged(object sender, EventArg<bool> arg) {
      if ( m_isPressed ) { return; } // Don't worry about change events while being interacted with
      setButtonState(arg.CurrentValue);
    }

    protected virtual void setButtonState(bool toggleState) {
      if ( toggleState == m_toggleState ) { return; } // Don't do anything if there's no change
      m_toggleState = toggleState;
      if ( m_dataBinder != null ) {
        if (m_toggleState == true)
          ButtonTurnsOn();
        else
          ButtonTurnsOff();
      }
    }

    protected override void buttonReleased()
    {
      base.FireButtonEnd(m_toggleState);
      if ( m_dataBinder != null ) {
        setButtonState(m_dataBinder.GetCurrentData()); // Update once we're done interacting
      }
    }

    protected override void buttonPressed()
    {
      if (m_toggleState == false)
        ButtonTurnsOn();
      else
        ButtonTurnsOff();
      m_toggleState = !m_toggleState;
      base.FireButtonStart(m_toggleState);
      if ( m_dataBinder != null ) { m_dataBinder.SetCurrentData(m_toggleState); } // Update externally linked data
    }
  }
}
