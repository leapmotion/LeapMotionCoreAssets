using UnityEngine;
using System.Collections;

namespace LMWidgets
{
  public abstract class SliderBase : LeapPhysicsSpring
  {
    public float triggerDistance = 0.025f;
    public float cushionThickness = 0.005f;

    public GameObject upperLimit;
    public GameObject lowerLimit;

    protected float m_localTriggerDistance;
    protected float m_localCushionThickness;
    protected bool m_isPressed = false;

    public abstract void SliderPressed();
    protected void FireSliderPressed()
    {
      ApplyInteractionConstraints(Vector3.one);
      SliderPressed();
    }

    public abstract void SliderReleased();
    private void FireSliderReleased()
    {
      ApplyInteractionConstraints(new Vector3(0.0f, 0.0f, 1.0f));
      SliderReleased();
    }

    /// <summary>
    /// Returns the fraction of the slider between lower and upper limit. 0.0 = At Lower. 1.0 = At Upper
    /// </summary>
    /// <returns></returns>
    public float GetSliderFraction()
    {
      float lowerLimitValue = lowerLimit.transform.localPosition.x;
      float upperLimitValue = upperLimit.transform.localPosition.x;
      if (lowerLimitValue <= upperLimitValue)
        return 0.0f;
      else
        return (transform.localPosition.x + lowerLimitValue) / (upperLimitValue - lowerLimitValue);
    }

    /// <summary>
    /// Returns the fraction of how much the handle is pressed down. 0.0 = At Rest. 1.0 = At Triggered Distance
    /// </summary>
    /// <returns></returns>
    public float GetHandleFraction()
    {
      if (m_localTriggerDistance == 0.0f)
        return 0.0f;
      else
      {
        float scale = transform.lossyScale.z;
        float fraction = transform.localPosition.z / m_localTriggerDistance;
        return Mathf.Clamp(fraction, 0.0f, 1.0f);
      }
    }

    /// <summary>
    /// Constrain the slider to y-axis and z-axis
    /// </summary>
    protected override void ApplyConstraints()
    {
      Vector3 localPosition = transform.localPosition;
      localPosition.x = Mathf.Clamp(localPosition.x, lowerLimit.transform.localPosition.x, upperLimit.transform.localPosition.x);
      localPosition.y = 0.0f;
      localPosition.z = Mathf.Max(localPosition.z, 0.0f);
      transform.localPosition = localPosition;
    }

    /// <summary>
    /// Check if the slider is being pressed or not
    /// </summary>
    private void CheckTrigger()
    {
      float scale = transform.lossyScale.z;
      m_localTriggerDistance = triggerDistance / scale;
      m_localCushionThickness = Mathf.Clamp(cushionThickness / scale, 0.0f, m_localTriggerDistance - 0.001f);
      if (m_isPressed == false)
      {
        if (transform.localPosition.z > m_localTriggerDistance)
        {
          m_isPressed = true;
          FireSliderPressed();
        }
      }
      else if (m_isPressed == true)
      {
        if (transform.localPosition.z < (m_localTriggerDistance - m_localCushionThickness))
        {
          m_isPressed = false;
          FireSliderReleased();
        }
      }
    }

    protected virtual void Update()
    {
      CheckTrigger();
    }
  }
}

