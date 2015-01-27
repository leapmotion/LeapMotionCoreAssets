using UnityEngine;
using System;
using System.Collections;

namespace LMWidgets
{
  [RequireComponent(typeof(Rigidbody))]
  public abstract class ButtonBase : MonoBehaviour, BinaryInteractionHandler<bool>
  {
    // Binary Interaction Handler - Fires when interaction with the widget starts.
    public event EventHandler<LMWidgets.EventArg<bool>> StartHandler;
    // Binary Interaction Handler - Fires when interaction with the widget ends.
    public event EventHandler<LMWidgets.EventArg<bool>> EndHandler;

    public float spring = 1000.0f;
    public float triggerDistance = 0.025f;
    public float cushionThickness = 0.005f;

    protected float scaled_spring_;
    protected float scaled_trigger_distance_;
    protected float scaled_cushion_thickness_;

    protected bool is_pressed_;
    protected float min_distance_;
    protected float max_distance_;

    public virtual void ButtonReleased ()
    {
      FireButtonStart ();
    }
    
    public virtual void ButtonPressed ()
    {
      FireButtonEnd ();
    }
    
    protected void FireButtonStart (bool value = true)
    {
      if (StartHandler != null) {
        StartHandler (this, new LMWidgets.EventArg<bool> (value));
      } 
    }
    
    protected void FireButtonEnd (bool value = false)
    {
      if (EndHandler != null) {
        EndHandler (this, new LMWidgets.EventArg<bool> (value));
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

    protected void SetMinDistance(float distance)
    {
      min_distance_ = distance;
    }

    protected void SetMaxDistance(float distance)
    {
      max_distance_ = distance;
    }

    protected virtual void ApplyConstraints()
    {
      Vector3 local_position = transform.localPosition;
      local_position.x = 0.0f;
      local_position.y = 0.0f;
      local_position.z = Mathf.Clamp(local_position.z, min_distance_, max_distance_);
      transform.localPosition = local_position;
    }

    protected void ApplySpring()
    {
      rigidbody.AddRelativeForce(new Vector3(0.0f, 0.0f, -scaled_spring_ * (transform.localPosition.z)));
    }

    protected void CheckTrigger()
    {
      if (is_pressed_ == false)
      {
        if (transform.localPosition.z > scaled_trigger_distance_)
        {
          is_pressed_ = true;
          ButtonPressed();
        }
      }
      else if (is_pressed_ == true)
      {
        if (transform.localPosition.z < (scaled_trigger_distance_- scaled_cushion_thickness_))
        {
          is_pressed_ = false;
          ButtonReleased();
        }
      }
    }

    private void ScaleProperties()
    {
      float scale = transform.lossyScale.z;
      scaled_spring_ = spring * scale;
      scaled_trigger_distance_ = triggerDistance / scale;
      scaled_cushion_thickness_ = Mathf.Clamp(cushionThickness / scale, 0.0f, scaled_trigger_distance_ - 0.001f);
    }

    public virtual void Awake()
    {
      if (GetComponent<Collider>() == null)
      {
        Debug.LogWarning("This Widget lacks a collider. Will not function as expected");
      }
      is_pressed_ = false;
      cushionThickness = Mathf.Clamp(cushionThickness, 0.0f, triggerDistance - 0.001f);
      min_distance_ = 0.0f;
      max_distance_ = float.MaxValue;
      ScaleProperties();
    }

    public virtual void FixedUpdate()
    {
      ScaleProperties();
      ApplySpring();
      ApplyConstraints();
      CheckTrigger();
    }
  }
}
