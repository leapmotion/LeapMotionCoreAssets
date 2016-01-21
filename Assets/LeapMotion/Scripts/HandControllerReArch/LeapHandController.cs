using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap;

namespace Leap {
  public class LeapHandController : MonoBehaviour {

    public LeapProvider Provider { get; set; }
    public HandFactory Factory { get; set; }


    public Dictionary<int, HandRepresentation> reps = new Dictionary<int, HandRepresentation>();

    // Use this for initialization
    void Start() {
      Provider = GetComponent<LeapProvider>();
      Factory = GetComponent<HandFactory>();

    }

    // Update is called once per frame
    void Update() {
      //Debug.Log("reps.Count:" + reps.Count);
      foreach (Leap.Hand curHand in Provider.CurrentFrame.Hands) {
        HandRepresentation rep;
        if (!reps.TryGetValue(curHand.Id, out rep)) {
          rep = Factory.MakeHandRepresentation(curHand);
          reps.Add(curHand.Id, rep);
          Debug.Log("reps.Add(" + curHand.Id + ", " + rep + ")");
        }
       
        rep.IsMarked = true;
        rep.UpdateRepresentation(curHand);
        rep.LastUpdatedTime = (int)Provider.CurrentFrame.Timestamp;
      }


      // TODO:  Mark-and-sweep or set difference implementation
      HandRepresentation toBeDeleted = null;
      foreach (KeyValuePair<int, HandRepresentation> r in reps) {
        if (r.Value != null) {
          if (r.Value.IsMarked) {
            //Debug.Log("LeapHandController Marking False");
            r.Value.IsMarked = false;
          }
          else {
            //TODO:  Initialize toBeDeleted with a value to be deleted
            Debug.Log("Finishing");
            toBeDeleted = r.Value;
          }
        }
      }
      if (toBeDeleted != null) {
        //Inform the representation that we will no longer be giving it any hand updates
        //because the corresponding hand has gone away
        reps.Remove(toBeDeleted.HandID);
        toBeDeleted.Finish();
      }
    }
  }
}
