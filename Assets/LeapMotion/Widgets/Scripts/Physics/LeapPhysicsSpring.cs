using UnityEngine;

namespace LMWidgets
{
  public class LeapPhysicsSpring : LeapPhysicsBase
  {
    public float springConstant = 1000.0f;

    protected override void ApplyPhysics()
    {
      float scale = transform.lossyScale.z;
      float localSpringConstant = springConstant * scale;
      float springVelocity = -localSpringConstant * Time.deltaTime * transform.localPosition.z;
      transform.localPosition += new Vector3(0.0f, 0.0f, springVelocity);
    }

    protected override void ApplyConstraints()
    {
      Vector3 localPosition = transform.localPosition;
      localPosition.x = 0.0f;
      localPosition.y = 0.0f;
      localPosition.z = Mathf.Max(localPosition.z, 0.0f);
      transform.localPosition = localPosition;
    }
  }
}
