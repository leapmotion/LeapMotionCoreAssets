Shader "LeapMotion/Passthrough/HandHighlight" {
	Properties {
		_Color           ("Color", Color)                  = (0.165,0.337,0.578,1.0)
		_Fade            ("Fade", Range(0, 1))             = 0
		_Extrude         ("Extrude", Float)                = 0.008
		_Intersection    ("Intersection Threshold", Float) = 0.035

		_MinThreshold    ("Min Threshold", Float)     = 0.1
		_MaxThreshold    ("Max Threshold", Float)     = 0.2
		_GlowThreshold   ("Glow Threshold", Float)    = 0.5
		_GlowPower       ("Glow Power", Float)        = 10
	}


	CGINCLUDE
	#pragma multi_compile LEAP_FORMAT_IR LEAP_FORMAT_RGB
	#include "LeapCG.cginc"
	#include "UnityCG.cginc"

	#pragma target 3.0

	uniform float4    _Color;
    uniform float     _Fade;
	uniform float     _Extrude;
	uniform float     _Intersection;
	uniform float     _MinThreshold;
	uniform float     _MaxThreshold;
	uniform float     _GlowThreshold;
	uniform float     _GlowPower;

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

	float4 getHandColor(float4 screenPos){
		float4 rawColor = LeapRawColorBrightness(screenPos);
		float3 color = pow(rawColor.rgb, _LeapGammaCorrectionExponent);
		float brightness = smoothstep(_MinThreshold, _MaxThreshold, rawColor.a);
		float glow = smoothstep(_GlowThreshold, _MinThreshold, rawColor.a) * brightness;
		return float4(color + _Color * glow * _GlowPower, brightness);
	}

	float4 frag(frag_in i) : COLOR {
		float4 handColor = getHandColor(i.screenPos);
		return float4(handColor.rgb, _Fade * handColor.a);
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