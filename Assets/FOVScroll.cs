using UnityEngine;
using System.Collections;

public class FOVScroll : MonoBehaviour {
    private Camera _camera;

    void Start() {
        _camera = GetComponent<Camera>();
    }

    void Update() {
        _camera.fieldOfView += Input.mouseScrollDelta.y;
    }
}
