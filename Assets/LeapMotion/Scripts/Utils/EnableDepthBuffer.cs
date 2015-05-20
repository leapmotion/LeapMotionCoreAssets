using UnityEngine;
using System.Collections;

public class EnableDepthBuffer : MonoBehaviour {

    void Awake() {
        if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth)) {
            Shader.EnableKeyword("USE_DEPTH_EFFECTS");
        } else {
            Shader.DisableKeyword("USE_DEPTH_EFFECTS");
        }
        GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;
    }
}
