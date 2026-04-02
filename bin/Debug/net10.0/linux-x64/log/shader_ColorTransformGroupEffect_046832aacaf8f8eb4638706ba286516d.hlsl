/**************************
***** Compiler Parameters *****
***************************
@P EffectName: ColorTransformGroupEffect
@P   - ColorTransformGroup.Transforms: Stride.Rendering.Images.ToneMap, Stride.Rendering.Images.LuminanceToChannelTransform
***************************
****  ConstantBuffers  ****
***************************
cbuffer PerDraw [Size: 64]
@C    MatrixTransform_id73 => SpriteBase.MatrixTransform
cbuffer Globals [Size: 108]
@C    Texture0TexelSize_id15 => Texturing.Texture0TexelSize
@C    Texture1TexelSize_id17 => Texturing.Texture1TexelSize
@C    Texture2TexelSize_id19 => Texturing.Texture2TexelSize
@C    Texture3TexelSize_id21 => Texturing.Texture3TexelSize
@C    Texture4TexelSize_id23 => Texturing.Texture4TexelSize
@C    Texture5TexelSize_id25 => Texturing.Texture5TexelSize
@C    Texture6TexelSize_id27 => Texturing.Texture6TexelSize
@C    Texture7TexelSize_id29 => Texturing.Texture7TexelSize
@C    Texture8TexelSize_id31 => Texturing.Texture8TexelSize
@C    Texture9TexelSize_id33 => Texturing.Texture9TexelSize
@C    WhitePoint_id75 => ToneMapHejl2OperatorShader.WhitePoint.ToneMapOperator.Transforms[0]
@C    KeyValue_id80 => ToneMapShader.KeyValue.Transforms[0]
@C    LuminanceLocalFactor_id81 => ToneMapShader.LuminanceLocalFactor.Transforms[0]
@C    LuminanceAverageGlobal_id82 => ToneMapShader.LuminanceAverageGlobal.Transforms[0]
@C    Contrast_id83 => ToneMapShader.Contrast.Transforms[0]
@C    Brightness_id84 => ToneMapShader.Brightness.Transforms[0]
@C    Exposure_id85 => ToneMapShader.Exposure.Transforms[0]
***************************
******  Resources    ******
***************************
@R    PerDraw => PerDraw [Stage: Vertex, Slot: (-1--1)]
@R    Globals => Globals [Stage: Pixel, Slot: (-1--1)]
@R    Texture0_id14 => Texturing.Texture0 [Stage: Pixel, Slot: (-1--1)]
@R    Sampler_id42 => Texturing.Sampler [Stage: Pixel, Slot: (-1--1)]
***************************
*****     Sources     *****
***************************
@S    ColorTransformGroupShader => fb26384f5b6470ecead6ca44fcd040e7
@S    ImageEffectShader => 8064e30cc02e5eb4052f420259dbf05e
@S    SpriteBase => 5a7aa9dfd5b5c7613053f4f66c79ca0d
@S    ShaderBase => 4ecbcd2528b64a79eebe81a863892d8c
@S    ShaderBaseStream => b705b699a7385d39c7de52a8d13f3978
@S    Texturing => 91ef3011c1071c2e5d41cd3ee0418b18
@S    ColorTransformShader => 72c2b9c4ae27125468616d070d5b5284
@S    ToneMapShader => 193caf32ef48a8d6223b763df258cbac
@S    ToneMapOperatorShader => a3ee79064be0fe5996ae9e0269f3348a
@S    ToneMapHejl2OperatorShader => 0d19888a588917597eb3dc1a423cee7d
@S    LuminanceToChannelShader => c4f98351658734a81c669dbc582e65a1
@S    LuminanceUtils => 39cb56630d44d77f1ff5a3ebade5ba1c
***************************
*****     Stages      *****
***************************
@G    Vertex => 7bd8d32da3d2924bfe993fd32e966f57
@G    Pixel => bc4c6a99da35073c32b00e75f1272e5f
***************************
*************************/
const static bool TAutoKeyValue_id76 = true;
const static bool TAutoExposure_id77 = true;
const static bool TUseLocalLuminance_id78 = false;
struct PS_STREAMS 
{
    float2 TexCoord_id62;
    float4 ColorTarget_id2;
};
struct PS_OUTPUT 
{
    float4 ColorTarget_id2 : SV_Target0;
};
struct PS_INPUT 
{
    float4 ShadingPosition_id0 : SV_Position;
    float2 TexCoord_id62 : TEXCOORD0;
};
struct VS_STREAMS 
{
    float4 Position_id72;
    float2 TexCoord_id62;
    float4 ShadingPosition_id0;
};
struct VS_OUTPUT 
{
    float4 ShadingPosition_id0 : SV_Position;
    float2 TexCoord_id62 : TEXCOORD0;
};
struct VS_INPUT 
{
    float4 Position_id72 : POSITION;
    float2 TexCoord_id62 : TEXCOORD0;
};
cbuffer PerDraw 
{
    float4x4 MatrixTransform_id73;
};
cbuffer Globals 
{
    float2 Texture0TexelSize_id15;
    float2 Texture1TexelSize_id17;
    float2 Texture2TexelSize_id19;
    float2 Texture3TexelSize_id21;
    float2 Texture4TexelSize_id23;
    float2 Texture5TexelSize_id25;
    float2 Texture6TexelSize_id27;
    float2 Texture7TexelSize_id29;
    float2 Texture8TexelSize_id31;
    float2 Texture9TexelSize_id33;
    float WhitePoint_id75 = 5.0f;
    float KeyValue_id80 = 0.18f;
    float LuminanceLocalFactor_id81 = 0.0f;
    float LuminanceAverageGlobal_id82;
    float Contrast_id83 = 0.0f;
    float Brightness_id84 = 0.0f;
    float Exposure_id85 = 1.0f;
};
Texture2D Texture0_id14;
Texture2D Texture1_id16;
Texture2D Texture2_id18;
Texture2D Texture3_id20;
Texture2D Texture4_id22;
Texture2D Texture5_id24;
Texture2D Texture6_id26;
Texture2D Texture7_id28;
Texture2D Texture8_id30;
Texture2D Texture9_id32;
TextureCube TextureCube0_id34;
TextureCube TextureCube1_id35;
TextureCube TextureCube2_id36;
TextureCube TextureCube3_id37;
Texture3D Texture3D0_id38;
Texture3D Texture3D1_id39;
Texture3D Texture3D2_id40;
Texture3D Texture3D3_id41;
SamplerState Sampler_id42;
SamplerState PointSampler_id43 
{
    Filter = MIN_MAG_MIP_POINT;
};
SamplerState LinearSampler_id44 
{
    Filter = MIN_MAG_MIP_LINEAR;
};
SamplerState LinearBorderSampler_id45 
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Border;
    AddressV = Border;
};
SamplerComparisonState LinearClampCompareLessEqualSampler_id46 
{
    Filter = COMPARISON_MIN_MAG_LINEAR_MIP_POINT;
    AddressU = Clamp;
    AddressV = Clamp;
    ComparisonFunc = LessEqual;
};
SamplerState AnisotropicSampler_id47 
{
    Filter = ANISOTROPIC;
};
SamplerState AnisotropicRepeatSampler_id48 
{
    Filter = ANISOTROPIC;
    AddressU = Wrap;
    AddressV = Wrap;
    MaxAnisotropy = 16;
};
SamplerState PointRepeatSampler_id49 
{
    Filter = MIN_MAG_MIP_POINT;
    AddressU = Wrap;
    AddressV = Wrap;
};
SamplerState LinearRepeatSampler_id50 
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
};
SamplerState RepeatSampler_id51 
{
    AddressU = Wrap;
    AddressV = Wrap;
};
SamplerState Sampler0_id52;
SamplerState Sampler1_id53;
SamplerState Sampler2_id54;
SamplerState Sampler3_id55;
SamplerState Sampler4_id56;
SamplerState Sampler5_id57;
SamplerState Sampler6_id58;
SamplerState Sampler7_id59;
SamplerState Sampler8_id60;
SamplerState Sampler9_id61;
Texture2D LuminanceTexture_id79;
float4 Compute_id9(float4 color)
{
    float w = (1.425 * WhitePoint_id75) + 0.05f;
    w = ((WhitePoint_id75 * w + 0.004f) / ((WhitePoint_id75 * (w + 0.55f) + 0.0491f))) - 0.0821f;
    float4 vh = float4(color.rgb, WhitePoint_id75);
    float4 va = (1.425 * vh) + 0.05f;
    float4 vf = ((vh * va + 0.004f) / ((vh * (va + 0.55f) + 0.0491f))) - 0.0821f;
    return float4(vf.rgb / w, 1.0);
}
float CalculateExposure_id7(float avgLuminance)
{
    float exposure;

    {
        float keyValue;

        {
            keyValue = 1.03f - (2.0f / (2 + log10(avgLuminance + 1)));
        }
        float linearExposure = (keyValue / avgLuminance);
        exposure = max(linearExposure, 0.0001f);
    }
    return exposure;
}
static float Luma_id12(float3 color)
{
    return max(dot(color, float3(0.299, 0.587, 0.114)), 0.0001);
}
float3 ToneMap_id6(float3 color, float avgLuminance)
{
    float exposure = CalculateExposure_id7(avgLuminance);
    color *= exposure;
    color = Compute_id9(float4(color, 1)).rgb;
    return color;
}
float4 Compute_id5(float4 color)
{
    float4 outColor = color;
    outColor.a = Luma_id12(color.rgb);
    return outColor;
}
float4 Compute_id4(inout PS_STREAMS streams, float4 inputColor)
{
    float3 color = inputColor.rgb;
    float avgLuminance = LuminanceAverageGlobal_id82;
    avgLuminance = exp2(avgLuminance);
    avgLuminance = max(avgLuminance, 0.0001f);
    float globalAverageLum = exp2(LuminanceAverageGlobal_id82);
    color = max(color + globalAverageLum.xxx * Brightness_id84, 0.0001);
    color = max(lerp(globalAverageLum.xxx, color, Contrast_id83 + 1.0f), 0.0001);
    color = ToneMap_id6(color, avgLuminance);
    return float4(color, inputColor.a);
}
float4 Shading_id2(inout PS_STREAMS streams)
{
    return Texture0_id14.Sample(Sampler_id42, streams.TexCoord_id62);
}
float4 Shading_id3(inout PS_STREAMS streams)
{
    float4 color = Shading_id2(streams);

    {
        color = Compute_id4(streams, color);
    }

    {
        color = Compute_id5(color);
    }
    return color;
}
PS_OUTPUT PSMain(PS_INPUT __input__)
{
    PS_STREAMS streams = (PS_STREAMS)0;
    streams.TexCoord_id62 = __input__.TexCoord_id62;
    streams.ColorTarget_id2 = Shading_id3(streams);
    PS_OUTPUT __output__ = (PS_OUTPUT)0;
    __output__.ColorTarget_id2 = streams.ColorTarget_id2;
    return __output__;
}
VS_OUTPUT VSMain(VS_INPUT __input__)
{
    VS_STREAMS streams = (VS_STREAMS)0;
    streams.Position_id72 = __input__.Position_id72;
    streams.TexCoord_id62 = __input__.TexCoord_id62;
    streams.ShadingPosition_id0 = mul(streams.Position_id72, MatrixTransform_id73);
    VS_OUTPUT __output__ = (VS_OUTPUT)0;
    __output__.ShadingPosition_id0 = streams.ShadingPosition_id0;
    __output__.TexCoord_id62 = streams.TexCoord_id62;
    return __output__;
}
