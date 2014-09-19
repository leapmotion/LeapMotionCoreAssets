// Upgrade NOTE: replaced 'PositionFog()' with multiply of UNITY_MATRIX_MVP by position
// Upgrade NOTE: replaced 'V2F_POS_FOG' with 'float4 pos : SV_POSITION'

Shader "Skin/Bumped Diffuse" {
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
	_Shininess ("Shininess", Range (0.01, 1)) = 0.078125
	_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
	_BumpMap ("Bump (RGB)", 2D) = "white" {}
	_RimTex ("Rim ramp (GRB) Fresnel ramp (A)", 2D) = " grey" {}
	_WrapTex ("Wrap ramp (RGBA)", 2D) = "grey" {}
}
 
// ------------------------------------------------------------------
// Fragment program
 
#warning Upgrade NOTE: SubShader commented out; uses Unity 2.x per-pixel lighting. You should rewrite shader into a Surface Shader.
/*SubShader {
    /* Upgrade NOTE: commented out, possibly part of old style per-pixel lighting: Blend AppSrcAdd AppDstAdd */
    Fog { Color [_AddFog] }
    TexCount 4
    
	UsePass  "Skin/Specular/BASE"
	
	// Pixel lights
	Pass {
		Name "PPL"
		Tags { "LightMode" = "Pixel" }
CGPROGRAM
// Upgrade NOTE: excluded shader from DX11 and Xbox360; has structs without semantics (struct v2f members uvK,viewDirT,lightDirT,bumpUV)
#pragma exclude_renderers d3d11 xbox360
// Upgrade NOTE: excluded shader from Xbox360; has structs without semantics (struct v2f members uvK,viewDirT,lightDirT,bumpUV)
#pragma exclude_renderers xbox360
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_builtin
#pragma fragmentoption ARB_fog_exp2
#pragma fragmentoption ARB_precision_hint_fastest 
#include "UnityCG.cginc"
#include "AutoLight.cginc" 
#include "skinhelpers.cginc"
struct v2f {
	float4 pos : SV_POSITION;
	LIGHTING_COORDS
	float3	uvK; 	// xy = UV, z = specular K
	float3	viewDirT;
	float3	lightDirT;
	float2 	bumpUV;
}; 
 
uniform float _Shininess;
uniform float4 _MainTex_ST, _BumpMap_ST;
 
v2f vert (appdata_tan v)
{
	v2f o;
	o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
	o.uvK.z = _Shininess * 128;
	o.uvK.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
	o.bumpUV = TRANSFORM_TEX(v.texcoord, _BumpMap);

	TANGENT_SPACE_ROTATION;
	o.lightDirT = normalize (mul (rotation, ObjSpaceLightDir( v.vertex )));
	o.viewDirT = normalize (mul (rotation, ObjSpaceViewDir( v.vertex )));


	TRANSFER_VERTEX_TO_FRAGMENT(o);
	return o;
}
 
uniform sampler2D _MainTex;
uniform sampler2D _BumpMap;

float4 frag (v2f i) : COLOR
{    
	half4 texcol = tex2D( _MainTex, i.uvK.xy );
	float3 normalT = normalize (tex2D (_BumpMap, i.bumpUV).rgb * 2 - 1);
	return DiffuseLightWrap(i.lightDirT, i.viewDirT, normalT, texcol, LIGHT_ATTENUATION(i));
}
ENDCG

	}
}*/
 
Fallback "Skin/Diffuse", 0 
}