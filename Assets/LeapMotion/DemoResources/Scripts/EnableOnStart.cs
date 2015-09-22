using UnityEngine;
using System.Collections;

public class EnableOnStart : MonoBehaviour {

  public MonoBehaviour _target;

	// Use this for initialization
	void Start () {
    if (_target != null) {
      _target.enabled = true;
    }
	}
}
