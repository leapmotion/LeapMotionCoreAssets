// Upgrade NOTE: replaced 'PositionFog()' with multiply of UNITY_MATRIX_MVP by position
// Upgrade NOTE: replaced 'V2F_POS_FOG' with 'float4 pos : SV_POSITION'
// Upgrade NOTE: replaced '_PPLAmbient' with 'UNITY_LIGHTMODEL_AMBIENT'

Shader "Skin/Specular" {
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
	_Shininess ("Shininess", Range (0.01, 1)) = 0.078125
	_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
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
    
    
    // Ambient pass
    // This does ambient + rim lighting. Note that if you have vertexlit
    // lights in the scene, then standard vertex lighting will be used instead,
    // so you'll lose rim lighting.
    Pass {
        Name "BASE"
        Tags {"LightMode" = "Always" /* Upgrade NOTE: changed from PixelOrNone to Always */}
CGPROGRAM
#pragma fragment frag
#pragma vertex vert
#pragma fragmentoption ARB_fog_exp2
#pragma fragmentoption ARB_precision_hint_fastest
#include "UnityCG.cginc"
#include "skinhelpers.cginc"
 
struct v2f {
    float4 pos : SV_POSITION;
    float2    uv            : TEXCOORD0;
    float3    viewDir        : TEXCOORD1;
    float3    normal        : TEXCOORD2;
}; 
 
v2f vert (appdata_base v)
{
    v2f o;
    o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
    o.normal = v.normal;
    o.uv = TRANSFORM_UV(0);
    o.viewDir = ObjSpaceViewDir( v.vertex );
    return o;
}
 
uniform float4 _Color;
 
uniform sampler2D _MainTex;
float4 frag (v2f i)  : COLOR
{
	half4 texcol = tex2D( _MainTex, i.uv );
	
	half3 ambient = texcol.rgb * (UNITY_LIGHTMODEL_AMBIENT.rgb * 2 * RimLight (i.viewDir, i.normal));
	return float4( ambient, texcol.a * _Color.a );
}
ENDCG
    }
    
    
    // Vertex lights
    Pass {
        Name "BASE"
        Tags {"LightMode" = "Vertex"}
        Lighting On
        Material {
            Diffuse [_Color]
            Emission [_PPLAmbient]
            Specular [_SpecColor]
            Shininess [_Shininess]
        }
        SeparateSpecular On
 
CGPROGRAM
// Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it does not contain a surface program or both vertex and fragment programs.
#pragma exclude_renderers gles
#pragma fragment
#pragma fragmentoption ARB_fog_exp2
#pragma fragmentoption ARB_precision_hint_fastest
 
#include "UnityCG.cginc"

 
uniform sampler2D _MainTex;
 
half4 main (v2f_vertex_lit i) : COLOR {
    return VertexLight( i, _MainTex );
} 
ENDCG
 
        SetTexture [_MainTex] {combine texture}
    }
    
    
    // Pixel lights
    Pass {
        Name "PPL"
        Tags { "LightMode" = "Pixel" }
CGPROGRAM
// Upgrade NOTE: excluded shader from DX11 and Xbox360; has structs without semantics (struct v2f members uvK)
#pragma exclude_renderers d3d11 xbox360
// Upgrade NOTE: excluded shader from Xbox360; has structs without semantics (struct v2f members uvK)
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
    float3    viewDir        : TEXCOORD1;
    float3    normal        : TEXCOORD2;
    float3    lightDir    : TEXCOORD3;
}; 
 
uniform float _Shininess;
 
v2f vert (appdata_base v)
{
	v2f o;
	o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
	o.normal = v.normal;
	o.uvK.xy = TRANSFORM_UV(0);
	o.uvK.z = _Shininess * 128;
	o.lightDir = normalize (ObjSpaceLightDir( v.vertex ));
	o.viewDir = normalize (ObjSpaceViewDir( v.vertex ));
	TRANSFER_VERTEX_TO_FRAGMENT(o);	
	return o;
}
 
uniform sampler2D _MainTex;
 
float4 frag (v2f i)  : COLOR
{    
	half4 texcol = tex2D( _MainTex, i.uvK.xy );
	return SpecularLightWrap( i.lightDir, i.viewDir, i.normal, texcol, i.uvK.z, LIGHT_ATTENUATION(i));
}
ENDCG
    }
}*/
 
Fallback " Glossy", 0
 
}