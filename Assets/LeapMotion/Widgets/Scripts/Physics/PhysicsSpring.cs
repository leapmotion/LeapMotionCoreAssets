using UnityEngine;
using System.Collections;

namespace LMWidgets
{
  public class PhysicsSpring : LeapPhysics
  {
    public float spring = 1000.0f;

    protected float m_scaled_spring;

    protected override void ApplyPhysics()
    {
    }

    private void ApplyDisplacements()
    {

    }

    private void ApplySpring()
    {
      // Apply Spring
    }

    private void ApplyConstraints()
    {

    }

    protected override void FixedUpdate()
    {
      base.FixedUpdate();
      switch (m_state)
      {
        case LeapPhysicsState.Interacting:
          ApplyDisplacements();
          break;
        case LeapPhysicsState.Reflecting:
          ApplySpring();
          break;
        default:
          ApplySpring();
          break;
      }
      ApplyConstraints();
    }
  }
}
