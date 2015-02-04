using UnityEngine;
using System.Collections;
using LMWidgets;

public class ScrollTextDemo : ScrollTextBase 
{
  public float contentLimit;

  protected override void scrollPressed()
  {
    base.scrollPressed();
  }

  protected override void scrollReleased()
  {
    base.scrollReleased();
    // Don't allow the scroll to move sideways because the content is only 1-dimensional
    m_scrollVelocity.X = 0.0f;
  }

  private void ApplyContentConstraints()
  {
    Vector3 content_position = content.transform.localPosition;
    content_position.x = 0.0f;
    content_position.z = Mathf.Min(transform.localPosition.z, contentLimit);
    content.transform.localPosition = content_position;
  }

  protected override void FixedUpdate()
  {
    base.FixedUpdate();
    ApplyContentConstraints();
  }
}
