using UnityEngine;
using System.Collections;

public class WooCube : MonoBehaviour {

  void Update() {
    transform.position = new Vector3(0, 10, 0);
  }

  void OnWillRenderObject() {
    transform.position = new Vector3(0.0f, 0.0f, 0.3f);
  }
}
