using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Leap {
  public class HandPool :
    HandFactory
  {
    public List<HandModel> ModelPool { get; set; }

    // Use this for initialization
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }

    public override HandRepresentation MakeHandRepresentation(Leap.Hand hand) {
      for (int i = 0; i < ModelPool.Count; i++)
        if (ModelPool[i].IsMirrored() && hand.IsRight) {
          var retVal = ModelPool[i];
          ModelPool.RemoveAt(i);
          return new HandProxy(this, retVal, hand);
        }

      return null;
    }
  }
}
