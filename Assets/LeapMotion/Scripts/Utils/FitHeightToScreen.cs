/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2014.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

using UnityEngine;

public class FitHeightToScreen : MonoBehaviour {

  void Awake() {
    float width_height_ratio = GetComponent<Sprite>().rect.width / GetComponent<Sprite>().rect.width;
    float width = width_height_ratio * Screen.height;
    float x_offset = (Screen.width - width) / 2.0f;
    GetComponent<UnityEngine.UI.Image>().SetClipRect(new Rect(x_offset, 0.0f, width, Screen.height), true);
  }
}

