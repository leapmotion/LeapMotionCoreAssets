/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2014.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

using UnityEngine;
using System.Collections;
using Leap;

// Class to setup a rigged hand based on a model.
public class RiggedHand : HandModel {

  public Transform palm;
  public Transform foreArm;

  public override void InitHand() {
    UpdateHand();
  }

  public override void UpdateHand() {
    if (palm != null) {
      palm.position = GetPalmPosition();
      palm.rotation = GetPalmRotation();
    }

    if (foreArm != null)
      foreArm.rotation = GetArmRotation();

    for (int i = 0; i < fingers.Length; ++i) {
      if (fingers[i] != null)
        fingers[i].UpdateFinger();
    }
  }
}
