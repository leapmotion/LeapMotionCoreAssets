using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Leap {
  public class HandPool :
    HandFactory

  {
    public HandModel LeftHandModel;
    public HandModel RightHandModel;
    public List<HandModel> ModelPool;
    public LeapHandController controller_ { get; set; }

    // Use this for initialization
    void Start() {
      ModelPool = new List<HandModel>();
      ModelPool.Add(LeftHandModel);
      ModelPool.Add(RightHandModel);
      controller_ = GetComponent<LeapHandController>();
    }

    // Update is called once per frame
    void Update() {

    }

    public override HandRepresentation MakeHandRepresentation(Leap.Hand hand) {
      Debug.Log("Making a hand");
      //return new HandProxy(this, RightHandModel, hand);
      HandRepresentation handRep = null;
      for (int i = 0; i < ModelPool.Count; i++)
        if (ModelPool[i].Handedness == HandModel.Chirality.Right && hand.IsRight) {
          HandModel retVal = ModelPool[i];
          ModelPool[i].SetController(controller_);
          ModelPool.RemoveAt(i);
          handRep = new HandProxy(this, retVal, hand);
          return handRep;
      }
      else if (ModelPool[i].Handedness == HandModel.Chirality.Left && hand.IsLeft) {
          HandModel retVal = ModelPool[i];
          ModelPool[i].SetController(controller_);
          ModelPool.RemoveAt(i);
          handRep = new HandProxy(this, retVal, hand);
          return handRep;
        }
      return handRep;
    }
  }
}
