using UnityEngine;
using System.Collections;

namespace Leap {
  [RequireComponent(typeof(HandModel))]
  public abstract class HandTransitionBehavior : MonoBehaviour {

    public abstract void Reset();
    public abstract void HandFinish();
  } 
}
