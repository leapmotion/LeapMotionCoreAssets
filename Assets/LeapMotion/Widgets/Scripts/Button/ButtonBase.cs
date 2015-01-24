using UnityEngine;
using System;

namespace LMWidgets
{
  public abstract class ButtonBase : LeapPhysicsSpring, WidgetBinaryEventHandler<bool>
  {
    public float triggerDistance = 0.025f;
    public float cushionThickness = 0.005f;
    
    public virtual event EventHandler<WidgetEventArg<bool>> StartHandler;
    public virtual event EventHandler<WidgetEventArg<bool>> EndHandler;

    protected float m_localTriggerDistance;
    protected float m_localCushionThickness;
    protected bool m_isPressed = false;
    
    public abstract void ButtonPressed();
    protected void FireButtonPressed(bool value = true) 
    {
      ButtonPressed();
      if (StartHandler != null) {
        StartHandler(this, new WidgetEventArg<bool>(value));
      }
    }

    public abstract void ButtonReleased();
    protected void FireButtonReleased(bool value = false)
    {
      ButtonReleased();
      if (EndHandler != null) {
        EndHandler(this, new WidgetEventArg<bool>(value));
      }
    }
    
    /// <summary>
    /// Returns the fraction of position of the button between rest and trigger. 0.0 = At Rest. 1.0 = At Triggered Distance.
    /// </summary>
    /// <returns>fraction</returns>
    public float GetFraction()
    {
      if (triggerDistance == 0.0f)
        return 0.0f;
      else
      {
        float scale = transform.lossyScale.z;
        float fraction = transform.localPosition.z / m_localTriggerDistance;
        return Mathf.Clamp(fraction, 0.0f, 1.0f);
      }
    }

    /// <summary>
    ///  Constrain the button to the z-axis
    /// </summary>
    protected override void ApplyConstraints()
    {
      Vector3 localPosition = transform.localPosition;
      localPosition.x = 0.0f;
      localPosition.y = 0.0f;
      localPosition.z = Mathf.Max(localPosition.z, 0.0f);
      transform.localPosition = localPosition;
    }

    protected void CheckTrigger()
    {
      float scale = transform.lossyScale.z;
      m_localTriggerDistance = triggerDistance / scale;
      m_localCushionThickness = Mathf.Clamp(cushionThickness / scale, 0.0f, m_localTriggerDistance - 0.001f);
      if (m_isPressed == false)
      {
        if (transform.localPosition.z > m_localTriggerDistance)
        {
          m_isPressed = true;
          FireButtonPressed();
        }
      }
      else if (m_isPressed == true)
      {
        if (transform.localPosition.z < (m_localTriggerDistance - m_localCushionThickness))
        {
          m_isPressed = false;
          FireButtonReleased();
        }
      }
    }

    protected virtual void Start()
    {
      cushionThickness = Mathf.Clamp(cushionThickness, 0.0f, triggerDistance - 0.001f);
    }
    
    protected virtual void Update() 
    {
      CheckTrigger();
    }
  }
}
