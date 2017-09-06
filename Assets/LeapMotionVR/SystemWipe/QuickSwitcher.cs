using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class QuickSwitcher : MonoBehaviour {

  public bool m_enabled = false;
  [SerializeField]
  private float m_minProgressToStartTransition;
  [SerializeField]
  private float m_fractionToLockTransition;
  [SerializeField]
  private Vector3 m_wipeOutPosition;

  private Vector3 m_startPosition;

  private enum TransitionState { ON, OFF, MANUAL, TWEENING };
  private TransitionState m_currentTransitionState;
  // Know what the last locked state was so we know what we're transitioning to.
  private TransitionState m_lastLockedState;

  // Where are we transitioning to and from
  private Vector3 m_from;
  private Vector3 m_to;

  private delegate void TweenCompleteDelegate();

  // Use this for initialization
  void Start() {
    m_startPosition = transform.localPosition;
    m_from = m_startPosition;
    m_to = m_wipeOutPosition;
    m_lastLockedState = TransitionState.ON;
    SystemWipeRecognizerListener.Instance.SystemWipeUpdate += onWipeUpdate;
    TweenToOffPosition();
  }

  private void onWipeUpdate(object sender, SystemWipeArgs eventArgs) {
    if (!m_enabled) { return; }

    string debugLine = "Debug";
    if (eventArgs.WipeInfo.Status == Leap.Util.Status.SwipeAbort) {
      debugLine += " | Abort";
      // If the user aborts, tween back to original location
      if (m_lastLockedState == TransitionState.ON) {
        TweenToOnPosition();
      } else {
        TweenToOffPosition();
      }
    }

    if (m_currentTransitionState == TransitionState.MANUAL) {
      debugLine += " | Manual Control";
      float fraction = Mathf.Clamp01(eventArgs.WipeInfo.Progress);

      debugLine += ": " + fraction;
      transform.localPosition = Vector3.Lerp(m_from, m_to, fraction);

      // If we're sure of the gesture, just go make the transition
      if (fraction >= m_fractionToLockTransition) {
        debugLine += " | Transition Cofirmed";
        if (m_lastLockedState == TransitionState.OFF) {
          TweenToOnPosition();
        } else {
          TweenToOffPosition();
        }
      }
    } else if (m_currentTransitionState == TransitionState.TWEENING) {
      debugLine += " | Currently Tweening";
      //Debug.Log(debugLine);
      return;
    } else { // We're either on or off
      debugLine += " | Locked";
      if (eventArgs.WipeInfo.Progress >= m_minProgressToStartTransition) {
        debugLine += " | Go To Manual";
        m_currentTransitionState = TransitionState.MANUAL;
      }
    }

    //Debug.Log(debugLine);
  }

  private void onOnPosition() {
    //Debug.Log("onOnPosition");
    m_currentTransitionState = TransitionState.ON;
    m_lastLockedState = TransitionState.ON;
    m_from = m_startPosition;
    m_to = m_wipeOutPosition;

    foreach (var controller in HandController.All) {
      controller.PhysicsEnabled = false;
      controller.GraphicsEnabled = false;
    }
  }

  private void onOffPosition() {
    //Debug.Log("onOffPosition");
    m_currentTransitionState = TransitionState.OFF;
    m_lastLockedState = TransitionState.OFF;
    m_from = m_wipeOutPosition;
    m_to = m_startPosition;

    foreach (var controller in HandController.All) {
      controller.PhysicsEnabled = true;
      controller.GraphicsEnabled = true;
    }
  }

  public void TweenToOnPosition() {
    //Debug.Log("tweenToOnPosition");
    StopAllCoroutines();
    StartCoroutine(doPositionTween(0.0f, 0.1f, onOnPosition));
  }

  public void TweenToOffPosition() {
    //Debug.Log("tweenToOffPosition");
    StopAllCoroutines();
    StartCoroutine(doPositionTween(1.0f, 0.1f, onOffPosition));
  }

  public void TweenToPosition(float fraction, float time = 0.4f) {
    m_currentTransitionState = TransitionState.TWEENING;
    StopAllCoroutines();
    StartCoroutine(doPositionTween(fraction, time));
  }

  private IEnumerator doPositionTween(float goalPercent, float transitionTime, TweenCompleteDelegate onComplete = null) {
    float startTime = Time.time;

    Vector3 from = transform.localPosition;
    Vector3 to = Vector3.Lerp(m_startPosition, m_wipeOutPosition, goalPercent);

    while (true) {
      float fraction = Mathf.Clamp01((Time.time - startTime) / transitionTime);
      //Debug.Log("Tween step: " + fraction);

      transform.localPosition = Vector3.Lerp(from, to, fraction);

      // Kick out of the loop if we're done
      if (fraction == 1) {
        break;
      } else { // otherwise continue
        yield return 1;
      }
    }

    if (onComplete != null) {
      onComplete();
    }
  }
}

