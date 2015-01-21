using UnityEngine;

namespace LMWidgets
{
  public enum LeapPhysicsState
  {
    Interacting, // Responsible for moving the widgets with the fingers
    Reflecting // Responsible for reflecting widget information and simulating the physics
  }

  [RequireComponent(typeof(Rigidbody))]
  public abstract class LeapPhysics : MonoBehaviour
  {
    protected LeapPhysicsState m_state = LeapPhysicsState.Reflecting;

    private GameObject m_target;
    private Vector3 m_pivot_;
    private Vector3 m_target_pivot_;

    protected abstract void ApplyPhysics();
    private void ApplyInteraction()
    {
                                                            
    }

    private bool IsHand(Collider other)
    {
      return other.transform.parent && other.transform.parent.parent && other.transform.parent.parent.GetComponent<HandModel>();
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
      // Change state to interacting if the collider entering is part of a hand
      if (m_target == null && IsHand(other))
      {
        m_target = other.gameObject;
        m_state = LeapPhysicsState.Interacting;
        m_pivot_ = transform.localPosition;
        m_target_pivot_ = transform.InverseTransformPoint(m_target.transform.position);
      }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
      // Change state to reflecting if the collider exiting is part of a hand
      if (other.gameObject == m_target)
      {
        m_target = null;
        m_state = LeapPhysicsState.Reflecting;
      }
    }

    protected virtual void FixedUpdate() 
    {
      if (m_target == null && m_state == LeapPhysicsState.Interacting)
      {
        m_state = LeapPhysicsState.Reflecting;
      }

      switch (m_state)
      {
        case LeapPhysicsState.Interacting:
          ApplyInteraction();
          break;
        case LeapPhysicsState.Reflecting:
          ApplyPhysics();
          break;
      }
    }
  }
}
