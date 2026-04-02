/**************************
***** Compiler Parameters *****
***************************
@P EffectName: SSLRResolvePassEffect
@P   - SSLR.ResolveSamples: 4
@P SSLR.ReduceHighlights: True
***************************
****  ConstantBuffers  ****
***************************
cbuffer Data [Size: 192]
@C    MaxColorMiplevel_id74 => SSLRCommon.MaxColorMiplevel
@C    TraceSizeMax_id75 => SSLRCommon.TraceSizeMax
@C    MaxTraceSamples_id76 => SSLRCommon.MaxTraceSamples
@C    RoughnessFade_id77 => SSLRCommon.RoughnessFade
@C    TemporalTime_id78 => SSLRCommon.TemporalTime
@C    BRDFBias_id79 => SSLRCommon.BRDFBias
@C    ViewFarPlane_id80 => SSLRCommon.ViewFarPlane
@C    ViewInfo_id81 => SSLRCommon.ViewInfo
@C    CameraPosWS_id82 => SSLRCommon.CameraPosWS
@C    WorldAntiSelfOcclusionBias_id83 => SSLRCommon.WorldAntiSelfOcclusionBias
@C    V_id84 => SSLRCommon.V
@C    IVP_id85 => SSLRCommon.IVP
cbuffer PerDraw [Size: 64]
@C    MatrixTransform_id73 => SpriteBase.MatrixTransform
cbuffer Globals [Size: 80]
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
***************************
******  Resources    ******
***************************
@R    Data => Data [Stage: Pixel, Slot: (-1--1)]
@R    PerDraw => PerDraw [Stage: Vertex, Slot: (-1--1)]
@R    Globals => Globals [Stage: Pixel, Slot: (-1--1)]
@R    Texture0_id14 => Texturing.Texture0 [Stage: Pixel, Slot: (-1--1)]
@R    Texture1_id16 => Texturing.Texture1 [Stage: Pixel, Slot: (-1--1)]
@R    Texture2_id18 => Texturing.Texture2 [Stage: Pixel, Slot: (-1--1)]
@R    Texture3_id20 => Texturing.Texture3 [Stage: Pixel, Slot: (-1--1)]
@R    Texture4_id22 => Texturing.Texture4 [Stage: Pixel, Slot: (-1--1)]
@R    PointSampler_id43 => Texturing.PointSampler [Stage: Pixel, Slot: (-1--1)]
@R    LinearSampler_id44 => Texturing.LinearSampler [Stage: Pixel, Slot: (-1--1)]
***************************
*****     Sources     *****
***************************
@S    SSLRResolvePass => 7c643796857bd20bb17d6d175b852b01
@S    ImageEffectShader => 8064e30cc02e5eb4052f420259dbf05e
@S    SpriteBase => 5a7aa9dfd5b5c7613053f4f66c79ca0d
@S    ShaderBase => 4ecbcd2528b64a79eebe81a863892d8c
@S    ShaderBaseStream => b705b699a7385d39c7de52a8d13f3978
@S    Texturing => 91ef3011c1071c2e5d41cd3ee0418b18
@S    SSLRCommon => 161d9b8711411c56906b0b0d551faeae
@S    Utilities => d8e15010fb2006b8edf6bdc952dd31f0
@S    NormalPack => af1a18518fb63b6295c012c18fe9f9c0
@S    Math => 9787fbb9c5fc970c8a2b04e18943e1bd
@S    BRDFMicrofacet => e6c8c59dd59d4fef181ae8322a095acf
***************************
*****     Stages      *****
***************************
@G    Vertex => 7bd8d32da3d2924bfe993fd32e966f57
@G    Pixel => f5d7a51bdcc1f45f1dbc90c10e2b4fb2
***************************
*************************/
const static int ResolveSamples_id87 = 4;
const static bool ReduceHighlights_id88 = true;
static const float2 Offsets_id89[8] = { float2(0, 0), float2(2, -2), float2(-2, -2), float2(0, 2), float2(-2, 0), float2(0, -2), float2(2, 0), float2(2, 2)};
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
typedef uint Half2;
typedef uint2 Half4;
cbuffer Data 
{
    float MaxColorMiplevel_id74;
    float TraceSizeMax_id75;
    float MaxTraceSamples_id76;
    float RoughnessFade_id77;
    float TemporalTime_id78;
    float BRDFBias_id79;
    float ViewFarPlane_id80;
    float4 ViewInfo_id81;
    float3 CameraPosWS_id82;
    float WorldAntiSelfOcclusionBias_id83;
    float4x4 V_id84;
    float4x4 IVP_id85;
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
float2 UvToClip_id16(float2 uv)
{
    return uv * float2(2, -2) + float2(-1, 1);
}
float LinearizeZ_id13(in float depth)
{
    return ViewInfo_id81.w / (depth - ViewInfo_id81.z);
}
float Luminance_id26(float3 color)
{
    return dot(color, float3(0.2126, 0.7152, 0.0722));
}
float2 RandN2_id23(float2 pos, float2 random)
{
    return frac(sin(dot(pos.xy + random, float2(12.9898, 78.233))) * float2(43758.5453, 28001.8384));
}
float3 ComputeWorldPosition_id17(float2 uv, float rawDepth)
{
    float4 clipPos = float4(UvToClip_id16(uv), rawDepth, 1);
    float4 pos = mul(clipPos, IVP_id85);
    return pos.xyz / pos.w;
}
float3 DecodeNormal_id11(float3 enc)
{
    return normalize(enc * 2 - 1);
}
float3 ComputeViewPosition_id19(float2 uv, float rawDepth)
{
    float eyeZ = LinearizeZ_id13(rawDepth) * ViewFarPlane_id80;
    return float3(UvToClip_id16(uv) * ViewInfo_id81.xy * eyeZ, eyeZ);
}
float SampleZ_id12(in float2 uv)
{
    return Texture1_id16.SampleLevel(PointSampler_id43, uv, 0).r;
}
float4 Shading_id59(inout PS_STREAMS streams)
{
    float2 uv = streams.TexCoord_id62;
    if (Texture4_id22.SampleLevel(LinearSampler_id44, uv, 0).z <= 0.001)
        return 0;
    float4 specularRoughnessBuffer = Texture3_id20.SampleLevel(PointSampler_id43, uv, 0);
    float roughness = specularRoughnessBuffer.a;
    float depth = SampleZ_id12(uv);
    float3 positionVS = ComputeViewPosition_id19(uv, depth);
    if (positionVS.z > 100.0f || roughness > RoughnessFade_id77)
        return 0;
    float4 normalsBuffer = Texture2_id18.SampleLevel(PointSampler_id43, uv, 0);
    float3 normalWS = DecodeNormal_id11(normalsBuffer.rgb);
    float3 positionWS = ComputeWorldPosition_id17(uv, depth);
    float3 viewVector = normalize(CameraPosWS_id82.xyz - positionWS);
    float2 random = RandN2_id23(uv, TemporalTime_id78);
    float2 blueNoise = random.xy * 2.0 - 1.0;
    float2x2 offsetRotationMatrix = float2x2(blueNoise.x, blueNoise.y, -blueNoise.y, blueNoise.x);
    float NdotV = saturate(dot(normalWS, viewVector));
    float coneTangent = lerp(0.0, roughness * 5 * (1.0 - BRDFBias_id79), pow(NdotV, 1.5) * sqrt(roughness));
    float4 result = 0.0;

    for (int i = 0; i < ResolveSamples_id87; i++)
    {
        float2 offsetUV = Offsets_id89[i] * Texture4TexelSize_id23;
        offsetUV = mul(offsetRotationMatrix, offsetUV);
        float2 neighborUv = uv + offsetUV;
        float4 hitPacked = Texture4_id22.SampleLevel(LinearSampler_id44, neighborUv, 0);
        float2 hitUv = hitPacked.xy;
        float hitMask = hitPacked.z;
        float intersectionCircleRadius = coneTangent * length(hitUv - uv);
        float mip = clamp(log2(intersectionCircleRadius * TraceSizeMax_id75), 0.0, MaxColorMiplevel_id74);
        float4 sampleColor = float4(Texture0_id14.SampleLevel(LinearSampler_id44, hitUv, mip).rgb, 1);
        sampleColor.rgb /= 1 + Luminance_id26(sampleColor.rgb);
        result += sampleColor * hitMask;
    }
    result /= ResolveSamples_id87;
    result.rgb /= 1 - Luminance_id26(result.rgb);
    result.rgb *= result.a;
    return max(1e-5, result);
}
PS_OUTPUT PSMain(PS_INPUT __input__)
{
    PS_STREAMS streams = (PS_STREAMS)0;
    streams.TexCoord_id62 = __input__.TexCoord_id62;
    streams.ColorTarget_id2 = Shading_id59(streams);
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
