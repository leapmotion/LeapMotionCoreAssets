using UnityEngine;
using System.Collections;

namespace LMWidgets
{
  public class ScrollTextBase : ButtonBase
  {
    public GameObject content;

    private Vector3 local_pivot_ = Vector3.zero;
    private Vector3 target_pivot_ = Vector3.zero;
    private Vector3 content_pivot_ = Vector3.zero;

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
      Vector3 local_displacement = transform.InverseTransformDirection(displacement);
      Vector3 local_position = transform.localPosition;
      local_position.x = 0.0f;
      local_position.y = local_pivot_.y + local_displacement.y;
      local_position.z = Mathf.Max(local_position.z, 0.0f);
      transform.localPosition = local_position;

      Vector3 content_displacement = content.transform.InverseTransformDirection(displacement);
      Vector3 content_position = content.transform.localPosition;
      content_position.y = content_pivot_.y + content_displacement.y * 1000;
      content.transform.localPosition = content_position;
    }

    public override void ButtonPressed()
    {
      if (target_ != null)
      {
        local_pivot_ = transform.localPosition;
        target_pivot_ = target_.transform.position;
        content_pivot_ = content.transform.localPosition;
      }
    }

    public override void ButtonReleased()
    {
      target_ = null;
      transform.localPosition = Vector3.zero;
    }

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

