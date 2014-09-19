uniform sampler2D _WrapTex;
uniform sampler2D _RimTex;

inline half4 RimLight (
	half3 viewDir, half3 normal) 
{	
	// rim factor
	float rim = dot(normal, viewDir );
	return tex2D (_RimTex, rim.xx);
}

// Mostly copied from UnityCG.cginc; modified to use wrap lighting
// instead of regular diffuse.
inline half4 SpecularLightWrap(
	half3 lightDir, half3 viewDir, half3 normal,
	half4 color, float specK, half atten)
{
	half3 h = normalize( lightDir + viewDir );
	half4 rim = RimLight (viewDir, normal);
	
	// This is "wrap diffuse" lighting.
	// What we do here is to calculate the color, then look up the wrap coloring ramp so you can tint things properly
	half diffusePos = dot(normal, lightDir) * 0.5 + 0.5;
	half4 diffuse = tex2D (_WrapTex, diffusePos.xx);
	diffuse.rgb *= rim.rgb * 4;

	float nh = saturate( dot( h, normal ) );
	float spec = pow( nh, specK ) * color.a;
	
	half4 c;
	c.rgb = (color.rgb * _ModelLightColor0.rgb * diffuse + _SpecularLightColor0.rgb * spec * rim.a) * (atten * 2);
	// specular passes by default put highlights to overbright
	c.a = _SpecularLightColor0.a * spec * atten;
	return c;
} 

// Mostly copied from UnityCG.cginc; modified to use wrap lighting
// instead of regular diffuse.
inline half4 DiffuseLightWrap(
	half3 lightDir, half3 viewDir, half3 normal,
	half4 color, half atten)
{
	half4 rim = RimLight (viewDir, normal);
	
	// This is "wrap diffuse" lighting.
	// What we do here is to calculate the color, then look up the wrap coloring ramp so you can tint things properly
	half diffusePos = dot(normal, lightDir) * 0.5 + 0.5;
	half4 diffuse = tex2D (_WrapTex, diffusePos.xx);
	diffuse.rgb *= rim.rgb * 4;

	half4 c;
	c.rgb = (color.rgb * _ModelLightColor0.rgb * diffuse) * (atten * 2);
	// specular passes by default put highlights to overbright
	c.a = 0;
	return c;
} 
