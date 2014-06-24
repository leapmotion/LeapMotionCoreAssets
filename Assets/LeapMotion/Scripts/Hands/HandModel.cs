/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2014.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

using UnityEngine;
using System.Collections;
using Leap;

// Interface for all hands.
public abstract class HandModel : MonoBehaviour {

  public const int NUM_FINGERS = 5;

  public FingerModel[] fingers = new FingerModel[NUM_FINGERS];

  private Hand hand_;
  private HandController controller_;

  public Vector3 GetPalmOffset() {
    if (controller_ == null)
      return Vector3.zero;

    Vector3 additional_movement = controller_.handMovementScale - Vector3.one;
    Vector3 scaled_palm_position = Vector3.Scale(additional_movement,
                                                 hand_.PalmPosition.ToUnityScaled());

    return controller_.transform.TransformPoint(scaled_palm_position) -
           controller_.transform.position;
  }

  // Returns the palm position of the hand in relation to the controller.
  public Vector3 GetPalmPosition() {
    return controller_.transform.TransformPoint(hand_.PalmPosition.ToUnityScaled()) +
           GetPalmOffset();
  }

  // Returns the palm rotation of the hand in relation to the controller.
  public Quaternion GetPalmRotation() {
    return GetController().transform.rotation * GetLeapHand().Basis.Rotation();
  }

  // Returns the palm direction of the hand in relation to the controller.
  public Vector3 GetPalmDirection() {
    return controller_.transform.TransformDirection(hand_.Direction.ToUnity());
  }

  // Returns the palm normal of the hand in relation to the controller.
  public Vector3 GetPalmNormal() {
    return controller_.transform.TransformDirection(hand_.PalmNormal.ToUnity());
  }

  // Returns the lower arm direction in relation to the controller.
  public Vector3 GetArmDirection() {
    return controller_.transform.TransformDirection(hand_.Arm.Direction.ToUnity());
  }

  // Returns the lower arm center in relation to the controller.
  public Vector3 GetArmCenter() {
    Vector leap_center = 0.5f * (hand_.Arm.WristPosition + hand_.Arm.ElbowPosition);
    return controller_.transform.TransformPoint(leap_center.ToUnityScaled());
  }

  // Returns the lower arm elbow position in relation to the controller.
  public Vector3 GetElbowPosition() {
    return controller_.transform.TransformPoint(hand_.Arm.ElbowPosition.ToUnityScaled());
  }

  // Returns the lower arm wrist position in relation to the controller.
  public Vector3 GetWristPosition() {
    return controller_.transform.TransformPoint(hand_.Arm.WristPosition.ToUnityScaled());
  }

  // Returns the rotation quaternion of the arm in relation to the controller.
  public Quaternion GetArmRotation() {
    Quaternion local_rotation = hand_.Arm.Basis.Rotation();
    return controller_.transform.rotation * local_rotation;
  }

  public Hand GetLeapHand() {
    return hand_;
  }

  public void SetLeapHand(Hand hand) {
    hand_ = hand;
    for (int i = 0; i < fingers.Length; ++i) {
      if (fingers[i] != null) {
        fingers[i].SetLeapHand(hand_);
        fingers[i].SetOffset(GetPalmOffset());
      }
    }
  }

  public HandController GetController() {
    return controller_;
  }

  public void SetController(HandController controller) {
    controller_ = controller;
    for (int i = 0; i < fingers.Length; ++i) {
      if (fingers[i] != null)
        fingers[i].SetController(controller_);
    }
  }

  public abstract void InitHand();

  public abstract void UpdateHand();

  protected void IgnoreCollisionsWithSelf() {
    Collider[] colliders = gameObject.GetComponentsInChildren<Collider>();
    for (int i = 0; i < colliders.Length; ++i)
      for (int j = i + 1; j < colliders.Length; ++j)
        Physics.IgnoreCollision(colliders[i], colliders[j]);
  }
}
