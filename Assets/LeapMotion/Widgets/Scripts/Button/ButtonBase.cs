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

    protected float scaled_spring_;
    protected float scaled_trigger_distance_;
    protected float scaled_cushion_thickness_;

    protected bool is_pressed_;
    
    public abstract void ButtonPressed();
    protected void FireButtonPressed(bool value = true) 
    {
      if (StartHandler != null) {
        StartHandler(this, new WidgetEventArg<bool>(value));
      }
    }

    public abstract void ButtonReleased();
    protected void FireButtonReleased(bool value = false)
    {
      if (EndHandler != null) {
        EndHandler(this, new WidgetEventArg<bool>(value));
      }
    }
    
    public float GetFraction()
    {
      return Mathf.Clamp(transform.localPosition.z / scaled_trigger_distance_, 0.0f, 1.0f);
    }

    public Vector3 GetPosition()
    {
      if (triggerDistance == 0.0f)
        return Vector3.zero;

      Vector3 position = transform.localPosition;
      position.z = GetFraction() * scaled_trigger_distance_;
      return position;
    }

    protected void CheckTrigger()
    {
      if (is_pressed_ == false)
      {
        if (transform.localPosition.z > scaled_trigger_distance_)
        {
          is_pressed_ = true;
          ButtonPressed();
          FireButtonPressed();
        }
      }
      else if (is_pressed_ == true)
      {
        if (transform.localPosition.z < (scaled_trigger_distance_- scaled_cushion_thickness_))
        {
          is_pressed_ = false;
          ButtonReleased();
          FireButtonReleased();
        }
      }
    }

    protected virtual void Awake()
    {
      if (GetComponent<Collider>() == null)
      {
        Debug.LogWarning("This Widget lacks a collider. Will not function as expected");
      }
      is_pressed_ = false;
      cushionThickness = Mathf.Clamp(cushionThickness, 0.0f, triggerDistance - 0.001f);
    }
    
    protected virtual void Update() 
    {
      CheckTrigger();
    }
  }
}
