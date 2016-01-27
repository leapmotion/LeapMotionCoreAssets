using UnityEngine;
using System.Collections;

namespace Leap {
  public class HandDrop : HandFinishBehavior {
    public Vector3 startingPosition;
    public Quaternion startingOrientation;
    public Vector3 startingScale;

    // Use this for initialization
    void Awake() {
      startingPosition = transform.localPosition;
      startingOrientation = transform.rotation;
      startingScale = transform.localScale;
    }

    public override void HandFinish() {
      StartCoroutine(LerpToStart());
    }

    private IEnumerator LerpToStart() {
      transform.localScale = startingScale;
      float speed = 1.0F;
      Vector3 dropPosition = transform.localPosition;

      float elapsedTime = 0f;

      while (elapsedTime < speed) {
        Debug.Log("Dropping");
        transform.localPosition = Vector3.Lerp(dropPosition, startingPosition, (elapsedTime / speed));
        elapsedTime += Time.deltaTime;
        yield return new WaitForEndOfFrame();
      }

    }
  }
}
