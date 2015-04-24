Shader "LeapMotion/Passthrough/SmoothDragonfly" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_Min ("Min Brightness", Range(0, 1)) = 0.1
		_Max ("Max Brightness", Range(0, 1)) = 0.3
		_Extrude ("Extrude", Range(0, 0.01)) = 0.01
	}

	SubShader {
		Tags {"Queue"="Overlay+10" "IgnoreProjector"="True" "RenderType"="Transparent"}

		Lighting Off
		Cull Off
		Zwrite Off

		Blend SrcAlpha OneMinusSrcAlpha

		Pass{
		CGPROGRAM
		#pragma multi_compile LEAP_FORMAT_IR LEAP_FORMAT_RGB
		#include "LeapCG.cginc"
		#include "UnityCG.cginc"
		
		#pragma vertex vert
		#pragma fragment frag

		struct appdata {
			float4 vertex : POSITION;
			float3 normal : NORMAL;
		};

		struct frag_in{
			float4 position : SV_POSITION;
			float4 screenPos  : TEXCOORD1;
		};

		float _Extrude;

		frag_in vert(appdata v){
			frag_in o;
			o.position = mul(UNITY_MATRIX_MVP, v.vertex);
			float3 norm   = mul ((float3x3)UNITY_MATRIX_IT_MV, v.normal);
			float2 offset = TransformViewToProjection(norm.xy);
			o.position.xy += offset * _Extrude;

			o.screenPos = ComputeScreenPos(o.position);
			return o;
		}

		float4 _Color;
		float _Min;
		float _Max;

		float4 frag (frag_in i) : COLOR {
			float4 colorBrightness = LeapRawColorBrightness(i.screenPos);
			float alpha = smoothstep(_Min, _Max, colorBrightness.a);
			return float4(pow(colorBrightness.rgb, _LeapGammaCorrectionExponent), alpha);
			//return float4(alpha, alpha, alpha, alpha);
			//return float4(0,0,0,0);
		}

		ENDCG
		}
	} 
	Fallback off
}
