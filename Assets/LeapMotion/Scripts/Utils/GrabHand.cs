/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2014.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

using UnityEngine;
using System.Collections;
using Leap;

// NOTE: This script is very new and may change a lot in the near future.
// Leap Motion hand grab script. Will rotate the grabbed rigidbody with the hand.
public class GrabHand : MonoBehaviour {

  private const float TRIGGER_DISTANCE_RATIO = 0.7f;

  public float grabDistance = 2.0f;
  public float filtering = 0.5f;

  private bool pinching_;
  private Collider grabbed_;
  private Quaternion start_rotation_;

  private Vector3 pinch_position_;
  private Quaternion palm_rotation_;

  private float grabbed_max_angular_velocity_;

  void Start() {
    pinching_ = false;
    grabbed_ = null;
  }

  void OnDestroy() {
    OnRelease();
  }

  private void OnPinch(Vector3 pinch_position) {
    pinching_ = true;

    // Check if we pinched a movable object and grab the closest one that's not part of the hand.
    Collider[] close_things = Physics.OverlapSphere(pinch_position, grabDistance);
    Vector3 distance = new Vector3(grabDistance, 0.0f, 0.0f);

    HandModel hand_model = GetComponent<HandModel>();

    for (int j = 0; j < close_things.Length; ++j) {
      Vector3 new_distance = pinch_position - close_things[j].transform.position;
      if (close_things[j].rigidbody != null && new_distance.magnitude < distance.magnitude &&
          !close_things[j].transform.IsChildOf(transform)) {
        grabbed_ = close_things[j];
        distance = new_distance;
        pinch_position_ = close_things[j].transform.position;
      }
    }

    if (grabbed_ != null) {
      grabbed_max_angular_velocity_ = grabbed_.rigidbody.maxAngularVelocity;
      grabbed_.rigidbody.maxAngularVelocity = Mathf.Infinity;
      grabbed_.rigidbody.detectCollisions = false;
      palm_rotation_ = hand_model.GetPalmRotation();
      start_rotation_ = grabbed_.transform.rotation * Quaternion.Inverse(palm_rotation_);
    }
  }

  void OnRelease() {
    pinching_ = false;
    if (grabbed_ != null) {
      grabbed_.rigidbody.maxAngularVelocity = grabbed_max_angular_velocity_;
      grabbed_.rigidbody.detectCollisions = true;
    }
    grabbed_ = null;
  }

  void Update() {
    bool trigger_pinch = false;
    HandModel hand_model = GetComponent<HandModel>();
    Hand leap_hand = hand_model.GetLeapHand();

    if (leap_hand == null)
      return;

    // Scale trigger distance by thumb proximal bone length.
    Vector leap_thumb_tip = leap_hand.Fingers[0].TipPosition;
    float proximal_length = leap_hand.Fingers[0].Bone(Bone.BoneType.TYPE_PROXIMAL).Length;
    float trigger_distance = proximal_length * TRIGGER_DISTANCE_RATIO;

    // Check thumb tip distance to joints on all other fingers.
    // If it's close enough, start pinching.
    for (int i = 1; i < HandModel.NUM_FINGERS && !trigger_pinch; ++i) {
      Finger finger = leap_hand.Fingers[i];

      for (int j = 0; j < FingerModel.NUM_BONES && !trigger_pinch; ++j) {
        Vector leap_joint_position = finger.Bone((Bone.BoneType)j).NextJoint;
        if (leap_joint_position.DistanceTo(leap_thumb_tip) < trigger_distance)
          trigger_pinch = true;
      }
    }

    Vector3 pinch_position = hand_model.fingers[0].GetTipPosition();

    // Only change state if it's different.
    if (trigger_pinch && !pinching_)
      OnPinch(pinch_position);
    else if (!trigger_pinch && pinching_)
      OnRelease();

    // Move and rotate what we are grabbing toward the pinch.
    if (grabbed_ != null) {
      pinch_position_ += (1 - filtering) * (pinch_position - pinch_position_);
      Vector3 velocity = (pinch_position_ - grabbed_.transform.position) / Time.fixedDeltaTime;
      grabbed_.rigidbody.velocity = velocity;

      palm_rotation_ = Quaternion.Slerp(palm_rotation_, hand_model.GetPalmRotation(), filtering);
      Quaternion target_rotation = palm_rotation_ * start_rotation_;
      Quaternion delta_rotation = target_rotation *
                                  Quaternion.Inverse(grabbed_.transform.rotation);

      float angle = 0.0f;
      Vector3 axis = Vector3.zero;
      delta_rotation.ToAngleAxis(out angle, out axis);

      if (angle >= 180) {
        angle = 360 - angle;
        axis = -axis;
      }
      if (angle != 0)
        grabbed_.rigidbody.angularVelocity = angle * axis;
    }
  }
}
