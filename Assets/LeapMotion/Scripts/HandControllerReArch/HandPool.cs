using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Leap {
  public class HandPool :
    HandFactory

  {
    public HandModel LeftGraphicsModel;
    public HandModel RightGraphicsModel;
    public HandModel LeftPhysicsModel;
    public HandModel RightPhysicsModel;
    public List<HandModel> ModelPool;
    public LeapHandController controller_ { get; set; }

    // Use this for initialization
    void Start() {
      ModelPool = new List<HandModel>();
      ModelPool.Add(LeftGraphicsModel);
      ModelPool.Add(RightGraphicsModel);
      ModelPool.Add(LeftPhysicsModel);
      ModelPool.Add(RightPhysicsModel);
      controller_ = GetComponent<LeapHandController>();
    }

    // Update is called once per frame
    void Update() {

    }

    public override HandRepresentation MakeHandRepresentation(Leap.Hand hand, HandModel.ModelType modelType) {
      Debug.Log("Making a " + modelType + " hand");
      //return new HandProxy(this, RightHandModel, hand);
      HandRepresentation handRep = null;
      for (int i = 0; i < ModelPool.Count; i++)
        if (ModelPool[i].Handedness == HandModel.Chirality.Right && hand.IsRight && ModelPool[i].HandModelType == modelType) {
          Debug.Log("Found a " + modelType + " HandModel");
          HandModel retVal = ModelPool[i];
          ModelPool[i].SetController(controller_);
          ModelPool.RemoveAt(i);
          //ModelPool[i].SetLeapHand(hand);
          //ModelPool[i].InitHand();
          handRep = new HandProxy(this, retVal, hand);
          return handRep;
        }
        else if (ModelPool[i].Handedness == HandModel.Chirality.Left && hand.IsLeft && ModelPool[i].HandModelType == modelType) {
          Debug.Log("Found a " + modelType + " HandModel");
          HandModel retVal = ModelPool[i];
          ModelPool[i].SetController(controller_);
          ModelPool.RemoveAt(i);
          //ModelPool[i].SetLeapHand(hand);
          //ModelPool[i].InitHand();
          handRep = new HandProxy(this, retVal, hand);
          return handRep;
        }
      return handRep;
    }
  }
}
