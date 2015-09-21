using UnityEngine;
using System.Collections;

public class WooCube : MonoBehaviour {

  public Transform wooCube;

  void OnPreRender() {
    wooCube.transform.position = new Vector3(0.0f, 0.0f, 0.3f);
  }

  void OnPostRender() {
    wooCube.transform.position = new Vector3(0.0f, 10.0f, 0.0f);
  }
}
