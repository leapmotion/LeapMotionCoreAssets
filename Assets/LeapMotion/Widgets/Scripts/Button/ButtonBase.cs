using UnityEngine;
using System.Collections;

namespace VRWidgets
{
  [RequireComponent(typeof(BoxCollider))]
  [RequireComponent(typeof(Rigidbody))]
  public abstract class ButtonBase : MonoBehaviour
  {
    public float spring = 50.0f;
    public float triggerDistance = 0.025f;
    public float cushionThickness = 0.005f;

    protected bool is_pressed_;
    protected float min_distance_;
    protected float max_distance_;

    public abstract void ButtonReleased();
    public abstract void ButtonPressed();

    public float GetPercent()
    {
      return Mathf.Clamp(transform.localPosition.z / triggerDistance, 0.0f, 1.0f);
    }

    public Vector3 GetPosition()
    {
      if (triggerDistance == 0.0f)
        return Vector3.zero;

      Vector3 position = transform.localPosition;
      position.z = GetPercent() * triggerDistance;
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
      rigidbody.AddRelativeForce(new Vector3(0.0f, 0.0f, -spring * (transform.localPosition.z)));
    }

    protected void CheckTrigger()
    {
      if (is_pressed_ == false)
      {
        if (transform.localPosition.z > triggerDistance)
        {
          is_pressed_ = true;
          ButtonPressed();
        }
      }
      else if (is_pressed_ == true)
      {
        if (transform.localPosition.z < (triggerDistance - cushionThickness))
        {
          is_pressed_ = false;
          ButtonReleased();
        }
      }
    }

    public virtual void Awake()
    {
      is_pressed_ = false;
      cushionThickness = Mathf.Clamp(cushionThickness, 0.0f, triggerDistance - 0.001f);
      min_distance_ = 0.0f;
      max_distance_ = float.MaxValue;
    }

    public virtual void Update()
    {
      ApplySpring();
      ApplyConstraints();
      CheckTrigger();
    }
  }
}
