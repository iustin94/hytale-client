/**************************
***** Compiler Parameters *****
***************************
@P EffectName: SSLRTemporalPass
***************************
****  ConstantBuffers  ****
***************************
cbuffer PerDraw [Size: 64]
@C    MatrixTransform_id73 => SpriteBase.MatrixTransform
cbuffer Globals [Size: 224]
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
@C    TemporalResponse_id74 => SSLRTemporalPass.TemporalResponse
@C    TemporalScale_id75 => SSLRTemporalPass.TemporalScale
@C    IVP_id76 => SSLRTemporalPass.IVP
@C    prevVP_id77 => SSLRTemporalPass.prevVP
***************************
******  Resources    ******
***************************
@R    PerDraw => PerDraw [Stage: Vertex, Slot: (-1--1)]
@R    Globals => Globals [Stage: Pixel, Slot: (-1--1)]
@R    Texture0_id14 => Texturing.Texture0 [Stage: Pixel, Slot: (-1--1)]
@R    Texture1_id16 => Texturing.Texture1 [Stage: Pixel, Slot: (-1--1)]
@R    Texture2_id18 => Texturing.Texture2 [Stage: Pixel, Slot: (-1--1)]
@R    PointSampler_id43 => Texturing.PointSampler [Stage: Pixel, Slot: (-1--1)]
@R    LinearSampler_id44 => Texturing.LinearSampler [Stage: Pixel, Slot: (-1--1)]
***************************
*****     Sources     *****
***************************
@S    SSLRTemporalPass => a935dc4eecf44b3cb0874f38000b1f57
@S    ImageEffectShader => 8064e30cc02e5eb4052f420259dbf05e
@S    SpriteBase => 5a7aa9dfd5b5c7613053f4f66c79ca0d
@S    ShaderBase => 4ecbcd2528b64a79eebe81a863892d8c
@S    ShaderBaseStream => b705b699a7385d39c7de52a8d13f3978
@S    Texturing => 91ef3011c1071c2e5d41cd3ee0418b18
***************************
*****     Stages      *****
***************************
@G    Vertex => 7bd8d32da3d2924bfe993fd32e966f57
@G    Pixel => d3a23336df3a9d25b9d9ce901cfcb3b5
***************************
*************************/
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
    float TemporalResponse_id74;
    float TemporalScale_id75;
    float4x4 IVP_id76;
    float4x4 prevVP_id77;
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
float2 UvToClip_id4(float2 uv)
{
    return uv * float2(2, -2) + float2(-1, 1);
}
float3 ComputeWorldPosition_id5(float2 uv, float rawDepth)
{
    float4 clipPos = float4(UvToClip_id4(uv), rawDepth, 1);
    float4 pos = mul(clipPos, IVP_id76);
    return pos.xyz / pos.w;
}
float2 ClipToUv_id3(float2 clipPos)
{
    return clipPos * float2(0.5, -0.5) + float2(0.5, 0.5);
}
float3 SampleWorldPosition_id6(float2 uv)
{
    float rawDepth = Texture2_id18.SampleLevel(PointSampler_id43, uv, 0).r;
    return ComputeWorldPosition_id5(uv, rawDepth);
}
float4 Shading_id7(inout PS_STREAMS streams)
{
    float2 uv = streams.TexCoord_id62;
    float3 posWS = SampleWorldPosition_id6(uv);
    float4 prevSS = mul(float4(posWS, 1), prevVP_id77);
    prevSS.xy /= prevSS.w;
    float2 prevUV = ClipToUv_id3(prevSS.xy);
    float4 current = Texture0_id14.SampleLevel(LinearSampler_id44, uv, 0);
    float4 previous = Texture1_id16.SampleLevel(LinearSampler_id44, prevUV, 0);
    float2 du = float2(Texture0TexelSize_id15.x, 0.0);
    float2 dv = float2(0.0, Texture0TexelSize_id15.y);
    float4 currentTopLeft = Texture0_id14.SampleLevel(LinearSampler_id44, uv.xy - dv - du, 0);
    float4 currentTopCenter = Texture0_id14.SampleLevel(LinearSampler_id44, uv.xy - dv, 0);
    float4 currentTopRight = Texture0_id14.SampleLevel(LinearSampler_id44, uv.xy - dv + du, 0);
    float4 currentMiddleLeft = Texture0_id14.SampleLevel(LinearSampler_id44, uv.xy - du, 0);
    float4 currentMiddleCenter = Texture0_id14.SampleLevel(LinearSampler_id44, uv.xy, 0);
    float4 currentMiddleRight = Texture0_id14.SampleLevel(LinearSampler_id44, uv.xy + du, 0);
    float4 currentBottomLeft = Texture0_id14.SampleLevel(LinearSampler_id44, uv.xy + dv - du, 0);
    float4 currentBottomCenter = Texture0_id14.SampleLevel(LinearSampler_id44, uv.xy + dv, 0);
    float4 currentBottomRight = Texture0_id14.SampleLevel(LinearSampler_id44, uv.xy + dv + du, 0);
    float4 currentMin = min(currentTopLeft, min(currentTopCenter, min(currentTopRight, min(currentMiddleLeft, min(currentMiddleCenter, min(currentMiddleRight, min(currentBottomLeft, min(currentBottomCenter, currentBottomRight))))))));
    float4 currentMax = max(currentTopLeft, max(currentTopCenter, max(currentTopRight, max(currentMiddleLeft, max(currentMiddleCenter, max(currentMiddleRight, max(currentBottomLeft, max(currentBottomCenter, currentBottomRight))))))));
    float scale = TemporalScale_id75;
    float4 center = (currentMin + currentMax) * 0.5f;
    currentMin = (currentMin - center) * scale + center;
    currentMax = (currentMax - center) * scale + center;
    previous = clamp(previous, currentMin, currentMax);
    return lerp(current, previous, TemporalResponse_id74);
}
PS_OUTPUT PSMain(PS_INPUT __input__)
{
    PS_STREAMS streams = (PS_STREAMS)0;
    streams.TexCoord_id62 = __input__.TexCoord_id62;
    streams.ColorTarget_id2 = Shading_id7(streams);
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
