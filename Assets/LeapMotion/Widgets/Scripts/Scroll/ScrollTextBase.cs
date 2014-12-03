using UnityEngine;
using System.Collections;

namespace LMWidgets
{
  public class ScrollTextBase : ButtonBase
  {
    private Vector3 this_pivot_ = Vector3.zero;
    private Vector3 target_pivot_ = Vector3.zero;

    private GameObject target_ = null;

    private bool IsHand(Collision other)
    {
      return other.transform.parent && other.transform.parent.parent && other.transform.parent.parent.GetComponent<HandModel>();
    }

    void OnCollisionEnter(Collision other)
    {
      if (target_ == null && IsHand(other))
      {
        target_ = other.gameObject;
      }
    }

    private void UpdatePosition(Vector3 displacement)
    {
      Vector3 local_position = transform.localPosition;
      local_position.x = 0.0f;
      local_position.y = transform.InverseTransformPoint(this_pivot_).y + transform.InverseTransformDirection(displacement).y;
      local_position.z = Mathf.Max(local_position.z, 0.0f);
      transform.localPosition = local_position;
    }

    public override void ButtonPressed()
    {
      if (target_ != null)
      {
        this_pivot_ = transform.position;
        target_pivot_ = target_.transform.position;
      }
    }

    public override void ButtonReleased()
    {
      target_ = null;
      transform.localPosition = Vector3.zero;
    }

    // Update is called once per frame
    public override void Update()
    {
      base.Update();
      if (is_pressed_)
      {
        if (target_ != null)
        {
          UpdatePosition(target_.transform.position - target_pivot_);
        }
      }
    }
  }
}

