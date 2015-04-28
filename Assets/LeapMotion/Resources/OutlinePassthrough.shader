Shader "LeapMotion/Passthrough/OutlinePassthrough" {
	Properties {
		_Color           ("Color", Color)                  = (0.165,0.337,0.578,1.0)
		_Fade            ("Fade", Range(0, 1))             = 0
		_Extrude         ("Extrude", Float)                = 0.008

		_Threshold       ("Threshold", Float)     = 0.1
		_Edge            ("Edge width", Range(0, 0.01))  = 0.002
	}


	CGINCLUDE
	#pragma multi_compile LEAP_FORMAT_IR LEAP_FORMAT_RGB
	#include "LeapCG.cginc"
	#include "UnityCG.cginc"

	#pragma target 3.0

	uniform sampler2D _CameraDepthTexture;

	uniform float4    _Color;
	uniform float     _Extrude;
	uniform float     _Threshold;
	uniform float     _Edge;

	struct appdata {
		float4 vertex : POSITION;
		float3 normal : NORMAL;
	};

	struct frag_in{
		float4 vertex : POSITION;
		float4 screenPos  : TEXCOORD0;
	};

	frag_in vert(appdata v){
		frag_in o;
		o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);

		float3 norm   = mul ((float3x3)UNITY_MATRIX_IT_MV, v.normal);
		float2 offset = TransformViewToProjection(norm.xy);
		o.vertex.xy += offset * _Extrude;

		o.screenPos = ComputeScreenPos(o.vertex);
		return o;
	}

	float4 frag(frag_in i) : COLOR{
		float2 uv = LeapGetUndistortedUV(i.screenPos);

		float4 colorCenter = LeapRawColorBrightnessUV(uv);
		float colorRight = LeapRawBrightnessUV(uv + float2(_Edge, 0));
		float colorLeft = LeapRawBrightnessUV(uv + float2(-_Edge, 0));
		float colorUp = LeapRawBrightnessUV(uv + float2(0, _Edge));
		float colorDown = LeapRawBrightnessUV(uv + float2(0, -_Edge));

		float alpha = step(_Threshold, colorCenter.a);

		float edge = alpha;
		edge *= step(_Threshold, colorRight);
		edge *= step(_Threshold, colorLeft);
		edge *= step(_Threshold, colorUp);
		edge *= step(_Threshold, colorDown);

		float3 finalColor = lerp(_Color,
		                  pow(colorCenter.rgb, _LeapGammaCorrectionExponent),
						  edge);

		return float4(finalColor, alpha);
	}

	float4 alphaFrag(frag_in i) : COLOR {
		return float4(0,0,0,0);
	}

	ENDCG

	SubShader {
		Tags {"Queue"="Overlay"}

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
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
		
	} 
	Fallback "Unlit/Texture"
}
