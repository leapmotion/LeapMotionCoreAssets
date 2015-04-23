Shader "LeapMotion/Passthrough/Background" {
	Properties {
	}

	SubShader {
		Tags {"Queue"="Background" "IgnoreProjector"="True" "RenderType"="Transparent"}

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

		struct frag_in{
			float4 position : SV_POSITION;
			float4 fragPos  : TEXCOORD1;
		};

		frag_in vert(appdata_img v){
			frag_in o;
			o.position = mul(UNITY_MATRIX_MVP, v.vertex);
			o.fragPos = o.position;
			return o;
		}

		float4 frag (frag_in i) : COLOR {
			return float4(LeapColor(i.fragPos), 1);
		}

		ENDCG
		}
	} 
	Fallback off
}
