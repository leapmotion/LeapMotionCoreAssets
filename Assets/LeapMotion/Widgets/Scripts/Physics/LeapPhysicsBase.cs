using UnityEngine;

namespace LMWidgets
{
  public enum LeapPhysicsState
  {
    Interacting, // Responsible for moving the widgets with the fingers
    Reflecting // Responsible for reflecting widget information and simulating the physics
  }

  public abstract class LeapPhysicsBase : MonoBehaviour
  {
    protected LeapPhysicsState m_state = LeapPhysicsState.Reflecting;
    protected GameObject m_target = null;
    protected Vector3 m_pivot = Vector3.zero;
    protected Vector3 m_targetPivot = Vector3.zero;

    // Apply the physics interactions when the hand is no longer interacting with the object
    protected abstract void ApplyPhysics();

    // Apply constraints for the object (e.g. Constrain movements along a specific axis)
    protected abstract void ApplyConstraints();

    // Let the object follow the hand
    private void ApplyInteraction()
    {
      transform.localPosition = transform.InverseTransformPoint(m_target.transform.position) - m_targetPivot + m_pivot;
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
        m_pivot = transform.localPosition;
        m_targetPivot = transform.InverseTransformPoint(m_target.transform.position);
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
        default:
          break;
      }
      ApplyConstraints();
    }
  }
}
