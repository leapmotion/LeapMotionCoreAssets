Shader "Custom/BloomGainTransparency" {
	Properties {
    _Color ("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
    _Gain ("Gain", Range (1.0, 10.0)) = 1.0
	}
	SubShader {
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert alpha

    float4 _Color;
    float _Gain;

		struct Input {
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			o.Albedo = _Color.rgb * _Gain;
			o.Alpha = _Color.a;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
