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

  public float handModelPalmWidth = 0.085f;
  public FingerModel[] fingers = new FingerModel[NUM_FINGERS];

  // Unity references
  public Transform palm;
  public Transform forearm;
  public Transform wristJoint;
  public Transform elbowJoint;
  
  // Leap references
  protected Hand hand_;
  protected HandController controller_;

  protected bool mirror_z_axis_ = false;

  // QUESTION: Can handMovementScale and this method be removed?
  public Vector3 GetHandOffset() {
    if (controller_ == null || hand_ == null)
      return Vector3.zero;

    Vector3 additional_movement = controller_.handMovementScale - Vector3.one;
    Vector3 scaled_wrist_position = Vector3.Scale(additional_movement, hand_.WristPosition.ToUnityScaled(mirror_z_axis_));

    return controller_.transform.TransformPoint(scaled_wrist_position) -
           controller_.transform.position;
  }

  // Returns the palm position of the hand in global coordinates.
  public Vector3 GetPalmPosition() {
    if (controller_ != null && hand_ != null) {
      return controller_.transform.TransformPoint (hand_.PalmPosition.ToUnityScaled (mirror_z_axis_)) + GetHandOffset ();
    }
    if (palm) {
      return palm.position;
    }
    return Vector3.zero;
  }

  // Returns the palm rotation of the hand in global coordinates.
  public Quaternion GetPalmRotation() {
    if (controller_ != null && hand_ != null) {
      return controller_.transform.rotation * hand_.Basis.Rotation(mirror_z_axis_);
    }
    if (palm) {
      return palm.rotation;
    }
    return Quaternion.identity;
  }

  // Returns the palm direction of the hand in global coordinates.
  public Vector3 GetPalmDirection() {
    if (controller_ != null && hand_ != null) {
      return controller_.transform.TransformDirection(hand_.Direction.ToUnity(mirror_z_axis_));
    }
    if (palm) {
      return palm.forward;
    }
    return Vector3.forward;
  }

  // Returns the palm normal of the hand in global coordinates.
  public Vector3 GetPalmNormal() {
    if (controller_ != null && hand_ != null) {
      return controller_.transform.TransformDirection(hand_.PalmNormal.ToUnity(mirror_z_axis_));
    }
    if (palm) {
      return -palm.up;
    }
    return -Vector3.up;
  }

  // Returns the lower arm direction in global coordinates.
  public Vector3 GetArmDirection() {
    if (controller_ != null && hand_ != null) {
      return controller_.transform.TransformDirection(hand_.Arm.Direction.ToUnity(mirror_z_axis_));
    }
    if (forearm) {
      return forearm.forward;
    }
    return Vector3.forward;
  }

  // Returns the lower arm center in global coordinates.
  public Vector3 GetArmCenter() {
    if (controller_ != null && hand_ != null) {
      Vector leap_center = 0.5f * (hand_.Arm.WristPosition + hand_.Arm.ElbowPosition);
      return controller_.transform.TransformPoint (leap_center.ToUnityScaled (mirror_z_axis_)) + GetHandOffset ();
    }
    if (forearm) {
      return forearm.position;
    }
    return Vector3.zero;
  }

  // Returns the length of the forearm
  public float GetArmLength() {
    return (hand_.Arm.WristPosition - hand_.Arm.ElbowPosition).Magnitude * UnityVectorExtension.INPUT_SCALE;
  }
  
  // Returns the width of the forearm
  public float GetArmWidth() {
    return hand_.Arm.Width * UnityVectorExtension.INPUT_SCALE;
  }

  // Returns the lower arm elbow position in global coordinates.
  public Vector3 GetElbowPosition() {
    if (controller_ != null && hand_ != null) {
      Vector3 local_position = hand_.Arm.ElbowPosition.ToUnityScaled (mirror_z_axis_);
      return controller_.transform.TransformPoint (local_position) + GetHandOffset ();
    }
    if (elbowJoint) {
      return elbowJoint.position;
    }
    return Vector3.zero;
  }

  // Returns the lower arm wrist position in global coordinates.
  public Vector3 GetWristPosition() {
    if (controller_ != null && hand_ != null) {
      Vector3 local_position = hand_.Arm.WristPosition.ToUnityScaled (mirror_z_axis_);
      return controller_.transform.TransformPoint (local_position) + GetHandOffset ();
    }
    if (wristJoint) {
      return wristJoint.position;
    }
    return Vector3.zero;
  }

  // Returns the rotation quaternion of the arm in global coordinates.
  public Quaternion GetArmRotation() {
    if (controller_ != null && hand_ != null) {
      Quaternion local_rotation = hand_.Arm.Basis.Rotation (mirror_z_axis_);
      return controller_.transform.rotation * local_rotation;
    }
    if (forearm) {
      return forearm.rotation;
    }
    return Quaternion.identity;
  }

  public Hand GetLeapHand() {
    return hand_;
  }

  public void SetLeapHand(Hand hand) {
    hand_ = hand;
    for (int i = 0; i < fingers.Length; ++i) {
      if (fingers[i] != null) {
        fingers[i].SetLeapHand(hand_);
        fingers[i].SetOffset(GetHandOffset());
      }
    }
  }

  public void MirrorZAxis(bool mirror = true) {
    mirror_z_axis_ = mirror;
    for (int i = 0; i < fingers.Length; ++i) {
      if (fingers[i] != null)
        fingers[i].MirrorZAxis(mirror);
    }
  }

  public bool IsMirrored() {
    return mirror_z_axis_;
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

  public virtual void InitHand() {
    for (int f = 0; f < fingers.Length; ++f) {
      if (fingers[f] != null) {
        fingers[f].fingerType = (Finger.FingerType)f;
        fingers[f].InitFinger();
      }
    }

    UpdateHand ();
  }

  public abstract void UpdateHand();
}
