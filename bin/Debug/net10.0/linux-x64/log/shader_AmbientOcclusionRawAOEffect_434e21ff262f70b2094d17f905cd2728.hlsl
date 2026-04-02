/**************************
***** Compiler Parameters *****
***************************
@P EffectName: AmbientOcclusionRawAOEffect
@P   - AmbientOcclusionRawAO.Count: 13
@P AmbientOcclusionRawAO.IsOrthographic: True
***************************
****  ConstantBuffers  ****
***************************
cbuffer PerDraw [Size: 64]
@C    MatrixTransform_id73 => SpriteBase.MatrixTransform
cbuffer PerView [Size: 28]
@C    NearClipPlane_id74 => Camera.NearClipPlane
@C    FarClipPlane_id75 => Camera.FarClipPlane
@C    ZProjection_id76 => Camera.ZProjection
@C    ViewSize_id77 => Camera.ViewSize
@C    AspectRatio_id78 => Camera.AspectRatio
cbuffer Globals [Size: 132]
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
@C    ProjInfo_id81 => AmbientOcclusionRawAOShader.ProjInfo
@C    ScreenInfo_id82 => AmbientOcclusionRawAOShader.ScreenInfo
@C    ParamProjScale_id83 => AmbientOcclusionRawAOShader.ParamProjScale
@C    ParamIntensity_id84 => AmbientOcclusionRawAOShader.ParamIntensity
@C    ParamBias_id85 => AmbientOcclusionRawAOShader.ParamBias
@C    ParamRadius_id86 => AmbientOcclusionRawAOShader.ParamRadius
@C    ParamRadiusSquared_id87 => AmbientOcclusionRawAOShader.ParamRadiusSquared
***************************
******  Resources    ******
***************************
@R    PerDraw => PerDraw [Stage: Vertex, Slot: (-1--1)]
@R    PerView => PerView [Stage: Pixel, Slot: (-1--1)]
@R    Globals => Globals [Stage: Pixel, Slot: (-1--1)]
@R    Texture0_id14 => Texturing.Texture0 [Stage: Pixel, Slot: (-1--1)]
@R    Sampler_id42 => Texturing.Sampler [Stage: Pixel, Slot: (-1--1)]
***************************
*****     Sources     *****
***************************
@S    AmbientOcclusionRawAOShader => 2eaa88bb683afe83e86f924be5bec590
@S    ImageEffectShader => 8064e30cc02e5eb4052f420259dbf05e
@S    SpriteBase => 5a7aa9dfd5b5c7613053f4f66c79ca0d
@S    ShaderBase => 4ecbcd2528b64a79eebe81a863892d8c
@S    ShaderBaseStream => b705b699a7385d39c7de52a8d13f3978
@S    Texturing => 91ef3011c1071c2e5d41cd3ee0418b18
@S    Camera => f5d1a113ef7a27319900e8cc2e11ae0d
***************************
*****     Stages      *****
***************************
@G    Vertex => 7bd8d32da3d2924bfe993fd32e966f57
@G    Pixel => 84b58dff82e91058e5722d53d9f7a8d9
***************************
*************************/
const static int SamplesCount_id79 = 13;
const static bool IsOrthographic_id80 = true;
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
cbuffer PerView 
{
    float NearClipPlane_id74 = 1.0f;
    float FarClipPlane_id75 = 100.0f;
    float2 ZProjection_id76;
    float2 ViewSize_id77;
    float AspectRatio_id78;
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
    float4 ProjInfo_id81;
    float4 ScreenInfo_id82;
    float ParamProjScale_id83 = 1;
    float ParamIntensity_id84 = 1;
    float ParamBias_id85 = 0.01f;
    float ParamRadius_id86 = 1;
    float ParamRadiusSquared_id87 = 1;
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
float3 reconstructCSPosition_id4(float2 S, float z)
{

    {
        float2 uv = S.xy / ScreenInfo_id82.xy;
        uv = uv * 2 - 1;
        return float3(uv * ProjInfo_id81.xy, z);
    }
}
float reconstructCSZ_id3(float depth)
{
    return ZProjection_id76.x + depth * ZProjection_id76.y;
}
float sampleAO_id6(inout PS_STREAMS streams, int2 screenPosition, float3 viewPosition, float3 viewNormal, float diskRadius, int i, float randomPatternRotationAngle)
{
    float alpha = 1 * (i + 0.5) * 0.675f / SamplesCount_id79;
    float angle = 1 * 43.9822971503f * alpha + randomPatternRotationAngle;
    float2 offset = float2(cos(angle), sin(angle));
    float ssRadius = alpha * diskRadius;
    float2 samplePos = streams.TexCoord_id62 + offset * ssRadius;
    int2 samplePosInt = saturate(samplePos) * ScreenInfo_id82.xy;
    float depth = Texture0_id14.Load(int3(samplePosInt, 0));
    float linearDepth = reconstructCSZ_id3(depth);
    float3 position = reconstructCSPosition_id4(samplePosInt + float2(0.5, 0.5), linearDepth);
    position.x *= -1;
    float3 v = position - viewPosition;
    v.z *= -1;
    float distSq = dot(v, v);
    float vn = dot(v, viewNormal);
    const float epsilon = 0.01;
    float f = max(ParamRadiusSquared_id87 - distSq, 0.0);
    return f * f * f * max((vn - ParamBias_id85) / (epsilon + distSq), 0.0);
}
float3 reconstructCSNormal_id5(float3 position)
{
    return normalize(cross(ddy(position), ddx(position)));
}
float4 Shading_id7(inout PS_STREAMS streams)
{
    float depth = Texture0_id14.SampleLevel(Sampler_id42, streams.TexCoord_id62, 0).x;
    float linearDepth = reconstructCSZ_id3(depth);
    int2 screenPosition = streams.TexCoord_id62.xy * ScreenInfo_id82.xy;
    float3 viewPosition = reconstructCSPosition_id4(screenPosition + float2(0.5, 0.5), linearDepth);
    viewPosition.x *= -1;
    float3 viewNormal = reconstructCSNormal_id5(viewPosition.xyz);
    viewNormal.xy *= -1;
    int linearDepthInt = (int)linearDepth;
    float randomPatternRotationAngle = ((15 * linearDepthInt + 3 * screenPosition.x ^ 2 * screenPosition.y + screenPosition.x * screenPosition.y) & 0x0000FFFF) * 10;
    float diskRadius;
    diskRadius = ParamProjScale_id83 / ProjInfo_id81.z;
    float sum = 0.0;

    for (int i = 0; i < SamplesCount_id79; i++)
    {
        sum += sampleAO_id6(streams, screenPosition, viewPosition, viewNormal, diskRadius, i, randomPatternRotationAngle);
    }
    float temp = ParamRadiusSquared_id87 * ParamRadius_id86;
    sum /= temp * temp;
    float A = max(0.0, 1.0 - sum * 5 * ParamIntensity_id84 / SamplesCount_id79);
    float nearPlaneFade = saturate(linearDepth * 2.0 - 0.5);
    A = lerp(1, A, nearPlaneFade);
    if (abs(ddx(linearDepth)) < 0.02)
    {
        A -= ddx(A) * ((screenPosition.x & 1) - 0.5);
    }
    if (abs(ddy(linearDepth)) < 0.02)
    {
        A -= ddy(A) * ((screenPosition.y & 1) - 0.5);
    }
    return float4(A, A, A, A);
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
