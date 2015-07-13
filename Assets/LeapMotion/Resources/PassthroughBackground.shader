Shader "LeapMotion/Passthrough/Background" {
  Properties {
    _ColorSpaceGamma ("Color Space Gamma", Float) = 1.0
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
    
    //Global Coordinate Transformation of viewer from Now to ImageTimestamp
    //If warping will not be used this must be equal to the identity.
    float4x4 _ViewerImageFromNow;

    struct frag_in{
      float4 position : SV_POSITION;
      float4 screenPos  : TEXCOORD1;
    };

    frag_in vert(appdata_img v){
      frag_in o;
      
      //(1) Apply the model transform
      //(2) Apply the global transform
      //NOTE: Rotating viewer from Image to Now is equivalent to inverse rotation applied to work
      //(3) Apply the view and project matrices to derive the warped screen position
      o.position = mul(UNITY_MATRIX_P, mul(_ViewerImageFromNow, mul(UNITY_MATRIX_MV, v.vertex)));
      //o.position = mul(UNITY_MATRIX_P, mul(UNITY_MATRIX_MV, v.vertex));
      //o.position = mul(UNITY_MATRIX_MVP, v.vertex);
      
      o.screenPos = ComputeScreenPos(o.position);
      return o;
    }

    float4 frag (frag_in i) : COLOR {
      return float4(pow(LeapColor(i.screenPos), 1/_ColorSpaceGamma), 1);
    }

    ENDCG
    }
  } 
  Fallback off
}
