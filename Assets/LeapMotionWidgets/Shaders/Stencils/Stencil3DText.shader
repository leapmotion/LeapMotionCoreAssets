Shader "Stencils/Material/3DText" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
    _Color ("Text Color", Color) = (1,1,1,1)
	}
	SubShader {
    Stencil {
      Ref 1
      Comp equal
      Pass keep
      Fail keep
    }
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
    Lighting Off Cull Off ZWrite Off Fog { Mode Off }
		Blend SrcAlpha OneMinusSrcAlpha
		Pass {
      Color [_Color]
      SetTexture [_MainTex] {
        combine primary, texture * primary
      }
    }
	}
}
