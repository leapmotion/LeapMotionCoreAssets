using UnityEngine;
using System.Collections;
using LeapInternal;
using Leap;

namespace Leap {
  public class LeapProvider :
    MonoBehaviour
  {
    public Frame CurrentFrame { get; private set; }
    public Image CurrentImage { get; private set; }
    private Transform providerSpace;

    public Connection connection { get; set; }

    /** The smoothed offset between the FixedUpdate timeline and the Leap timeline.  
   * Used to provide temporally correct frames within FixedUpdate */
  private SmoothedFloat smoothedFixedUpdateOffset_ = new SmoothedFloat();
  /** The maximum offset calculated per frame */
  [HideInInspector]
  public float PerFrameFixedUpdateOffset;
     /** Conversion factor for millimeters to meters. */
  protected const float MM_TO_M = 1e-3f;
  /** Conversion factor for nanoseconds to seconds. */
  protected const float NS_TO_S = 1e-6f;
  /** Conversion factor for seconds to nanoseconds. */
  protected const float S_TO_NS = 1e6f;
  /** How much smoothing to use when calculating the FixedUpdate offset. */
  protected const float FIXED_UPDATE_OFFSET_SMOOTHING_DELAY = 0.1f;
    void Awake() {
      connection = Connection.GetConnection();

    }

    // Use this for initialization
    void Start() {

      //set empty frame
      CurrentFrame = new Frame();
    }

    // Update is called once per frame
    void Update() {
      CurrentFrame = connection.Frames.Get();
      //Debug.Log(CurrentFrame);

    }

    void FixedUpdate() {
      //which frame to deliver
    }
    public virtual Frame GetFixedFrame() {

      //Aproximate the correct timestamp given the current fixed time
      float correctedTimestamp = (Time.fixedTime + smoothedFixedUpdateOffset_.value) * S_TO_NS;

      //Search the leap history for a frame with a timestamp closest to the corrected timestamp
      Frame closestFrame = connection.Frames.Get();
      for (int searchHistoryIndex = 1; searchHistoryIndex < 60; searchHistoryIndex++) {
        Frame historyFrame = connection.Frames.Get(searchHistoryIndex);

        //If we reach an invalid frame, terminate the search
        if (!historyFrame.IsValid) {
          historyFrame.Dispose();
          break;
        }

        if (Mathf.Abs(historyFrame.Timestamp - correctedTimestamp) < Mathf.Abs(closestFrame.Timestamp - correctedTimestamp)) {
          closestFrame.Dispose();
          closestFrame = historyFrame;
        }
        else {
          //Since frames are always reported in order, we can terminate the search once we stop finding a closer frame
          historyFrame.Dispose();
          break;
        }
      }
      return closestFrame; 
    }
    void OnDestroy() {
      connection.Stop();
    }
  }
}
