using UnityEngine;
using System.Collections;

namespace Leap {
  public class HandProxy:
    HandRepresentation
  {
    HandPool parent;
    HandModel handModel;

    public HandProxy(HandPool parent, HandModel handModel, Leap.Hand hand) :
      base(hand.Id)
    {
      this.parent = parent;
      this.handModel = handModel;
    }

    public override void Finish() {
      parent.ModelPool.Add(handModel);
      handModel = null;
    }

    public override void UpdateRepresentation(Leap.Hand hand) {
      // TODO:  Decide how to pass information about the updated hand to the hand model
      handModel.SetLeapHand(hand);
      handModel.UpdateHand();
    }
  }
}
