using UnityEngine;
using System.Collections;

public class EnableDepthBuffer : MonoBehaviour {

    void Awake() {
        if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth)) {
            Shader.EnableKeyword("USE_DEPTH_TEXTURE");
        } else {
            Shader.DisableKeyword("USE_DEPTH_TEXTURE");
        }
        GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;
    }
}
