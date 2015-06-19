using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap;

/// <summary>
/// Maintains a history of transform data, with time-stamps set by the Leap service
/// </summary>
public class TransformHistory : MonoBehaviour {
  public struct TransformData {
    /// QUESTION: What is the type & precision of a timeStamp?
    public float dataTime;
    public Vector3 position;
    public Quaternion rotation;
    public TransformData (float dataTime, Vector3 position, Quaternion rotation) {
      this.dataTime = dataTime;
      this.position = position;
      this.rotation = rotation;
    }

    public static TransformData Lerp(TransformData from, TransformData to, float time) {
      if (from.dataTime == to.dataTime) {
        return from;
      }
      float fraction = (time - from.dataTime) / (to.dataTime - from.dataTime);
      return new TransformData (
        time, 
        Vector3.Lerp (from.position, to.position, fraction), 
        Quaternion.Slerp (from.rotation, to.rotation, fraction)
        );
    }
  }
  
  public HandController handController;
  public float maxTime = 2f;

  protected List<TransformData> history;
  protected Controller leapController;

	// Use this for initialization
	void Start () {
	  if (handController == null) {
      Debug.LogWarning ("TransformHistory REQUIRES a reference to a HandController");
      return;
    }
    leapController = handController.GetLeapController ();
    history = new List<TransformData> ();
	}
	
  // IMPORTANT: This call must be immediately after Oculus position tracker is applied
	void LateUpdate () {
    if (leapController == null) {
      return;
    }

    // Append latest position & rotation to head
    // FIXME: When leap API is updated replace Time.time with leapController.time
    // LEAP_EXPORT int64_t now() const;
    float now = Time.time;
    history.Add (new TransformData (now, transform.position, transform.rotation));
    //Debug.Log ("Last Index Time = " + history[history.Count-1].dataTime + " =? " + now);

    // Reduce history length
    while (history.Count > 0 &&
      maxTime < now - history [0].dataTime) {
      Debug.Log ("Removing oldest from history.Count = " + history.Count);
      history.RemoveAt(0);
    }
	}

  /// <summary>
  /// Estimates the transform at the specified time
  /// </summary>
  /// <returns>
  /// A transform with dataTime == time only if interpolation was possible
  /// </returns>
  public TransformData TransformAtTime(float time) {
    if (history.Count < 1) {
      Debug.LogWarning ("NO HISTORY!");
      return new TransformData () {
        dataTime = 0f,
        position = Vector3.zero,
        rotation = Quaternion.identity
      };
    }
    if (history [0].dataTime > time) {
      Debug.LogWarning ("NO INTERPOLATION: history.First = " + history [0].dataTime + " > time = " + time);
      return history[0];
    }
    int t = 1;
    while (t < history.Count &&
      history[t].dataTime < time) {
      t++;
    }
    if (!(t < history.Count)) {
      Debug.LogWarning ("NO INTERPOLATION: history.Last = " + history[history.Count-1].dataTime + " < time = " + time);
      return history[history.Count-1];
    }

    return TransformData.Lerp (history[t-1], history[t], time);
  }
}
