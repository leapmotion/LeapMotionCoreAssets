using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Leap {
  public class LeapHandController : MonoBehaviour {

    public LeapProvider Provider {get; set; }
    public HandFactory Factory { get; set; }

    public Dictionary<int, HandRepresentation> reps;

    // Use this for initialization
    void Start() {
      Provider = GetComponent<LeapProvider>();
    }

    // Update is called once per frame
    void Update() {
      foreach (Leap.Hand curHand in Provider.CurrentFrame.Hands) {
        HandRepresentation rep;
        if (!reps.TryGetValue(curHand.Id, out rep)) {
          rep = Factory.MakeHandRepresentation(curHand);
          reps.Add(curHand.Id, rep);
        }

        if (rep == null)
          continue;

        rep.UpdateRepresentation(curHand);
        rep.LastUpdatedTime = (int)Provider.CurrentFrame.Timestamp;
      }

      // TODO:  Mark-and-sweep or set difference implementation
      //for (; ; ) {
        // TODO:  Initialize toBeDeleted with a value to be deleted
        
        //IHandRepresentation toBeDeleted;
        //reps.Remove(toBeDeleted.HandID);

        // Inform the representation that we will no longer be giving it any hand updates
        // because the corresponding hand has gone away
        
        //toBeDeleted.Finish();
      //}
    }
  }
}
