using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class KeyEnableMonoBehaviors : MonoBehaviour {
  public List<MonoBehaviour> targets;
  [Header("Controls")]
  public KeyCode unlockHold = KeyCode.RightShift;
  public KeyCode toggle = KeyCode.T;
  
  // Update is called once per frame
  void Update () {
    if (unlockHold != KeyCode.None &&
        !Input.GetKey (unlockHold)) {
      return;
    }
    if (Input.GetKeyDown (toggle)) {
      foreach (MonoBehaviour target in targets) {
        target.enabled = !target.enabled;
      }
    }
  }
}
