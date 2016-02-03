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

    public override HandRepresentation MakeHandRepresentation(Leap.Hand hand, ModelType modelType) {
      Debug.Log("Making a " + modelType + " hand");

      HandRepresentation handRep = null;
      for (int i = 0; i < ModelPool.Count; i++) {
        HandModel model = ModelPool[i];

        bool isCorrectHandedness;
        if(model.Handedness == Chirality.Either) {
          isCorrectHandedness = true;
        } else {
          Chirality handChirality = hand.IsRight ? Chirality.Right : Chirality.Left;
          isCorrectHandedness = model.Handedness == handChirality;
        }

        bool isCorrectModelType;
        isCorrectModelType = model.HandModelType == modelType;

        if(isCorrectHandedness && isCorrectModelType) {
          Debug.Log("Found a " + modelType + " HandModel");
          model.SetController(controller_);
          ModelPool.RemoveAt(i);
          handRep = new HandProxy(this, model, hand);
          break;
        }
      }

      Debug.Log("HandPool.MakeHandRepresentation() returning: " + handRep);
      return handRep;
    }
  }
}
