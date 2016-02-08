using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Leap {
  public class HandPool :
    HandFactory

  {
    public IHandModel LeftGraphicsModel;
    public IHandModel RightGraphicsModel;
    public IHandModel LeftPhysicsModel;
    public IHandModel RightPhysicsModel;
    public List<IHandModel> ModelPool;
    public LeapHandController controller_ { get; set; }

    // Use this for initialization
    void Start() {
      ModelPool = new List<IHandModel>();
      ModelPool.Add(LeftGraphicsModel);
      ModelPool.Add(RightGraphicsModel);
      ModelPool.Add(LeftPhysicsModel);
      ModelPool.Add(RightPhysicsModel);
      controller_ = GetComponent<LeapHandController>();
    }

    public override HandRepresentation MakeHandRepresentation(Leap.Hand hand, ModelType modelType) {
      HandRepresentation handRep = null;
      for (int i = 0; i < ModelPool.Count; i++) {
        IHandModel model = ModelPool[i];

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
          ModelPool.RemoveAt(i);
          handRep = new HandProxy(this, model, hand);
          break;
        }
      }
      return handRep;
    }
  }
}
