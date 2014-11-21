using UnityEngine;
using System.Collections;

namespace VRWidgets
{
  public abstract class SliderBase : ButtonBase
  {
    public GameObject upperLimit;
    public GameObject lowerLimit;

    private HandDetector handDetector;
    private Vector3 pivot_ = Vector3.zero;
    private Vector3 target_pivot_ = Vector3.zero;

    public abstract void SliderPressed();
    public abstract void SliderReleased();
    
    // When button is pressed, set the pivot and target_pivot in preparation for moving the handle
    public override void ButtonPressed()
    {
      if (handDetector.target)
      {
        pivot_ = transform.localPosition;
        target_pivot_ = transform.parent.InverseTransformPoint(handDetector.target.transform.position);
      }
      SliderPressed();
    }

    // When button is released, reset the target_pivot
    public override void ButtonReleased()
    {
      handDetector.ResetTarget();
      SliderReleased();
    }

    // Updates the position of the handle based on the displacement of the target
    public virtual void UpdatePosition(Vector3 displacement)
    {
      Vector3 local_position = transform.localPosition;
      local_position.x = displacement.x + pivot_.x;
      transform.localPosition = local_position;
      ApplyConstraints();
    }

    // Apply constraint to prevent the slider by going pass the lower and upper limits
    protected override void ApplyConstraints()
    {
      Vector3 local_position = transform.localPosition;
      local_position.x = Mathf.Clamp(local_position.x, lowerLimit.transform.localPosition.x, upperLimit.transform.localPosition.x);
      local_position.y = 0.0f;
      local_position.z = Mathf.Clamp(local_position.z, min_distance_, max_distance_);
      transform.localPosition = local_position;
      transform.rigidbody.velocity = Vector3.zero;
    }

    public override void Awake()
    {
      base.Awake();
      handDetector = GetComponentInChildren<HandDetector>();
    }

    // Update is called once per frame
    public override void Update()
    {
      base.Update();
      if (is_pressed_)
      {
        // If the button is pressed, update the button based on the movement of the target
        if (handDetector.target)
        {
          UpdatePosition(transform.parent.InverseTransformPoint(handDetector.target.transform.position) - target_pivot_);
        }
      }
    }
  }
}

