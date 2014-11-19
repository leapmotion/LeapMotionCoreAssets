using UnityEngine;
using System.Collections;

namespace VRWidgets
{
  [RequireComponent (typeof (Rigidbody))]
  public class ScrollContentBase : MonoBehaviour
  {
    public ScrollViewerBase scrollViewer;

    private float upper_limit_ = float.MinValue;
    private float lower_limit_ = float.MaxValue;

    public float GetPercent()
    {
      return (upper_limit_ != lower_limit_) ? (upper_limit_ - transform.localPosition.y) / (upper_limit_ - lower_limit_) : 0.0f;
    }

    public virtual void Start()
    {
      Limits content_limits = new Limits();
      content_limits.GetLimits(gameObject);
      Limits viewer_limits = new Limits();
      viewer_limits.GetLimits(scrollViewer.scrollWindow, gameObject);

      float viewer_height = viewer_limits.t - viewer_limits.b;

      if (content_limits.t - content_limits.b > viewer_height)
      {
        float y_offset = (content_limits.t + content_limits.b) / 2.0f - transform.localPosition.y;
        upper_limit_ = y_offset + content_limits.t - viewer_height / 2.0f;
        lower_limit_ = y_offset + content_limits.b + viewer_height / 2.0f;
      }
      else
      {
        upper_limit_ = 0.0f;
        lower_limit_ = 0.0f;
      }
    }

    private void ApplyConstraints()
    {
      Vector3 local_position = transform.localPosition;
      local_position.x = 0.0f;
      local_position.y = (local_position.y > upper_limit_) ? upper_limit_ : local_position.y;
      local_position.y = (local_position.y < lower_limit_) ? lower_limit_ : local_position.y;
      local_position.z = Mathf.Max(local_position.z, 0.0f);
      transform.localPosition = local_position;
    }

    // Update is called once per frame
    void LateUpdate()
    {
      ApplyConstraints();
    }
  }
}

