/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2014.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

using UnityEngine;
using System.Collections;

public class Grabbable : MonoBehaviour {

  public bool keepDistanceWhenGrabbed = false;
  public bool preferredOrientation = false;
  public Vector3 objectOrientation;
  public Vector3 palmOrientation;

  public Rigidbody[] ignoreOnGrab;

  public Joint breakableJoint;
  public float breakForce;
  public float breakTorque;

  public void OnGrab(){
    for (int i = 0; i < ignoreOnGrab.Length; ++i)
      ignoreOnGrab[i].detectCollisions = false;

    if (breakableJoint != null) {
      breakableJoint.breakForce = breakForce;
      breakableJoint.breakTorque = breakTorque;
    }
  }

  public void OnRelease(){
    for (int i = 0; i < ignoreOnGrab.Length; ++i)
      ignoreOnGrab[i].detectCollisions = true;

    if (breakableJoint != null) {
      breakableJoint.breakForce = Mathf.Infinity;
      breakableJoint.breakTorque = Mathf.Infinity;
    }
  }
}
