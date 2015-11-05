Shader "LeapMotion/Passthrough/ImageHandHighlight" {
  Properties {
    _Color           ("Color", Color)                  = (0.165,0.337,0.578,1.0)
    _Fade            ("Fade", Range(0, 1))             = 0.0
    _Extrude         ("Extrude", Float)                = 0.008
    _Intersection    ("Intersection Threshold", Float) = 0.035
    _IntersectPow    ("Intersection Brightness", Range (0, 2000)) = 100

    _MinThreshold    ("Min Threshold", Float)     = 0.1
    _MaxThreshold    ("Max Threshold", Float)     = 0.2
    _GlowThreshold   ("Glow Threshold", Float)    = 0.5
    _GlowPower       ("Glow Power", Float)        = 10.0
  }

  CGINCLUDE
  #pragma multi_compile LEAP_FORMAT_IR LEAP_FORMAT_RGB
  #pragma multi_compile _ USE_DEPTH_TEXTURE
  #include "LeapCG.cginc"
  #include "UnityCG.cginc"

  #pragma target 3.0

  uniform float4    _Color;
  uniform float     _Fade;
  uniform float     _Extrude;
  uniform float     _Intersection;
  uniform float     _IntersectPow;
  uniform float     _MinThreshold;
  uniform float     _MaxThreshold;
  uniform float     _GlowThreshold;
  uniform float     _GlowPower;
  uniform float     _LeapGlobalColorSpaceGamma;
  
  #ifdef USE_DEPTH_TEXTURE
  uniform sampler2D _CameraDepthTexture;
  #endif

  struct appdata {
    float4 vertex : POSITION;
    float3 normal : NORMAL;
  };

  struct frag_in {
    float4 vertex : POSITION;
    float4 screenPos  : TEXCOORD0;
#ifdef USE_DEPTH_TEXTURE
    float4 projPos  : TEXCOORD1;
#endif
  };

  frag_in vert(appdata v) {
    frag_in o;
    o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);

    float3 norm   = mul ((float3x3)UNITY_MATRIX_IT_MV, v.normal);
    o.vertex.xy += TransformViewToProjection(norm.xy) * _Extrude;

    o.screenPos = LeapGetWarpedScreenPos(o.vertex);

#ifdef USE_DEPTH_TEXTURE
    o.projPos = o.screenPos;
    COMPUTE_EYEDEPTH(o.projPos.z);
#endif

    return o;
  }

  float4 trackingGlow(float4 screenPos) {
    float leapBrightness = LeapGetStereoBrightness(screenPos);
    clip(leapBrightness - _MinThreshold);

    float3 leapRawColor = LeapGetStereoRawColor(screenPos);

    // Map leap image to linear color space
    float3 leapLinearColor = pow(pow(leapRawColor, _LeapGlobalGammaCorrectionExponent), 1/_LeapGlobalColorSpaceGamma);

    // Apply edge glow and interior shading
    float brightness = smoothstep(_MinThreshold, _MaxThreshold, leapBrightness) * _Fade;
    float glow = smoothstep(_GlowThreshold, _MinThreshold, leapBrightness) * brightness;

    float4 linearColor = pow(_Color, _LeapGlobalColorSpaceGamma) * glow * _GlowPower;

    return float4(leapLinearColor + linearColor, brightness);
  }
  
  #ifdef USE_DEPTH_TEXTURE
  float4 intersectionGlow(float4 handGlow, float4 projPos) {
    // Apply intersection highlight
    float sceneZ = LinearEyeDepth (SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(projPos)));
    float partZ = projPos.z;
    float diff = smoothstep(_Intersection, 0, sceneZ - partZ);
    float4 linearColor = pow(_Color, _LeapGlobalColorSpaceGamma) * _IntersectPow;
    return float4(lerp(handGlow.rgb, linearColor.rgb, diff), handGlow.a * (1 - diff));
  }
  #endif

  float4 frag(frag_in i) : COLOR {
    float4 handGlow = trackingGlow(i.screenPos);
    
#ifdef USE_DEPTH_TEXTURE
    handGlow = intersectionGlow(handGlow, i.projPos);
#endif

    return float4(handGlow.rgb, _Fade * handGlow.a);
  }

  float4 alphaFrag(frag_in i) : COLOR {
    float leapBrightness = LeapGetStereoBrightness(i.screenPos);
    clip(leapBrightness - _MinThreshold);
    return float4(0,0,0,0);
  }

  ENDCG

  SubShader {
    Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="Transparent"}
    
    Blend SrcAlpha OneMinusSrcAlpha

    Pass{
      ZWrite On
      ColorMask 0

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment alphaFrag
      ENDCG
        }

    Pass{
      ZWrite Off
    
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      ENDCG
    }
    
  } 
  Fallback "Unlit/Texture"
}
