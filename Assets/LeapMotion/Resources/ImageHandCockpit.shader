Shader "LeapMotion/Passthrough/ImageHandCockpit" {
	Properties {
    _Color           ("Tint color", Color)        = (1.0,1.0,1.0,1.0)
    
    // Lateral expansion to capture passthrough
    _Extrude         ("Extrusion Amount", Range(0.0,0.05)) = 0.03
    
    // Input from Leap passthrough
    _MainTex         ("Base (A=Opacity)", 2D)     = "" {}
    
    // Input for Leap image passthrough
    _Distortion      ("Distortion", 2D)           = "" {}
    _Projection      ("Projection", Vector)       = (0.0,0.0,0.0,0.0)
    _Cutoff          ("Cutoff", Float)     = 0.15

    // Grayscale image & glow
    _AlphaScale      ("Alpha Scale", Float)        = 0.25
    _Tint            ("Tint Amount", Range(0.0, 1.0)) = 0.0
    _GlowCutoff      ("Glow Cutoff", Range(0.0, 1.0)) = 0.3
    _GlowScale      ("Glow Scale", Range(0.0, 1.0)) = 0.0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
    #include "UnityCG.cginc"
    #include "LeapCG.cginc"
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows keepalpha vertex:extrude finalcolor:passglow

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

    float4    _Color;
    
    float     _Extrude;
    
    sampler2D _MainTex;
    sampler2D _Distortion;
    float4    _Projection;
    float     _Cutoff;

    float     _AlphaScale;
    float     _Tint;
    float     _GlowCutoff;
    float     _GlowScale;

		struct Input {
			float2 uv_MainTex;
      float4 screenPos;
		};
    
    // Expand only in directions perpendicular to view
    void extrude (inout appdata_full v) {
      float3 worldNormal = mul((float3x3)_Object2World, v.normal);
      float4 worldVertex = mul(_Object2World, v.vertex);
      float3 worldToCamera = _WorldSpaceCameraPos - worldVertex.xyz;
      float projectDenom = dot(worldToCamera, worldToCamera);
      if (projectDenom < 0.001) {
        projectDenom = 0.001;
      }
      float3 worldExtrude = worldNormal - worldToCamera * dot(worldToCamera, worldNormal) / projectDenom;
      worldVertex.xyz += worldExtrude * _Extrude;
      v.vertex.xyz = mul(_World2Object, worldVertex);
    }

    // Define color from screen space + glow
		void surf (Input IN, inout SurfaceOutputStandard o) {
      float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
      screenUV -= float2(0.5, 0.5);
      screenUV *= 2.0;
      float gray = LeapColorBrightnessUV(IN.screenPos).a; //tex2D(_MainTex, LeapGetUndistortedTextureUV(_Distortion, screenUV, _Projection)).a;
      //clip (gray - _Cutoff);
		}
    
    void passglow (Input IN, SurfaceOutputStandard o, inout fixed4 final) {
      float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
      screenUV -= float2(0.5, 0.5);
      screenUV *= 2.0;
      float gray = LeapColorBrightnessUV(IN.screenPos).a; //tex2D(_MainTex, LeapGetUndistortedTextureUV(_Distortion, screenUV, _Projection)).a;
      float3 glow = _Color.rgb * (smoothstep(_Cutoff, _GlowCutoff, gray) * smoothstep(_GlowCutoff, _Cutoff, gray) * 2 * _GlowScale);
      float3 passthrough = float3(gray, gray, gray) * _AlphaScale * lerp(float3(1.0,1.0,1.0), _Color.rgb, _Tint) + glow;
      final = float4(passthrough, 1.0);
    }
    
		ENDCG
	} 
	FallBack "Diffuse"
}
