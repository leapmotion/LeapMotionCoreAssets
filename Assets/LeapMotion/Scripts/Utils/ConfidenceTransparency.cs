/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2014.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

using UnityEngine;
using System.Collections;
using Leap;

// A Leap Motion hand script that set's the alpha
// of the hand based on the hand's self confidence value.
public class ConfidenceTransparency : MonoBehaviour {

  private Material material;

  void Start() {
    material = new Material(Shader.Find("Transparent/Diffuse"));
    Renderer[] renderers = GetComponentsInChildren<Renderer>();
    
    for (int i = 0; i < renderers.Length; ++i)
      renderers[i].material = material;
  }

  void Update() {
    Hand leap_hand = GetComponent<HandModel>().GetLeapHand();

    if (leap_hand != null) {
      Color new_color = material.color;
      new_color.a = leap_hand.Confidence;
      material.color = new_color;
    }
  }
}
