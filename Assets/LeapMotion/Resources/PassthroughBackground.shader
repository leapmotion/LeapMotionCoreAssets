Shader "LeapMotion/Passthrough/Background" {
  Properties {
    _ColorSpaceGamma ("Color Space Gamma", Float) = 1.0
    _hView ("Horizontal View Degrees", Float) = 60.0
    _vView ("Vertical View Degrees", Float) = 60.0
    _useTimeWarp ("Use Time Warp", Int) = 0
    _dbgTest("Original, Reprojected, R-O, O-R, Black", Int) = 0
  }

  SubShader {
    Tags {"Queue"="Background" "IgnoreProjector"="True"}

    Cull Off
    Zwrite Off
    Blend One Zero

    Pass{
    CGPROGRAM
    #pragma multi_compile LEAP_FORMAT_IR LEAP_FORMAT_RGB
    #include "LeapCG.cginc"
    #include "UnityCG.cginc"
    
    #pragma vertex vert
    #pragma fragment frag
    
    uniform float _ColorSpaceGamma;
    float _hView;
    float _vView;
    int _useTimeWarp;
    
    int _dbgTest;
    
    //Missing Camera Matrix
    float4x4 _InverseView;
    
    //Global Coordinate Transformation of viewer from Now to ImageTimestamp
    //If warping will not be used this must be equal to the identity.
    float4x4 _ViewerImageFromNow;

    struct frag_in{
      float4 position : SV_POSITION;
      float4 screenPos  : TEXCOORD1;
    };

    frag_in vert(appdata_img v){
      frag_in o;
      o.position = mul(UNITY_MATRIX_MVP, v.vertex);
      o.screenPos = ComputeScreenPos(o.position);
      
      return o;
    }

    float4 frag (frag_in i) : COLOR {
      //TODO: I need to perform the transformation here, when the screen space is defined
      //PROBLEM: I need to know the transformation from screen
      //NEED: Camera.fieldOfView = vertical field of view in degrees
      //NEED: Camera.aspect * Camera.fieldOfView = horizontal field of view in degrees
      //NEXT: Try constructing 3 vectors, applying rotation, then reprojecting to screen coordinates.
      if (_useTimeWarp < 1) {
        return float4(pow(LeapColor(i.screenPos), 1/_ColorSpaceGamma), 1);
      }
      
      // Map pixels to window coordinates
      float2 window = 1.0 - 2.0*i.screenPos.xy/i.screenPos.w;
      //range (-1,1), x is horizontal, y is vertical, origin is center
      
      // Map window coordinates to world coordinates
      float4 viewDir = float4(tan(radians(_hView) / 2.0)*window.x, tan(radians(_vView) / 2.0)*window.y, 1.0, 0.0);
      //float4 worldDir = mul(_InverseView, viewDir);
      
      // Apply time warping
      //worldDir = mul(_ViewerImageFromNow, worldDir);
      
      // Return to pixel coordinates
      //float4 proj = mul(UNITY_MATRIX_VP, worldDir);
      float4 proj = mul(UNITY_MATRIX_P, viewDir);
      float4 sp = ComputeScreenPos(proj);
      
      //return float4(pow(LeapColor(sp), 1/_ColorSpaceGamma), 1);
      if (_dbgTest < 1) {
        return fixed4(i.screenPos.xy / i.screenPos.w, 0.0, 1.0);
      }
      if (_dbgTest < 2) {
        return fixed4(sp.xy / sp.w, 0.0, 1.0);
      }
      if (_dbgTest < 3) {
        return fixed4((sp.xy / sp.w) - (i.screenPos.xy / i.screenPos.xy), 0.0, 1.0);
      }
      if (_dbgTest < 4) {
        return fixed4((i.screenPos.xy / i.screenPos.xy) - (sp.xy / sp.w), 0.0, 1.0);
      }
      return float4(pow(LeapColor(sp), 1/_ColorSpaceGamma), 1);
      
//      float2 wcoord = i.screenPos.xy/i.screenPos.w;
//      fixed4 color;
//      if (fmod(20.0*wcoord.x,2.0)<1.0) {
//        color = fixed4(wcoord.xy,0.0,1.0);
//      } else {
//        color = fixed4(0.3,0.3,0.3,1.0);
//      }
//      return color;
    }

    ENDCG
    }
  } 
  Fallback off
}
