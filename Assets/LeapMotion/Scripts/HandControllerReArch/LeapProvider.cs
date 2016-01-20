using UnityEngine;
using System.Collections;
using LeapInternal;
using Leap;

namespace Leap {
  public class LeapProvider :
    MonoBehaviour
  {
    public Frame CurrentFrame { get; private set; }
    public Image CurrentImage { get; private set; }
    private Transform providerSpace;

    public Connection connection { get; set; }

    void Awake() {
      connection = Connection.GetConnection();

    }

    // Use this for initialization
    void Start() {

      //set empty frame
      CurrentFrame = new Frame();
    }

    // Update is called once per frame
    void Update() {
      CurrentFrame = connection.Frames.Get();
      Debug.Log(CurrentFrame);

    }

    void FixedUpdate() {

    }
    void OnDestroy() {
      connection.Stop();
    }
  }
}
