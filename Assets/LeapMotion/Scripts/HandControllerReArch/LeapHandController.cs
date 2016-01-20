using UnityEngine;
using System.Collections;

namespace Leap {
  public class LeapHandController : MonoBehaviour {

    public LeapProvider Provider { get; set; }
    public IHandFactory Factory { get; set; }

    public System.Collections.Generic.Dictionary<int, IHandRepresentation> reps;

    // Use this for initialization
    void Start() {

    }

    // Update is called once per frame
    void Update() {
      foreach(Leap.Hand curHand in Provider.CurrentFrame.Hands) {
        IHandRepresentation rep;
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
      for (; ; ) {
        // TODO:  Initialize toBeDeleted with a value to be deleted
        
        //IHandRepresentation toBeDeleted;
        //reps.Remove(toBeDeleted.HandID);

        // Inform the representation that we will no longer be giving it any hand updates
        // because the corresponding hand has gone away
        
        //toBeDeleted.Finish();
      }
    }
  }
}
