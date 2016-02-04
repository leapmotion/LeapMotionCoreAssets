using UnityEngine;
using System.Collections;
using Leap;


public enum Chirality { Left, Right, Either };
public enum ModelType { Graphics, Physics };

public abstract class IHandModel : MonoBehaviour {
  public abstract Chirality Handedness { get; }
  public abstract ModelType HandModelType { get; }
  public abstract void InitHand();
  public abstract void UpdateHand();
  public abstract Hand GetLeapHand(); 
  public abstract void SetLeapHand(Hand hand);
  public abstract bool IsMirrored();
}


