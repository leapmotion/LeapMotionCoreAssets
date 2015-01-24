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

    private Vector3 m_interactionConstraints = Vector3.one;

    /// <summary>
    /// Applies Interaction constraints. Takes in a Vector3. If an axis has value > 0.5 then it's allowed to move. Otherwise it won't be
    /// </summary>
    /// <param name="interactionConstraints"></param>
    protected void ApplyInteractionConstraints(Vector3 interactionConstraints)
    {
      interactionConstraints.x = (interactionConstraints.x > 0.5f) ? 1.0f : 0.0f;
      interactionConstraints.y = (interactionConstraints.y > 0.5f) ? 1.0f : 0.0f;
      interactionConstraints.z = (interactionConstraints.z > 0.5f) ? 1.0f : 0.0f;
      m_interactionConstraints = interactionConstraints;
      ResetPivots();
    }

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
      Vector3 displacement = Vector3.Scale(transform.parent.InverseTransformPoint(m_target.transform.position) - m_targetPivot, m_interactionConstraints);
      transform.localPosition = displacement + m_pivot;
    }
  }
}
