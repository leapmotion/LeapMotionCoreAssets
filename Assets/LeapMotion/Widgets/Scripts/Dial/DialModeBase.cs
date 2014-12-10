using UnityEngine;
using System.Collections;

namespace LMWidgets
{
  public class DialModeBase : MonoBehaviour
  {
    private GameObject index_ = null;
    private GameObject thumb_ = null;

    private float angle_pivot_ = 0.0f;
    private Vector3 direction_pivot_ = Vector3.zero;

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
      if (IsHand(other))
      {
        if (index_ == null || thumb_ == null)
        {
          index_ = (index_ == null && IsFingerTip(other, "index")) ? other.gameObject : index_;
          thumb_ = (thumb_ == null && IsFingerTip(other, "thumb")) ? other.gameObject : thumb_;
          if (index_ != null && thumb_ != null)
          {
            direction_pivot_ = index_.transform.position - thumb_.transform.position;
          }
        }
      }
    }

    void OnTriggerExit(Collider other)
    {
      if (other.gameObject == index_)
      {
        index_ = null;
      }

      if (other.gameObject == thumb_)
      {
        thumb_ = null;
      }
    }

    void FixedUpdate()
    {
      if (index_ != null && thumb_ != null)
      {
        
      }
    }
  }
}

