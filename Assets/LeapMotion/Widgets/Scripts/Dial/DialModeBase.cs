using UnityEngine;
using System.Collections;

namespace LMWidgets
{
  public class DialModeBase : MonoBehaviour
  {
    private GameObject target_ = null;
    private Vector3 pivot_ = Vector3.zero;

    private float curr_angle_ = 0.0f;
    private float next_angle_ = 0.0f;

    private bool IsHand(Collider other)
    {
      return other.transform.parent && other.transform.parent.parent && other.transform.parent.parent.GetComponent<HandModel>();
    }

    private bool IsFingerTip(Collider other, string finger)
    {
      return (other.name == "bone3" && other.transform.parent.name == finger);
    }

    void OnTriggerEnter(Collider other)
    {
      if (target_ == null && IsHand(other))
      {
        target_ = other.gameObject;
        pivot_ = transform.InverseTransformPoint(target_.transform.position) - transform.localPosition;
        pivot_.z = 0.0f;
        transform.rigidbody.angularVelocity = Vector3.zero;
      }
    }

    void OnTriggerExit(Collider other)
    {
      if (other.gameObject == target_)
      {
        target_ = null;
        transform.rigidbody.AddRelativeTorque(new Vector3(0.0f, 0.0f, (next_angle_ - curr_angle_) / (Time.deltaTime)));
        Debug.Log((next_angle_ - curr_angle_) / (Time.deltaTime));
      }
    }

    void FixedUpdate()
    {
      if (target_ != null)
      {
        Vector3 curr_direction = transform.InverseTransformPoint(target_.transform.position) - transform.localPosition;
        curr_direction.z = 0.0f;
        curr_angle_ = transform.localRotation.z;
        transform.localRotation = Quaternion.FromToRotation(pivot_, curr_direction) * transform.localRotation;
        next_angle_ = transform.localRotation.z;
      }
    }
  }
}

