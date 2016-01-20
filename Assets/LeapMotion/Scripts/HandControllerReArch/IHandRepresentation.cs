using UnityEngine;
using System;
using System.Collections;

namespace Leap {
  public abstract class IHandRepresentation
  {
    public int HandID { get; private set; }
    public int LastUpdatedTime { get; set; }

    public IHandRepresentation(int handID) {
      HandID = handID;
    }

    /// <summary>
    /// Notifies the representation that a hand information update is available
    /// </summary>
    /// <param name="hand">The current Leap.Hand</param>
    public abstract void UpdateRepresentation(Leap.Hand hand);

    /// <summary>
    /// Called when a hand representation is no longer needed
    /// </summary>
    public abstract void Finish();
  }
}
