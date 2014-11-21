using UnityEngine;
using System.Collections;

namespace VRWidgets
{
  public class ScrollHandleBase : ButtonBase
  {
    public HandDetector handDetector;
    public ScrollViewerBase viewer;
    public ScrollContentBase content;

    private Vector3 this_pivot_ = Vector3.zero;
    private Vector3 target_pivot_ = Vector3.zero;
    private Vector3 content_pivot_ = Vector3.zero;

    private Vector3 prev_content_pos_ = Vector3.zero;

    private void AddContentMomentum()
    {
      content.rigidbody.velocity = (content.transform.position - prev_content_pos_) * 1 / (Time.deltaTime);
      //Vector3 local_velocity = transform.InverseTransformDirection((content.transform.position - prev_content_pos_) * 1 / (Time.deltaTime));
      //local_velocity.z = 0.0f;
      //content.rigidbody.velocity = transform.TransformDirection(local_velocity);
    }

    private void UpdatePosition(Vector3 displacement)
    {
      prev_content_pos_ = content.transform.position;
      transform.position = displacement + this_pivot_;
      content.transform.position = displacement + content_pivot_;

      Vector3 local_position = transform.localPosition;
      local_position.x = 0.0f;
      local_position.z = Mathf.Max(local_position.z, 0.0f);
      transform.localPosition = local_position;
    }

    public override void ButtonPressed()
    {
      if (handDetector.target)
      {
        this_pivot_ = transform.position;
        content_pivot_ = content.transform.position;
        target_pivot_ = handDetector.target.transform.position;
      }
      viewer.ScrollActive();
    }

    public override void ButtonReleased()
    {
      AddContentMomentum();
      transform.localPosition = Vector3.zero;
      Vector3 content_position = content.transform.localPosition;
      content_position.z = transform.localPosition.z;
      content.transform.localPosition = content_position;
      viewer.ScrollInactive();
    }

    public virtual void Start()
    {
      Limits viewer_limits = new Limits();
      viewer_limits.GetLimits(viewer.scrollWindow, gameObject);

      Vector3 local_scale = transform.localScale;
      local_scale.x = (viewer_limits.r - viewer_limits.l);
      local_scale.y = (viewer_limits.t - viewer_limits.b);
      transform.localScale = local_scale;

      prev_content_pos_ = content.transform.position;
    }

    // Update is called once per frame
    public override void Update()
    {
      base.Update();
      if (is_pressed_)
      {
        if (handDetector.target)
        {
          UpdatePosition(handDetector.target.transform.position - target_pivot_);
        }
      }
    }
  }
}

