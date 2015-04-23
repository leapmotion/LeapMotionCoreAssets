using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LeapImageBasedMaterial : MonoBehaviour {
    public ImageMode imageMode = ImageMode.STEREO;

    public enum ImageMode {
        STEREO,
        LEFT_ONLY,
        RIGHT_ONLY
    }

    void Awake() {
        if (FindObjectOfType<LeapImageRetriever>() == null) {
            Debug.LogWarning("Place a LeapImageRetriever script on a camera to enable Leap image-based materials");
            enabled = false;
        }
    }

    void OnEnable() {
        LeapImageRetriever.registerImageBasedMaterial(this);
    }

    void OnDisable() {
        LeapImageRetriever.unregisterImageBasedMaterial(this);
    }
}
