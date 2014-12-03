using UnityEngine;
using System.Collections;

namespace VRWidgets
{
  public class DialBase : ButtonBase 
  {
    public float lowestDegree;
    public float highestDegree;

    private GameObject target_ = null;
    private Vector3 pivot_ = Vector3.zero;
    private Vector3 target_pivot_ = Vector3.zero;

    public virtual void OnCollisionEnter(Collision collision)
    {
      if (target_ == null)
        target_ = collision.gameObject;
    }

    public override void ButtonPressed()
    {
      target_pivot_ = transform.parent.InverseTransformPoint(target_.transform.position);
    }

    public override void ButtonReleased()
    {
      target_ = null;
    }

    public virtual void UpdateRotation(Vector3 displacement)
    {

    }

    public override void Update()
    {
      base.Update();
      if (is_pressed_)
      {
        if (target_)
        {
          UpdateRotation(transform.parent.InverseTransformPoint(target_.transform.position) - target_pivot_);
        }
      }
    }
  }
}

