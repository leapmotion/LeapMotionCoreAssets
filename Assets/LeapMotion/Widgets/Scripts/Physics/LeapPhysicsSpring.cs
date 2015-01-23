using UnityEngine;

namespace LMWidgets
{
  public class LeapPhysicsSpring : LeapPhysicsBase
  {
    public float springConstant = 1000.0f;
    public float minimumDistance = float.MinValue;
    public float maximumDistance = float.MaxValue;

    private Vector3 m_springVelocity = Vector3.zero;

    protected override void ApplyPhysics()
    {
      float scale = transform.lossyScale.z;
      float localSpringConstant = springConstant * scale;

      m_springVelocity.z += -localSpringConstant * Time.deltaTime;
      transform.position += transform.TransformDirection(m_springVelocity) * Time.deltaTime;
    }

    protected override void ApplyConstraints()
    {
      transform.localPosition.Scale(new Vector3(0.0f, 0.0f, 1.0f));
    }
  }
}
