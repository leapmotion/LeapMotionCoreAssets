using System;
using UnityEngine;

namespace LMWidgets
{
  public enum LeapPhysicsState
  {
    Interacting, // Responsible for moving the widgets with the fingers
    Reflecting // Responsible for reflecting widget information and simulating the physics
  }

  /// <summary>
  /// Base class for physics. 
  /// Handles state changes between Interacting and Reflecting.
  /// </summary>
  public abstract class LeapPhysicsBase : MonoBehaviour
  {
    protected event EventHandler<LMWidgets.EventArg<LeapPhysicsState>> StateChangeHandler;

    private LeapPhysicsState m_state = LeapPhysicsState.Reflecting; // Don't set this directly. Use accessor.
    protected GameObject m_target = null;
    protected Vector3 m_pivot = Vector3.zero;
    protected Vector3 m_targetPivot = Vector3.zero;

    protected LeapPhysicsState State { 
      get {
        return m_state; 
      }
      set {
        m_state = value;
        EventHandler<LMWidgets.EventArg<LeapPhysicsState>> handler = StateChangeHandler;
        if ( handler != null ) { handler(this, new EventArg<LeapPhysicsState>(State)); }
      }
    }

    // Apply the physics interactions when the hand is no longer interacting with the object
    protected abstract void ApplyPhysics();
    // Apply interactions with the objects
    protected abstract void ApplyInteractions();
    // Apply constraints for the object (e.g. Constrain movements along a specific axis)
    protected abstract void ApplyConstraints();

    /// <summary>
    /// Resets the pivots
    /// </summary>
    protected void ResetPivots()
    {
      m_pivot = transform.localPosition;
      if (m_target != null)
        m_targetPivot = transform.parent.InverseTransformPoint(m_target.transform.position);
    }

    /// <summary>
    /// Returns true or false by checking if "HandModel" exits in the parent of the collider
    /// </summary>
    /// <param name="collider"></param>
    /// <returns></returns>
    private bool IsHand(Collider collider)
    {
      return collider.transform.parent && collider.transform.parent.parent && collider.transform.parent.parent.GetComponent<HandModel>();
    }

    /// <summary>
    /// Change the state of the physics to "Interacting" if no other hands were interacting and if the collider is a hand
    /// </summary>
    /// <param name="collider"></param>
    protected virtual void OnTriggerEnter(Collider collider)
    {
      if (m_target == null && IsHand(collider))
      {
        State = LeapPhysicsState.Interacting;
        m_target = collider.gameObject;
        ResetPivots();
      }
    }

    /// <summary>
    /// Change the state of the physics to "Reflecting" if the object exiting is the hand
    /// </summary>
    /// <param name="collider"></param>
    protected virtual void OnTriggerExit(Collider collider)
    {
      // TODO: Use interpolation to determine if the hand should still continue interacting with the widget to solve low-FPS
      // TODO(cont): It should solve low-FPS or fast hand movement problems
      if (collider.gameObject == m_target)
      {
        State = LeapPhysicsState.Reflecting;
        m_target = null;
      }
    }

    protected virtual void Awake()
    {
      if (GetComponent<Collider>() == null)
      {
        Debug.LogWarning("This Widget lacks a collider. Will not function as expected.");
      }
    }

    protected virtual void FixedUpdate() 
    {
      if (m_target == null && State == LeapPhysicsState.Interacting)
      {
        State = LeapPhysicsState.Reflecting;
      }

      switch (State)
      {
        case LeapPhysicsState.Interacting:
          ApplyInteractions();
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
