/**************************
***** Compiler Parameters *****
***************************
@P EffectName: FXAAShaderEffect
@P   - FXAAEffect.GreenAsLumaKey: 0
@P FXAAEffect.QualityKey: 23
***************************
****  ConstantBuffers  ****
***************************
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
@R    PerDraw => PerDraw [Stage: Vertex, Slot: (-1--1)]
@R    Globals => Globals [Stage: Pixel, Slot: (-1--1)]
@R    Texture0_id14 => Texturing.Texture0 [Stage: Pixel, Slot: (-1--1)]
@R    LinearSampler_id44 => Texturing.LinearSampler [Stage: Pixel, Slot: (-1--1)]
***************************
*****     Sources     *****
***************************
@S    FXAAShader => 5e4b2cff3ff2156e26f8b5dbbdd2bb95
@S    ImageEffectShader => 8064e30cc02e5eb4052f420259dbf05e
@S    SpriteBase => 5a7aa9dfd5b5c7613053f4f66c79ca0d
@S    ShaderBase => 4ecbcd2528b64a79eebe81a863892d8c
@S    ShaderBaseStream => b705b699a7385d39c7de52a8d13f3978
@S    Texturing => 91ef3011c1071c2e5d41cd3ee0418b18
***************************
*****     Stages      *****
***************************
@G    Vertex => 7bd8d32da3d2924bfe993fd32e966f57
@G    Pixel => 8596a1a8a4ce0ceb2b243c8fff9d64af
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
float FxaaLuma_id3(float4 rgba)
{
    return rgba.w;
}
float4 FxaaPixelShader_id4(float2 pos, float4 fxaaConsolePosPos, Texture2D tex, Texture2D fxaaConsole360TexExpBiasNegOne, Texture2D fxaaConsole360TexExpBiasNegTwo, float2 fxaaQualityRcpFrame, float4 fxaaConsoleRcpFrameOpt, float4 fxaaConsoleRcpFrameOpt2, float4 fxaaConsole360RcpFrameOpt2, float fxaaQualitySubpix, float fxaaQualityEdgeThreshold, float fxaaQualityEdgeThresholdMin, float fxaaConsoleEdgeSharpness, float fxaaConsoleEdgeThreshold, float fxaaConsoleEdgeThresholdMin, float4 fxaaConsole360ConstDir)
{
    float2 posM;
    posM.x = pos.x;
    posM.y = pos.y;
    float4 rgbyM = tex.SampleLevel(LinearSampler_id44, posM, 0.0);
    float lumaS = FxaaLuma_id3(tex.SampleLevel(LinearSampler_id44, posM, 0.0, int2(0, 1)));
    float lumaE = FxaaLuma_id3(tex.SampleLevel(LinearSampler_id44, posM, 0.0, int2(1, 0)));
    float lumaN = FxaaLuma_id3(tex.SampleLevel(LinearSampler_id44, posM, 0.0, int2(0, -1)));
    float lumaW = FxaaLuma_id3(tex.SampleLevel(LinearSampler_id44, posM, 0.0, int2(-1, 0)));
    float maxSM = max(lumaS, rgbyM.w);
    float minSM = min(lumaS, rgbyM.w);
    float maxESM = max(lumaE, maxSM);
    float minESM = min(lumaE, minSM);
    float maxWN = max(lumaN, lumaW);
    float minWN = min(lumaN, lumaW);
    float rangeMax = max(maxWN, maxESM);
    float rangeMin = min(minWN, minESM);
    float rangeMaxScaled = rangeMax * fxaaQualityEdgeThreshold;
    float range = rangeMax - rangeMin;
    float rangeMaxClamped = max(fxaaQualityEdgeThresholdMin, rangeMaxScaled);
    bool earlyExit = range < rangeMaxClamped;
    if (earlyExit)
        return rgbyM;
    float lumaNW = FxaaLuma_id3(tex.SampleLevel(LinearSampler_id44, posM, 0.0, int2(-1, -1)));
    float lumaSE = FxaaLuma_id3(tex.SampleLevel(LinearSampler_id44, posM, 0.0, int2(1, 1)));
    float lumaNE = FxaaLuma_id3(tex.SampleLevel(LinearSampler_id44, posM, 0.0, int2(1, -1)));
    float lumaSW = FxaaLuma_id3(tex.SampleLevel(LinearSampler_id44, posM, 0.0, int2(-1, 1)));
    float lumaNS = lumaN + lumaS;
    float lumaWE = lumaW + lumaE;
    float subpixRcpRange = 1.0 / range;
    float subpixNSWE = lumaNS + lumaWE;
    float edgeHorz1 = (-2.0 * rgbyM.w) + lumaNS;
    float edgeVert1 = (-2.0 * rgbyM.w) + lumaWE;
    float lumaNESE = lumaNE + lumaSE;
    float lumaNWNE = lumaNW + lumaNE;
    float edgeHorz2 = (-2.0 * lumaE) + lumaNESE;
    float edgeVert2 = (-2.0 * lumaN) + lumaNWNE;
    float lumaNWSW = lumaNW + lumaSW;
    float lumaSWSE = lumaSW + lumaSE;
    float edgeHorz4 = (abs(edgeHorz1) * 2.0) + abs(edgeHorz2);
    float edgeVert4 = (abs(edgeVert1) * 2.0) + abs(edgeVert2);
    float edgeHorz3 = (-2.0 * lumaW) + lumaNWSW;
    float edgeVert3 = (-2.0 * lumaS) + lumaSWSE;
    float edgeHorz = abs(edgeHorz3) + edgeHorz4;
    float edgeVert = abs(edgeVert3) + edgeVert4;
    float subpixNWSWNESE = lumaNWSW + lumaNESE;
    float lengthSign = fxaaQualityRcpFrame.x;
    bool horzSpan = edgeHorz >= edgeVert;
    float subpixA = subpixNSWE * 2.0 + subpixNWSWNESE;
    if (!horzSpan)
        lumaN = lumaW;
    if (!horzSpan)
        lumaS = lumaE;
    if (horzSpan)
        lengthSign = fxaaQualityRcpFrame.y;
    float subpixB = (subpixA * (1.0 / 12.0)) - rgbyM.w;
    float gradientN = lumaN - rgbyM.w;
    float gradientS = lumaS - rgbyM.w;
    float lumaNN = lumaN + rgbyM.w;
    float lumaSS = lumaS + rgbyM.w;
    bool pairN = abs(gradientN) >= abs(gradientS);
    float gradient = max(abs(gradientN), abs(gradientS));
    if (pairN)
        lengthSign = -lengthSign;
    float subpixC = saturate(abs(subpixB) * subpixRcpRange);
    float2 posB;
    posB.x = posM.x;
    posB.y = posM.y;
    float2 offNP;
    offNP.x = (!horzSpan) ? 0.0 : fxaaQualityRcpFrame.x;
    offNP.y = (horzSpan) ? 0.0 : fxaaQualityRcpFrame.y;
    if (!horzSpan)
        posB.x += lengthSign * 0.5;
    if (horzSpan)
        posB.y += lengthSign * 0.5;
    float2 posN;
    posN.x = posB.x - offNP.x * 1.0;
    posN.y = posB.y - offNP.y * 1.0;
    float2 posP;
    posP.x = posB.x + offNP.x * 1.0;
    posP.y = posB.y + offNP.y * 1.0;
    float subpixD = ((-2.0) * subpixC) + 3.0;
    float lumaEndN = FxaaLuma_id3(tex.SampleLevel(LinearSampler_id44, posN, 0.0));
    float subpixE = subpixC * subpixC;
    float lumaEndP = FxaaLuma_id3(tex.SampleLevel(LinearSampler_id44, posP, 0.0));
    if (!pairN)
        lumaNN = lumaSS;
    float gradientScaled = gradient * 1.0 / 4.0;
    float lumaMM = rgbyM.w - lumaNN * 0.5;
    float subpixF = subpixD * subpixE;
    bool lumaMLTZero = lumaMM < 0.0;
    lumaEndN -= lumaNN * 0.5;
    lumaEndP -= lumaNN * 0.5;
    bool doneN = abs(lumaEndN) >= gradientScaled;
    bool doneP = abs(lumaEndP) >= gradientScaled;
    if (!doneN)
        posN.x -= offNP.x * 1.5;
    if (!doneN)
        posN.y -= offNP.y * 1.5;
    bool doneNP = (!doneN) || (!doneP);
    if (!doneP)
        posP.x += offNP.x * 1.5;
    if (!doneP)
        posP.y += offNP.y * 1.5;
    if (doneNP)
    {
        if (!doneN)
            lumaEndN = FxaaLuma_id3(tex.SampleLevel(LinearSampler_id44, posN.xy, 0.0));
        if (!doneP)
            lumaEndP = FxaaLuma_id3(tex.SampleLevel(LinearSampler_id44, posP.xy, 0.0));
        if (!doneN)
            lumaEndN = lumaEndN - lumaNN * 0.5;
        if (!doneP)
            lumaEndP = lumaEndP - lumaNN * 0.5;
        doneN = abs(lumaEndN) >= gradientScaled;
        doneP = abs(lumaEndP) >= gradientScaled;
        if (!doneN)
            posN.x -= offNP.x * 2.0;
        if (!doneN)
            posN.y -= offNP.y * 2.0;
        doneNP = (!doneN) || (!doneP);
        if (!doneP)
            posP.x += offNP.x * 2.0;
        if (!doneP)
            posP.y += offNP.y * 2.0;
        if (doneNP)
        {
            if (!doneN)
                lumaEndN = FxaaLuma_id3(tex.SampleLevel(LinearSampler_id44, posN.xy, 0.0));
            if (!doneP)
                lumaEndP = FxaaLuma_id3(tex.SampleLevel(LinearSampler_id44, posP.xy, 0.0));
            if (!doneN)
                lumaEndN = lumaEndN - lumaNN * 0.5;
            if (!doneP)
                lumaEndP = lumaEndP - lumaNN * 0.5;
            doneN = abs(lumaEndN) >= gradientScaled;
            doneP = abs(lumaEndP) >= gradientScaled;
            if (!doneN)
                posN.x -= offNP.x * 2.0;
            if (!doneN)
                posN.y -= offNP.y * 2.0;
            doneNP = (!doneN) || (!doneP);
            if (!doneP)
                posP.x += offNP.x * 2.0;
            if (!doneP)
                posP.y += offNP.y * 2.0;
            if (doneNP)
            {
                if (!doneN)
                    lumaEndN = FxaaLuma_id3(tex.SampleLevel(LinearSampler_id44, posN.xy, 0.0));
                if (!doneP)
                    lumaEndP = FxaaLuma_id3(tex.SampleLevel(LinearSampler_id44, posP.xy, 0.0));
                if (!doneN)
                    lumaEndN = lumaEndN - lumaNN * 0.5;
                if (!doneP)
                    lumaEndP = lumaEndP - lumaNN * 0.5;
                doneN = abs(lumaEndN) >= gradientScaled;
                doneP = abs(lumaEndP) >= gradientScaled;
                if (!doneN)
                    posN.x -= offNP.x * 2.0;
                if (!doneN)
                    posN.y -= offNP.y * 2.0;
                doneNP = (!doneN) || (!doneP);
                if (!doneP)
                    posP.x += offNP.x * 2.0;
                if (!doneP)
                    posP.y += offNP.y * 2.0;
                if (doneNP)
                {
                    if (!doneN)
                        lumaEndN = FxaaLuma_id3(tex.SampleLevel(LinearSampler_id44, posN.xy, 0.0));
                    if (!doneP)
                        lumaEndP = FxaaLuma_id3(tex.SampleLevel(LinearSampler_id44, posP.xy, 0.0));
                    if (!doneN)
                        lumaEndN = lumaEndN - lumaNN * 0.5;
                    if (!doneP)
                        lumaEndP = lumaEndP - lumaNN * 0.5;
                    doneN = abs(lumaEndN) >= gradientScaled;
                    doneP = abs(lumaEndP) >= gradientScaled;
                    if (!doneN)
                        posN.x -= offNP.x * 8.0;
                    if (!doneN)
                        posN.y -= offNP.y * 8.0;
                    doneNP = (!doneN) || (!doneP);
                    if (!doneP)
                        posP.x += offNP.x * 8.0;
                    if (!doneP)
                        posP.y += offNP.y * 8.0;
                }
            }
        }
    }
    float dstN = posM.x - posN.x;
    float dstP = posP.x - posM.x;
    if (!horzSpan)
        dstN = posM.y - posN.y;
    if (!horzSpan)
        dstP = posP.y - posM.y;
    bool goodSpanN = (lumaEndN < 0.0) != lumaMLTZero;
    float spanLength = (dstP + dstN);
    bool goodSpanP = (lumaEndP < 0.0) != lumaMLTZero;
    float spanLengthRcp = 1.0 / spanLength;
    bool directionN = dstN < dstP;
    float dst = min(dstN, dstP);
    bool goodSpan = directionN ? goodSpanN : goodSpanP;
    float subpixG = subpixF * subpixF;
    float pixelOffset = (dst * (-spanLengthRcp)) + 0.5;
    float subpixH = subpixG * fxaaQualitySubpix;
    float pixelOffsetGood = goodSpan ? pixelOffset : 0.0;
    float pixelOffsetSubpix = max(pixelOffsetGood, subpixH);
    if (!horzSpan)
        posM.x += pixelOffsetSubpix * lengthSign;
    if (horzSpan)
        posM.y += pixelOffsetSubpix * lengthSign;
    return float4(tex.SampleLevel(LinearSampler_id44, posM, 0.0).xyz, rgbyM.w);
}
float4 Shading_id5(inout PS_STREAMS streams)
{
    float2 texCoord = streams.TexCoord_id62;
    float2 screenPixelRatio = Texture0TexelSize_id15;
    return FxaaPixelShader_id4(texCoord, 0, Texture0_id14, Texture0_id14, Texture0_id14, screenPixelRatio, 0, 0, 0, 0.75, 0.063, 0.0312, 8, 0.125, 0.05, 0);
}
PS_OUTPUT PSMain(PS_INPUT __input__)
{
    PS_STREAMS streams = (PS_STREAMS)0;
    streams.TexCoord_id62 = __input__.TexCoord_id62;
    streams.ColorTarget_id2 = Shading_id5(streams);
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
