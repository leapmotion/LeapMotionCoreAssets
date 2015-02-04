using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

namespace LMWidgets
{
  public abstract class ScrollTextBase : LeapPhysicsSpring, AnalogInteractionHandler<float>
  {
    // Binary Interaction Handler - Fires when interaction with the widget starts.
    public event EventHandler<LMWidgets.EventArg<float>> StartHandler;
    // Analog Interaction Handler - Fires while widget is being interacted with.
    public event EventHandler<LMWidgets.EventArg<float>> ChangeHandler;
    // Binary Interaction Handler - Fires when interaction with the widget ends.
    public event EventHandler<LMWidgets.EventArg<float>> EndHandler;

    public GameObject content;

    public float triggerDistance = 0.025f;
    public float cushionThickness = 0.005f;

    protected ExponentialSmoothingXYZ m_scrollVelocity = new ExponentialSmoothingXYZ(0.5f);
    private Vector3 m_scrollPivot = Vector3.zero;
    private Vector3 m_contentPivot = Vector3.zero;

    protected float m_localTriggerDistance;
    protected float m_localCushionThickness;
    protected bool m_isPressed = false;

    protected virtual void scrollPressed() {
      fireScrollStart(content.transform.localPosition.y);
    }

    protected virtual void scrollReleased() {
      fireScrollEnd(content.transform.localPosition.y);
    }

    protected virtual void fireScrollStart(float value) {
      EventHandler<LMWidgets.EventArg<float>> handler = StartHandler;
      if (handler != null) {
        handler (this, new LMWidgets.EventArg<float> (value));
      }
    }
    
    protected virtual void fireScrollChanged(float value) {
      EventHandler<LMWidgets.EventArg<float>> handler = ChangeHandler;
      if (handler != null) {
        handler (this, new LMWidgets.EventArg<float> (value));
      }
    }
    
    protected virtual void fireScrollEnd(float value) {
      EventHandler<LMWidgets.EventArg<float>> handler = EndHandler;
      if (handler != null) {
        handler (this, new LMWidgets.EventArg<float> (value));
      }
    }

    /// <summary>
    /// Update the content position based on how the scroll has moved. Will also save the momentum
    /// </summary>
    private void UpdateContentPosition()
    {
      Vector3 prevPosition = content.transform.localPosition;
      Vector3 contentLocalPosition = content.transform.localPosition;
      contentLocalPosition = transform.localPosition - m_scrollPivot + m_contentPivot;
      contentLocalPosition.z = Mathf.Max(contentLocalPosition.z, 0.0f);
      content.transform.localPosition = contentLocalPosition;
      Vector3 currPosition = content.transform.localPosition;
      Vector3 contentVelocity = (currPosition - prevPosition) / Time.deltaTime;
      m_scrollVelocity.Calculate(contentVelocity.x, contentVelocity.y, contentVelocity.z);
    }

    /// <summary>
    /// Constrain the scroll to the z-axis
    /// </summary>
    protected override void ApplyConstraints()
    {
      Vector3 localPosition = transform.localPosition;
      localPosition.z = Mathf.Max(localPosition.z, 0.0f);
      transform.localPosition = localPosition;
    }

    /// <summary>
    /// Check if the scroll is being pressed or not
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
          scrollPressed();
          m_scrollPivot = transform.localPosition;
          m_contentPivot = content.transform.localPosition;
        }
      }
      else if (m_isPressed == true)
      {
        if (transform.localPosition.z < (m_localTriggerDistance - m_localCushionThickness))
        {
          m_isPressed = false;
          scrollReleased();
          content.rigidbody2D.velocity = new Vector2(m_scrollVelocity.X, m_scrollVelocity.Y);
        }
      }
    }

    protected virtual void Start()
    {
      cushionThickness = Mathf.Clamp(cushionThickness, 0.0f, triggerDistance - 0.001f);
    }

    protected override void FixedUpdate()
    {
      base.FixedUpdate();
      if (m_isPressed)
      {
        UpdateContentPosition();
        fireScrollChanged(content.transform.localPosition.y);
      }

      // Set content velocity to zero once it's bouncing from the edges (ScrollRect vel > 0)
      if (Mathf.Abs(content.transform.parent.GetComponent<ScrollRect>().velocity.y) > 0.001f)
      {
        content.rigidbody2D.velocity = Vector2.zero;
      }
    }

    protected virtual void Update()
    {
      CheckTrigger();
    }
  }
}
