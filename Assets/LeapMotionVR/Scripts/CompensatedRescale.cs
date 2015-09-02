using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Enables rescaling of an object while preventing rescaling of specified child objects
/// </summary>
public class CompensatedRescale : MonoBehaviour {
  [Header("Scale-Invariant Children")]
  public List<Transform> compensated;
  [Header("Control Keys")]
  public KeyCode unlockHold = KeyCode.RightShift;
  public KeyCode resetScale = KeyCode.R;
  public KeyCode increaseScale = KeyCode.Equals;
  public KeyCode decreaseScale = KeyCode.Minus;
  [Range(0,1)]
  public float decreaseFactor = 0.625f; //40 mm CFS / 64 mm IPD

  [Range(0.25f,4f)]
  public float newScaleFactor = 1f;
  private float oldScaleFactor = 1f;

  private Vector3 initialScale;

	// Use this for initialization
	void OnEnable () {
    initialScale = transform.localScale;
	}

  void OnDisable () {
    ResetScale ();
  }
  
	// Update is called once per frame
	void Update () {
	  if (unlockHold != KeyCode.None &&
        !Input.GetKey (unlockHold)) {
      return;
    }
    if (Input.GetKeyDown (resetScale)) {
      ResetScale();
      return;
    }
    if (Input.GetKeyDown (increaseScale)) {
      IncreaseScale();
      Debug.Log ("IncreaseScale");
      return;
    }
    if (Input.GetKeyDown (decreaseScale)) {
      DecreaseScale();
      Debug.Log ("DecreaseScale");
      return;
    }

    if (oldScaleFactor != newScaleFactor) {
      ApplyRescale (newScaleFactor / oldScaleFactor);
      oldScaleFactor = newScaleFactor;
      Debug.Log("newScaleFactor = " + newScaleFactor);
    }
  }

  public void ResetScale() {
    oldScaleFactor = newScaleFactor = 1f;

    float multiplier = (
      (initialScale.x / transform.localScale.x) + 
      (initialScale.y / transform.localScale.y) +
      (initialScale.z / transform.localScale.z)
      ) / 3f;
    ApplyRescale(multiplier);
  }

  public void IncreaseScale() {
    ApplyRescale(1f / decreaseFactor);
  }
  
  public void DecreaseScale() {
    ApplyRescale(decreaseFactor);
  }

  void ApplyRescale(float multiplier) {
    transform.localScale *= multiplier;
    foreach (Transform child in compensated) {
      child.localScale /= multiplier;
    }
  }
}
