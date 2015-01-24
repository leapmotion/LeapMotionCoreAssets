using UnityEngine;
using System;
using System.Collections;

namespace LMWidgets
{
  public abstract class ButtonBase : LeapPhysicsSpring, WidgetBinaryEventHandler<bool>
  {
    public float triggerDistance = 0.025f;
    public float cushionThickness = 0.005f;
    
    public virtual event EventHandler<WidgetEventArg<bool>> StartHandler;
    public virtual event EventHandler<WidgetEventArg<bool>> EndHandler;

    protected float m_scaledTriggerDistance;
    protected float m_scaledCushionThickness;

    protected bool is_pressed_;
    
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
    
    public float GetFraction()
    {
      if (m_scaledTriggerDistance == 0.0f)
        return 0.0f;
      else
        return Mathf.Clamp(transform.localPosition.z / m_scaledTriggerDistance, 0.0f, 1.0f);
    }

    public Vector3 GetPosition()
    {
      Vector3 position = transform.localPosition;
      position.z = GetFraction() * m_scaledTriggerDistance;
      return position;
    }

    protected void CheckTrigger()
    {
      float scale = transform.lossyScale.z;
      m_scaledTriggerDistance = triggerDistance / scale;
      m_scaledCushionThickness = Mathf.Clamp(cushionThickness / scale, 0.0f, m_scaledTriggerDistance - 0.001f);
      if (is_pressed_ == false)
      {
        if (transform.localPosition.z > m_scaledTriggerDistance)
        {
          is_pressed_ = true;
          FireButtonPressed();
        }
      }
      else if (is_pressed_ == true)
      {
        if (transform.localPosition.z < (m_scaledTriggerDistance - m_scaledCushionThickness))
        {
          is_pressed_ = false;
          FireButtonReleased();
        }
      }
    }

    protected virtual void Awake()
    {
      base.Awake();
      is_pressed_ = false;
      cushionThickness = Mathf.Clamp(cushionThickness, 0.0f, triggerDistance - 0.001f);
    }
    
    protected virtual void Update() 
    {
      CheckTrigger();
    }
  }
}
