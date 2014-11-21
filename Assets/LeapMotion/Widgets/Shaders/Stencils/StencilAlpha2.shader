Shader "Stencils/Material/Transparent2" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
    _MainTex ("Base (RGB)", 2D) = "white" {}
	}
	SubShader {
    Stencil {
      Ref 2
      Comp equal
      Pass keep
      Fail keep
    }
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		LOD 200
		
		CGPROGRAM
		  #pragma surface surf Lambert alpha

		  sampler2D _MainTex;
      fixed4 _Color;

		  struct Input {
			  float2 uv_MainTex;
		  };

		  void surf (Input IN, inout SurfaceOutput o) {
			  half4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			  o.Albedo = c.rgb;
			  o.Alpha = c.a;
		  }
		ENDCG
	} 
	FallBack "Transparent/VertexLit"
}
