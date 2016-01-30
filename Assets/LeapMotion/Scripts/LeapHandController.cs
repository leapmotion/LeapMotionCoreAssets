using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap;

namespace Leap {
  public class LeapHandController : MonoBehaviour {
    /** Reverses the z axis. */
    public bool mirrorZAxis = false;
    /** The scale factors for hand movement. Set greater than 1 to give the hands a greater range of motion. */
    public Vector3 handMovementScale = Vector3.one;

    public LeapProvider Provider { get; set; }
    public HandFactory Factory { get; set; }

    public Dictionary<int, HandRepresentation> graphicsReps = new Dictionary<int, HandRepresentation>();
    public Dictionary<int, HandRepresentation> physicsReps = new Dictionary<int, HandRepresentation>();

    /** The smoothed offset between the FixedUpdate timeline and the Leap timeline.  
 * Used to provide temporally correct frames within FixedUpdate */
    private SmoothedFloat smoothedFixedUpdateOffset_ = new SmoothedFloat();
    /** The maximum offset calculated per frame */
    private float perFrameFixedUpdateOffset_;

    // Reference distance from thumb base to pinky base in mm.
    protected const float GIZMO_SCALE = 5.0f;
    /** Conversion factor for millimeters to meters. */
    protected const float MM_TO_M = 1e-3f;
    /** Conversion factor for nanoseconds to seconds. */
    protected const float NS_TO_S = 1e-6f;
    /** Conversion factor for seconds to nanoseconds. */
    protected const float S_TO_NS = 1e6f;
    /** How much smoothing to use when calculating the FixedUpdate offset. */
    protected const float FIXED_UPDATE_OFFSET_SMOOTHING_DELAY = 0.1f;

    protected bool graphicsEnabled = true;
    protected bool physicsEnabled = true;

    public bool GraphicsEnabled {
      get {
        return graphicsEnabled;
      }
      set {
        graphicsEnabled = value;
        if (!graphicsEnabled) {
          //DestroyGraphicsHands();
        }
      }
    }

    public bool PhysicsEnabled {
      get {
        return physicsEnabled;
      }
      set {
        physicsEnabled = value;
        if (!physicsEnabled) {
          //DestroyPhysicsHands();
        }
      }
    }
    private long prev_graphics_id_ = 0;
    private long prev_physics_id_ = 0;

    /** Draws the Leap Motion gizmo when in the Unity editor. */
    void OnDrawGizmos() {
      // Draws the little Leap Motion Controller in the Editor view.
      Gizmos.matrix = Matrix4x4.Scale(GIZMO_SCALE * Vector3.one);
      Gizmos.DrawIcon(transform.position, "leap_motion.png");
    }

    // Use this for initialization
    void Start() {
      Provider = GetComponent<LeapProvider>();
      Factory = GetComponent<HandFactory>();
    }
    /**
    * Turns off collisions between the specified GameObject and all hands.
    * Subject to the limitations of Unity Physics.IgnoreCollisions(). 
    * See http://docs.unity3d.com/ScriptReference/Physics.IgnoreCollision.html.
    */
    public void IgnoreCollisionsWithHands(GameObject to_ignore, bool ignore = true) {
      foreach (HandRepresentation rep in physicsReps.Values) {
        Leap.Utils.IgnoreCollisions(rep.handModel.gameObject, to_ignore, ignore);
      }
    }
    void Update() {
      Frame frame = Provider.CurrentFrame;
      if (frame.Id != prev_graphics_id_ && graphicsEnabled) {
        UpdateHandRepresentations(graphicsReps, HandModel.ModelType.Graphics);
        prev_graphics_id_ = frame.Id;

      }
    }

    void UpdateHandRepresentations(Dictionary<int, HandRepresentation> all_hand_reps, HandModel.ModelType modelType) {
      foreach (Leap.Hand curHand in Provider.CurrentFrame.Hands) {
        // If we've mirrored since this hand was updated, destroy it.
        if (all_hand_reps.ContainsKey(curHand.Id) &&
            all_hand_reps[curHand.Id].handModel.IsMirrored() != mirrorZAxis) {
          all_hand_reps[curHand.Id].Finish();
          all_hand_reps.Remove(curHand.Id);
        }

        HandRepresentation rep;
        if (!all_hand_reps.TryGetValue(curHand.Id, out rep)) {
          rep = Factory.MakeHandRepresentation(curHand, modelType);
          if (rep != null) {
            all_hand_reps.Add(curHand.Id, rep);
            rep.handModel.MirrorZAxis(mirrorZAxis);

            float hand_scale = MM_TO_M * curHand.PalmWidth / rep.handModel.handModelPalmWidth;
            rep.handModel.transform.localScale = hand_scale * Vector3.one;
            Debug.Log("reps.Add(" + curHand.Id + ", " + rep + ")");
          }
        }
        if (rep != null) {
          rep.IsMarked = true;
          rep.handModel.MirrorZAxis(mirrorZAxis);

          float hand_scale = MM_TO_M * curHand.PalmWidth / rep.handModel.handModelPalmWidth;
          rep.handModel.transform.localScale = hand_scale * Vector3.one;

          rep.UpdateRepresentation(curHand, modelType);
          rep.LastUpdatedTime = (int)Provider.CurrentFrame.Timestamp;
        }
      }

      //Mark-and-sweep or set difference implementation
      HandRepresentation toBeDeleted = null;
      foreach (KeyValuePair<int, HandRepresentation> r in all_hand_reps) {
        if (r.Value != null) {
          if (r.Value.IsMarked) {
            //Debug.Log("LeapHandController Marking False");
            r.Value.IsMarked = false;
          }
          else {
            //Initialize toBeDeleted with a value to be deleted
            Debug.Log("Finishing");
            toBeDeleted = r.Value;
          }
        }
      }
      //Inform the representation that we will no longer be giving it any hand updates
      //because the corresponding hand has gone away
      if (toBeDeleted != null) {
        all_hand_reps.Remove(toBeDeleted.HandID);
        toBeDeleted.Finish();
      }
    }
    /** Updates the physics objects */
    protected virtual void FixedUpdate() {
      //All FixedUpdates of a frame happen before Update, so only the last of these calculations is passed
      //into Update for smoothing.
      using (var latestFrame = Provider.CurrentFrame) {
        Provider.PerFrameFixedUpdateOffset = latestFrame.Timestamp * NS_TO_S - Time.fixedTime;
      }

      Frame frame = Provider.GetFixedFrame();

      if (frame.Id != prev_physics_id_ && physicsEnabled) {
        UpdateHandRepresentations(physicsReps, HandModel.ModelType.Physics);
        //UpdateHandModels(hand_physics_, frame.Hands, leftPhysicsModel, rightPhysicsModel);
        prev_physics_id_ = frame.Id;
      }
    }
  }
}
