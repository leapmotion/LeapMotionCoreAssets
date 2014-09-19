Shader "Skin for Unity 3/Bumped Specular" {
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
	_Shininess ("Shininess", Range (0.01, 1)) = 0.078125
	_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
	_BumpMap ("Bump (RGB)", 2D) = "bump" {}
	_RimTex ("Rim ramp (RGB) Fresnel ramp (A)", 2D) = " grey" {}
	_WrapTex ("Wrap ramp (RGBA)", 2D) = "black" {}
}
 
SubShader {
	Tags { "RenderType" = "Opaque" }
    LOD 400
    
CGPROGRAM
#pragma surface surf BumpSpecSkin

uniform float4 _Color;
uniform float _Shininess;
uniform sampler2D _MainTex;
uniform sampler2D _WrapTex;
uniform sampler2D _RimTex;
uniform sampler2D _BumpMap;

half4 LightingBumpSpecSkin (SurfaceOutput s, half3 lightDir, half3 viewDir, half atten) {
	// rim factor
	float rimf = dot(s.Normal, viewDir);
	half4 rim = tex2D (_RimTex, rimf.xx);
	
	half3 h = normalize( lightDir + viewDir );
	
	// This is "wrap diffuse" lighting.
	// What we do here is to calculate the color, then look up the wrap coloring ramp so you can tint things properly
	half diffusePos = dot(s.Normal, lightDir) * 0.5 + 0.5;
	half4 diffuse = tex2D (_WrapTex, diffusePos.xx);
	diffuse.rgb *= rim.rgb * 4;
	
	float nh = saturate( dot( h, s.Normal ) );
	float spec = pow( nh, s.Specular ) * s.Gloss;

	half4 c;
	c.rgb = (s.Albedo + _SpecColor.rgb * spec * rim.a) * diffuse * (atten * 2) * _LightColor0.rgb;
	// specular passes by default put highlights to overbright
	c.a =  _LightColor0.a * _SpecColor.a * spec * atten;
	
	return c * _Color;
}

struct Input {
	float2 uv_MainTex;
	float2 uv_BumpMap;
};

void surf (Input IN, inout SurfaceOutput o) {
	half4 texcol = tex2D( _MainTex, IN.uv_MainTex);	
	o.Albedo = texcol.rgb;
	o.Gloss = texcol.a;
	o.Specular = _Shininess * 128;
	o.Normal = UnpackNormal (tex2D (_BumpMap, IN.uv_BumpMap));
}

ENDCG

}
}