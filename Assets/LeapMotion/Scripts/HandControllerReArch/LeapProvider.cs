using UnityEngine;
using System.Collections;

namespace Leap {
  public class LeapProvider :
    MonoBehaviour
  {
    public Frame CurrentFrame { get; private set; }
    public Image CurrentImage { get; private set; }
    private Transform providerSpace;

    public LeapInternal.Connection connection { get; set; }

    // Use this for initialization
    void Start() {

    }

    // Update is called once per frame
    void Update() {
    }

    void FixedUpdate() {

    }
  }
}
