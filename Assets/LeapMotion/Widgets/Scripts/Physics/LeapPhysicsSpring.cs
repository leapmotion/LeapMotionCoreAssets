using UnityEngine;

namespace LMWidgets
{
  /// <summary>
  /// Base class for spring. Restrains the widget in its local z-axis.
  /// It will apply spring physics in ApplyPhysics and translate the button with hand in ApplyInteractions
  /// </summary>
  public abstract class LeapPhysicsSpring : LeapPhysicsBase
  {
    /// <summary>
    /// Spring constant is separated to xyz-axis for more flexible configuration
    /// </summary>
    public Vector3 springConstant = Vector3.one * 1000.0f;
    
    /// <summary>
    /// Apply spring physics
    /// </summary>
    protected override void ApplyPhysics()
    {
      Vector3 localSpringConstant = Vector3.Scale(springConstant, transform.lossyScale);
      transform.localPosition += Vector3.Scale(-localSpringConstant * Time.deltaTime, transform.localPosition);
    }

    /// <summary>
    /// Translate the widget with the hand during interaction
    /// </summary>
    protected override void ApplyInteractions()
    {
      transform.localPosition = transform.parent.InverseTransformPoint(m_target.transform.position) - m_targetPivot + m_pivot;
    }
  }
}
