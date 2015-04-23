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

    void OnEnable() {
        LeapImageRetriever.registerImageBasedMaterial(this);
    }

    void OnDisable() {
        LeapImageRetriever.unregisterImageBasedMaterial(this);
    }
}
