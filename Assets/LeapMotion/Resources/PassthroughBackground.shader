Shader "LeapMotion/Passthrough/Background" {
  Properties {
    _ColorSpaceGamma ("Color Space Gamma", Float) = 1.0
    _hView ("Horizontal View Degrees", Float) = 60.0
    _vView ("Vertical View Degrees", Float) = 60.0
    _useTimeWarp ("Use Time Warp", Int) = 0
    //_dbgTest("Original, Reprojected, R-O, O-R, Black", Int) = 0
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
    
    #pragma target 3.0
    
    #pragma vertex vert
    #pragma fragment frag
    
    uniform float _ColorSpaceGamma;

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
      return float4(pow(LeapColorWarp(i.screenPos), 1/_ColorSpaceGamma), 1);
    }

    ENDCG
    }
  } 
  Fallback off
}
