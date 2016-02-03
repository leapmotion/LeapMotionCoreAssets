using UnityEngine;
using System.Collections;
using Leap;

public enum Chirality { Left, Right, Either };
public enum ModelType { Graphics, Physics };

public interface IHandModel {
  Chirality Handedness { get; }
  ModelType HandModelType { get; }
  void InitHand();
  void UpdateHand();
  void SetLeapHand(Hand hand);
}


