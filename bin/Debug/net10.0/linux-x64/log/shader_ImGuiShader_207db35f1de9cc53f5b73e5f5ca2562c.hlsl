/**************************
***** Compiler Parameters *****
***************************
@P EffectName: ImGuiShader
***************************
****  ConstantBuffers  ****
***************************
cbuffer Globals [Size: 64]
@C    proj_id14 => ImGuiShader.proj
***************************
******  Resources    ******
***************************
@R    Globals => Globals [Stage: Vertex, Slot: (-1--1)]
@R    tex_id15 => ImGuiShader.tex [Stage: Pixel, Slot: (-1--1)]
@R    TexSampler_id16 => ImGuiShader.TexSampler [Stage: Pixel, Slot: (-1--1)]
***************************
*****     Sources     *****
***************************
@S    ImGuiShader => d604175e1b709e4f3ab9c60380014e84
@S    ShaderBase => 4ecbcd2528b64a79eebe81a863892d8c
@S    ShaderBaseStream => b705b699a7385d39c7de52a8d13f3978
***************************
*****     Stages      *****
***************************
@G    Vertex => 38223d56d3451f8c12dafddc9701b402
@G    Pixel => 0951cf715c43d48154da7be5943430a1
***************************
*************************/
struct PS_STREAMS 
{
    float4 col_id19;
    float2 uv_id18;
    float4 ColorTarget_id2;
};
struct PS_OUTPUT 
{
    float4 ColorTarget_id2 : SV_Target0;
};
struct PS_INPUT 
{
    float4 ShadingPosition_id0 : SV_Position;
    float4 col_id19 : COLOR;
    float2 uv_id18 : TEXCOORD0;
};
struct VS_STREAMS 
{
    float2 pos_id17;
    float4 col_id19;
    float2 uv_id18;
    float4 ShadingPosition_id0;
};
struct VS_OUTPUT 
{
    float4 ShadingPosition_id0 : SV_Position;
    float4 col_id19 : COLOR;
    float2 uv_id18 : TEXCOORD0;
};
struct VS_INPUT 
{
    float2 pos_id17 : POSITION;
    float4 col_id19 : COLOR;
    float2 uv_id18 : TEXCOORD0;
};
cbuffer Globals 
{
    matrix proj_id14;
};
Texture2D tex_id15;
SamplerState TexSampler_id16 
{
    Filter = ANISOTROPIC;
    AddressU = Wrap;
    AddressV = Wrap;
    MaxAnisotropy = 16;
};
PS_OUTPUT PSMain(PS_INPUT __input__)
{
    PS_STREAMS streams = (PS_STREAMS)0;
    streams.col_id19 = __input__.col_id19;
    streams.uv_id18 = __input__.uv_id18;
    streams.ColorTarget_id2 = streams.col_id19 * tex_id15.Sample(TexSampler_id16, streams.uv_id18);
    PS_OUTPUT __output__ = (PS_OUTPUT)0;
    __output__.ColorTarget_id2 = streams.ColorTarget_id2;
    return __output__;
}
VS_OUTPUT VSMain(VS_INPUT __input__)
{
    VS_STREAMS streams = (VS_STREAMS)0;
    streams.pos_id17 = __input__.pos_id17;
    streams.col_id19 = __input__.col_id19;
    streams.uv_id18 = __input__.uv_id18;
    streams.ShadingPosition_id0 = mul(proj_id14, float4(streams.pos_id17, 0.0, 1.0f)) + float4(-1.0f, 1.0f, 0.0f, 0.0f);
    VS_OUTPUT __output__ = (VS_OUTPUT)0;
    __output__.ShadingPosition_id0 = streams.ShadingPosition_id0;
    __output__.col_id19 = streams.col_id19;
    __output__.uv_id18 = streams.uv_id18;
    return __output__;
}
