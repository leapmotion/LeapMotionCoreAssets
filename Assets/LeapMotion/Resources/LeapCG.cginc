#include "UnityCG.cginc"

/////////////// Constants for Dragonfly Color Correction ///////////////
#define CAMERA_WIDTH  608.0
#define CAMERA_HEIGHT 540.0
#define CAMERA_DELTA  float2(1.0 / CAMERA_WIDTH, 1.0 / CAMERA_HEIGHT)

#define RGB_SCALE     1.5 * float3(1.5, 1.0, 0.5)

#define R_OFFSET      CAMERA_DELTA * float2(-0.5, 0.0)
#define G_OFFSET      CAMERA_DELTA * float2(-0.5, 0.5)
#define B_OFFSET      CAMERA_DELTA * float2( 0.0, 0.5)

#define R_BLEED       -0.05
#define G_BLEED       0.001
#define B_BLEED       -0.105
#define IR_BLEED      1.0

#define TRANSFORMATION  transpose(float4x4(5.0670, -1.2312, 0.8625, -0.0507, -1.5210, 3.1104, -2.0194, 0.0017, -0.8310, -0.3000, 13.1744, -0.1052, -2.4540, -1.3848, -10.9618, 1.0000))
#define CONSERVATIVE    transpose(float4x4(5.0670, 0.0000, 0.8625, 0.0000, 0.0000, 3.1104, 0.0000, 0.0017, 0.0000, 0.0000, 13.1744, 0.0000, 0.0000, 0.0000, 0.0000, 1.0000))

#define FUDGE_THRESHOLD 0.5
#define FUDGE_CONSTANT  (1 / (1 - FUDGE_THRESHOLD))
////////////////////////////////////////////////////////////////////////                                       

sampler2D _LeapGlobalBrightnessTexture;
sampler2D _LeapGlobalRawTexture;
sampler2D _LeapGlobalDistortion;

float4 _LeapGlobalProjection;
float _LeapGlobalGammaCorrectionExponent;

float4x4 _LeapGlobalWarpedOffset;

float2 LeapGetUndistortedUV(float4 screenPos){
  float2 uv = (screenPos.xy / screenPos.w) * 2 - float2(1,1);
  float2 tangent = (uv + _LeapGlobalProjection.xy) / _LeapGlobalProjection.zw;
  float2 distortionUV = 0.125 * tangent + float2(0.5, 0.5);

  float4 distortionAmount = tex2D(_LeapGlobalDistortion, distortionUV);
  return float2(DecodeFloatRG(distortionAmount.xy), DecodeFloatRG(distortionAmount.zw)) * 2.3 - float2(0.6, 0.6);
}

float4 LeapGetWarpedScreenPosition(float4 modelSpaceVertex){
  float4 cameraSpace = mul(UNITY_MATRIX_MV, modelSpaceVertex);
  float4 warpedSpace = mul(_LeapGlobalWarpedOffset, cameraSpace);
  float4 position = mul(UNITY_MATRIX_P, warpedSpace);
  return ComputeScreenPos(position);
}

float LeapBrightnessUV(float2 uv){
    return tex2D(_LeapGlobalBrightnessTexture, uv).a;
}

float LeapBrightness(float4 screenPos){
  return LeapBrightnessUV(LeapGetUndistortedUV(screenPos));
}

float3 LeapRawColorUV(float2 uv){
  #if LEAP_FORMAT_IR
    float color = tex2D(_LeapGlobalRawTexture, uv).a;
    return float3(color, color, color);
  #else
    float4 input_lf;

    uv.y = clamp(uv.y, 0.01, 0.99);

    input_lf.a = tex2D(_LeapGlobalRawTexture, uv).a;
    input_lf.r = tex2D(_LeapGlobalRawTexture, uv + R_OFFSET).b;
    input_lf.g = tex2D(_LeapGlobalRawTexture, uv + G_OFFSET).r;
    input_lf.b = tex2D(_LeapGlobalRawTexture, uv + B_OFFSET).g;

    float4 output_lf       = mul(TRANSFORMATION, input_lf);
    float4 output_lf_fudge = mul(CONSERVATIVE,   input_lf);

    float3 fudgeMult = input_lf.rgb * FUDGE_CONSTANT - FUDGE_CONSTANT * FUDGE_THRESHOLD;
    float3 fudge = step(FUDGE_THRESHOLD, input_lf.rgb) * fudgeMult;

    float3 color = (output_lf_fudge.rgb - output_lf.rgb) * fudge * fudge + output_lf.rgb;
    color *= RGB_SCALE;

    return saturate(color);
  #endif
}

float3 LeapRawColor(float4 screenPos){
  return LeapRawColorUV(LeapGetUndistortedUV(screenPos));
}

float3 LeapColorUV(float2 uv){
  return pow(LeapRawColorUV(uv), _LeapGlobalGammaCorrectionExponent);
}

float3 LeapColor(float4 screenPos){
  return pow(LeapRawColor(screenPos), _LeapGlobalGammaCorrectionExponent);
}