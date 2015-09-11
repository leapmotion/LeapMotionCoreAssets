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

sampler2D _LeapTexture;
sampler2D _LeapDistortion;

float4 _LeapProjection;
float _LeapGammaCorrectionExponent;

// Virtual Camera Parameters
float _VirtualCameraH; //degrees
float _VirtualCameraV; //degrees
float4x4 _InverseView;

// Global Coordinate Transformation of viewer from Image.Timestamp to Controller.Now
float4x4 _ViewerImageToNow;

float2 LeapGetUndistortedUV(float4 screenPos){
  float2 uv = (screenPos.xy / screenPos.w) * 2 - float2(1,1);
  float2 tangent = (uv + _LeapProjection.xy) / _LeapProjection.zw;
  float2 distortionUV = 0.125 * tangent + float2(0.5, 0.5);

  float4 distortionAmount = tex2D(_LeapDistortion, distortionUV);
  return float2(DecodeFloatRG(distortionAmount.xy), DecodeFloatRG(distortionAmount.zw)) * 2.3 - float2(0.6, 0.6);
}

float LeapRawBrightnessUV(float2 uv){
  #if LEAP_FORMAT_IR
    return tex2D(_LeapTexture, uv).a;
  #else
    float4 rawColor = tex2D(_LeapTexture, uv);

    float ir;
    ir = dot(rawColor, float4(G_BLEED, B_BLEED, R_BLEED, IR_BLEED));
    ir = saturate(ir);
    ir = pow(ir, 0.5);

    return ir;
  #endif
}

float3 LeapRawColorUV(float2 uv){
  #if LEAP_FORMAT_IR
    float brightness = LeapRawBrightnessUV(uv);
    return float3(brightness, brightness, brightness);
  #else
    float4 input_lf;

    uv.y = clamp(uv.y, 0.01, 0.99);

    input_lf.a = tex2D(_LeapTexture, uv).a;
    input_lf.r = tex2D(_LeapTexture, uv + R_OFFSET).b;
    input_lf.g = tex2D(_LeapTexture, uv + G_OFFSET).r;
    input_lf.b = tex2D(_LeapTexture, uv + B_OFFSET).g;

    float4 output_lf       = mul(TRANSFORMATION, input_lf);
    float4 output_lf_fudge = mul(CONSERVATIVE,   input_lf);

    float3 fudgeMult = input_lf.rgb * FUDGE_CONSTANT - FUDGE_CONSTANT * FUDGE_THRESHOLD;
    float3 fudge = step(FUDGE_THRESHOLD, input_lf.rgb) * fudgeMult;

    float3 color = (output_lf_fudge.rgb - output_lf.rgb) * fudge * fudge + output_lf.rgb;
    color *= RGB_SCALE;

    return saturate(color);
  #endif
}

float4 LeapRawColorBrightnessUV(float2 uv){
  #if LEAP_FORMAT_IR
    float brightness = LeapRawBrightnessUV(uv);
    return float4(brightness, brightness, brightness, brightness);
  #else
    float4 input_lf;

    uv.y = clamp(uv.y, 0.01, 0.99);

    float4 noOffset = tex2D(_LeapTexture, uv);
    input_lf.a = noOffset.a;
    input_lf.r = tex2D(_LeapTexture, uv + R_OFFSET).b;
    input_lf.g = tex2D(_LeapTexture, uv + G_OFFSET).r;
    input_lf.b = tex2D(_LeapTexture, uv + B_OFFSET).g;

    float4 output_lf       = mul(TRANSFORMATION, input_lf);
    float4 output_lf_fudge = mul(CONSERVATIVE,   input_lf);

    float3 fudgeMult = input_lf.rgb * FUDGE_CONSTANT - FUDGE_CONSTANT * FUDGE_THRESHOLD;
    float3 fudge = step(FUDGE_THRESHOLD, input_lf.rgb) * fudgeMult;

    float3 color = (output_lf_fudge.rgb - output_lf.rgb) * fudge * fudge + output_lf.rgb;
    color *= RGB_SCALE;

    float ir = dot(noOffset, float4(G_BLEED, B_BLEED, R_BLEED, IR_BLEED));
    ir = saturate(ir);
    ir = pow(ir, 0.5);

    return float4(color, ir);
  #endif
}

float4 WarpScreenPosition(float4 sp) {
  // Map pixels to clipping coordinates
  float2 window = float2(1.0, 1.0) - sp.xy*2.0/sp.w;
  //range (-1,1), x is horizontal, y is vertical, origin is center
      
  // Map window coordinates to world coordinates
  float4 viewDir = float4(tan(radians(_VirtualCameraH) / 2.0)*window.x, tan(radians(_VirtualCameraV) / 2.0)*window.y, 1.0, 0.0);
  float4 worldDir = mul(_InverseView, viewDir);
      
  // Apply time warping
  worldDir = mul(_ViewerImageToNow, worldDir);
      
  // Return to pixel coordinates
  return ComputeScreenPos(mul(UNITY_MATRIX_VP, worldDir));
}

float LeapRawBrightness(float4 screenPos){
  return LeapRawBrightnessUV(LeapGetUndistortedUV(screenPos));
}

float3 LeapRawColor(float4 screenPos){
  return LeapRawColorUV(LeapGetUndistortedUV(screenPos));
}

float4 LeapRawColorBrightness(float4 screenPos){
  return LeapRawColorBrightnessUV(LeapGetUndistortedUV(screenPos));
}

float LeapBrightnessUV(float2 uv){
  return pow(LeapRawBrightnessUV(uv), _LeapGammaCorrectionExponent);
}

float3 LeapColorUV(float2 uv){
  return pow(LeapRawColorUV(uv), _LeapGammaCorrectionExponent);
}

float4 LeapColorBrightnessUV(float2 uv){
  return pow(LeapRawColorBrightnessUV(uv), _LeapGammaCorrectionExponent);
}

float LeapBrightness(float4 screenPos){
  return pow(LeapRawBrightness(screenPos), _LeapGammaCorrectionExponent);
}

float3 LeapColor(float4 screenPos){
  return pow(LeapRawColor(screenPos), _LeapGammaCorrectionExponent);
}

float4 LeapColorBrightness(float4 screenPos){
  return pow(LeapRawColorBrightness(screenPos), _LeapGammaCorrectionExponent);
}

float3 LeapRawColorWarp(float4 screenPos){
  return LeapRawColorBrightness(WarpScreenPosition(screenPos));
}

float4 LeapRawColorBrightnessWarp(float4 screenPos){
  return LeapRawColorBrightness(WarpScreenPosition(screenPos));
}

float3 LeapColorWarp(float4 screenPos){
  return pow(LeapRawColorBrightnessWarp(screenPos), _LeapGammaCorrectionExponent);
}

float4 LeapColorBrightnessWarp(float4 screenPos){
  return pow(LeapRawColorBrightnessWarp(screenPos), _LeapGammaCorrectionExponent);
}
