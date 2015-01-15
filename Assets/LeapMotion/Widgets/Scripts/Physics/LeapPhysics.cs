using UnityEngine;
using System.Collections;

namespace LMWidgets
{
  enum LeapPhysicsState
  {
    Interacting,
    Reflecting
  }

  [RequireComponent(typeof(Rigidbody))]
  public class LeapPhysics : MonoBehaviour
  {
    protected LeapPhysicsState m_state = LeapPhysicsState.Reflecting;
    private GameObject m_target;

    private bool IsHand(Collider other)
    {
      return other.transform.parent && other.transform.parent.parent && other.transform.parent.parent.GetComponent<HandModel>();
    }

    void OnTriggerEnter(Collider other)
    {
      if (m_target == null && IsHand(other))
      {
        m_target = other.gameObject;
        m_state = LeapPhysicsState.Interacting;
      }
    }

    void OnTriggerExit(Collider other)
    {
      if (other.gameObject == m_target)
      {
        m_target = null;
      }
    }

    protected virtual void FixedUpdate() 
    {
      if (m_target == null && m_state == LeapPhysicsState.Interacting)
      {
        m_state = LeapPhysicsState.Reflecting;
      }
    }
  }
}
