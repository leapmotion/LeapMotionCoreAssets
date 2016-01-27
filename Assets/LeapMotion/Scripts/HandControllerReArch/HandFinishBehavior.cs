using UnityEngine;
using System.Collections;

namespace Leap {
  [RequireComponent(typeof(HandModel))]
  public abstract class HandFinishBehavior : MonoBehaviour {

    public abstract void Resest();
    public abstract void HandFinish();
  } 
}
