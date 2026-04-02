/**************************
***** Compiler Parameters *****
***************************
@P EffectName: StrideForwardShadingEffect
@P   - Material.PixelStageSurfaceShaders: mixin MaterialSurfaceArray [{layers = [mixin MaterialSurfaceDiffuse [{diffuseMap = ComputeColorTextureScaledOffsetDynamicSampler<Material.DiffuseMap,TEXCOORD0,Material.Sampler.i0,rgba,Material.TextureScale,Material.TextureOffset>}], mixin MaterialSurfaceLightingAndShading [{surfaces = [MaterialSurfaceShadingDiffuseLambert<false>]}]]}]
@P Material.PixelStageStreamInitializer: mixin MaterialStream, MaterialPixelShadingStream
@P Lighting.DirectLightGroups: mixin LightDirectionalGroup<8>, mixin LightDirectionalGroup<1>, ShadowMapReceiverDirectional<1,1,true,true,false,false>, ShadowMapFilterPcf<PerView.Lighting,5>, LightClusteredPointGroup, LightClusteredSpotGroup
@P Lighting.EnvironmentLights: LightSimpleAmbient
@P StrideEffectBase.RenderTargetExtensions: mixin [{ShadingColor1 = GBufferOutputNormals}, {ShadingColor2 = GBufferOutputSpecularColorRoughness}]
***************************
****  ConstantBuffers  ****
***************************
cbuffer PerDraw [Size: 416]
@C    World_id33 => Transformation.World
@C    WorldInverse_id34 => Transformation.WorldInverse
@C    WorldInverseTranspose_id35 => Transformation.WorldInverseTranspose
@C    WorldView_id36 => Transformation.WorldView
@C    WorldViewInverse_id37 => Transformation.WorldViewInverse
@C    WorldViewProjection_id38 => Transformation.WorldViewProjection
@C    WorldScale_id39 => Transformation.WorldScale
@C    EyeMS_id40 => Transformation.EyeMS
cbuffer PerMaterial [Size: 16]
@C    scale_id189 => Material.TextureScale
@C    offset_id190 => Material.TextureOffset
cbuffer PerView [Size: 1040]
@C    View_id26 => Transformation.View
@C    ViewInverse_id27 => Transformation.ViewInverse
@C    Projection_id28 => Transformation.Projection
@C    ProjectionInverse_id29 => Transformation.ProjectionInverse
@C    ViewProjection_id30 => Transformation.ViewProjection
@C    ProjScreenRay_id31 => Transformation.ProjScreenRay
@C    Eye_id32 => Transformation.Eye
@C    NearClipPlane_id171 => Camera.NearClipPlane
@C    FarClipPlane_id172 => Camera.FarClipPlane
@C    ZProjection_id173 => Camera.ZProjection
@C    ViewSize_id174 => Camera.ViewSize
@C    AspectRatio_id175 => Camera.AspectRatio
@C    _padding_PerView_Default => _padding_PerView_Default
@C    LightCount_id86 => DirectLightGroupPerView.LightCount.directLightGroups[0]
@C    Lights_id88 => LightDirectionalGroup.Lights.directLightGroups[0]
@C    LightCount_id89 => DirectLightGroupPerView.LightCount.directLightGroups[1]
@C    Lights_id91 => LightDirectionalGroup.Lights.directLightGroups[1]
@C    ShadowMapTextureSize_id93 => ShadowMap.TextureSize.directLightGroups[1]
@C    ShadowMapTextureTexelSize_id94 => ShadowMap.TextureTexelSize.directLightGroups[1]
@C    WorldToShadowCascadeUV_id156 => ShadowMapReceiverBase.WorldToShadowCascadeUV.directLightGroups[1]
@C    InverseWorldToShadowCascadeUV_id157 => ShadowMapReceiverBase.InverseWorldToShadowCascadeUV.directLightGroups[1]
@C    ViewMatrices_id158 => ShadowMapReceiverBase.ViewMatrices.directLightGroups[1]
@C    DepthRanges_id159 => ShadowMapReceiverBase.DepthRanges.directLightGroups[1]
@C    DepthBiases_id160 => ShadowMapReceiverBase.DepthBiases.directLightGroups[1]
@C    OffsetScales_id161 => ShadowMapReceiverBase.OffsetScales.directLightGroups[1]
@C    CascadeDepthSplits_id168 => ShadowMapReceiverDirectional.CascadeDepthSplits.directLightGroups[1]
@C    ClusterDepthScale_id180 => LightClustered.ClusterDepthScale
@C    ClusterDepthBias_id181 => LightClustered.ClusterDepthBias
@C    ClusterStride_id182 => LightClustered.ClusterStride
@C    AmbientLight_id185 => LightSimpleAmbient.AmbientLight.environmentLights[0]
@C    _padding_PerView_Lighting => _padding_PerView_Lighting
***************************
******  Resources    ******
***************************
@R    PerDraw => PerDraw [Stage: Vertex, Slot: (-1--1)]
@R    PerMaterial => PerMaterial [Stage: Pixel, Slot: (-1--1)]
@R    PerView => PerView [Stage: Vertex, Slot: (-1--1)]
@R    LinearClampCompareLessEqualSampler_id127 => Texturing.LinearClampCompareLessEqualSampler [Stage: Pixel, Slot: (-1--1)]
@R    Texture_id186 => Material.DiffuseMap [Stage: Pixel, Slot: (-1--1)]
@R    Texture_id186 => Material.DiffuseMap [Stage: None, Slot: (-1--1)]
@R    Sampler_id187 => Material.Sampler.i0 [Stage: Pixel, Slot: (-1--1)]
@R    Sampler_id187 => Material.Sampler.i0 [Stage: None, Slot: (-1--1)]
@R    ShadowMapTexture_id92 => ShadowMap.ShadowMapTexture.directLightGroups[1] [Stage: Pixel, Slot: (-1--1)]
@R    ShadowMapTexture_id92 => ShadowMap.ShadowMapTexture.directLightGroups[1] [Stage: None, Slot: (-1--1)]
@R    LightClusters_id178 => LightClustered.LightClusters [Stage: Pixel, Slot: (-1--1)]
@R    LightClusters_id178 => LightClustered.LightClusters [Stage: None, Slot: (-1--1)]
@R    LightIndices_id179 => LightClustered.LightIndices [Stage: Pixel, Slot: (-1--1)]
@R    LightIndices_id179 => LightClustered.LightIndices [Stage: None, Slot: (-1--1)]
@R    PointLights_id183 => LightClusteredPointGroup.PointLights [Stage: Pixel, Slot: (-1--1)]
@R    PointLights_id183 => LightClusteredPointGroup.PointLights [Stage: None, Slot: (-1--1)]
@R    SpotLights_id184 => LightClusteredSpotGroup.SpotLights [Stage: Pixel, Slot: (-1--1)]
@R    SpotLights_id184 => LightClusteredSpotGroup.SpotLights [Stage: None, Slot: (-1--1)]
***************************
*****     Sources     *****
***************************
@S    ShaderBase => 4ecbcd2528b64a79eebe81a863892d8c
@S    ShaderBaseStream => b705b699a7385d39c7de52a8d13f3978
@S    ShadingBase => b0f11f286acc22f5586417a8311cb632
@S    ComputeColor => c875a0e093379dd74cd9a5a73aca830f
@S    TransformationBase => 21981c533d88209fdcf07f258ddf01c2
@S    NormalStream => ea68512133899a045766d21afb59733a
@S    TransformationWAndVP => ea6628bcd79c8f0060c612aa9fc4819b
@S    PositionStream4 => 992b49e1b4dd13d8ef84a05830d70229
@S    PositionHStream4 => fc5e64dda1ac2bb4a31d58404dced340
@S    Transformation => 7c995c14d4da978d7dca233f15f1e7c0
@S    NormalFromMesh => 85b71ad3ed9065c0803bfd77d09fb685
@S    NormalBase => b2b31addde884722f942622026837c39
@S    NormalUpdate => 6fd3c9b8fa943d9951400645fe40502e
@S    MaterialSurfacePixelStageCompositor => ea7a1f076313986d24488e1073848b3d
@S    PositionStream => f677bb6cb046d4f5f594cc6d8a703259
@S    MaterialPixelShadingStream => ad7a8ed4b25fbb8ce36df356d447fda2
@S    MaterialPixelStream => 3e33eeb836e260b00905946a5267b87e
@S    MaterialStream => 6f0bb68dde7beef4b38d4867da2f4d0a
@S    IStreamInitializer => acbd5b5e1debd1516c61049f39f04fdc
@S    LightStream => db97b763bbf12e53eaf0875b9db31366
@S    DirectLightGroupArray => c8057c9126020ee1c62e23facba5036d
@S    DirectLightGroup => 501cdd2e28afd5e234c08907f26aa5df
@S    ShadowGroup => bd0b502fd18b37aedabdcefe14deef34
@S    ShadowStream => e75e57a19e12d81083677a851803d69a
@S    TextureProjectionGroup => 6f741ec2cbd4e9796145b35f8fc18c45
@S    EnvironmentLightArray => 2dfda49afe728922257f9b497bc6b904
@S    EnvironmentLight => 67198c913f8c86fad248a6726699dd4d
@S    IMaterialSurface => 4439d1801d274f7bab04ddbc33b85f40
@S    LightDirectionalGroup => 4b30b0154f396d93fd98b65dbdd2fe7f
@S    DirectLightGroupPerView => dba9b0c7c0e05b5469cf8940cc91d69c
@S    LightDirectional => 0e45e8c12e5e36d4bb0df5d5b2739bb1
@S    ShadowMapReceiverDirectional => b81b909e86a10160b58697624ea36c97
@S    ShadowMapReceiverBase => ecd55bb03a327260ea2ea0fddfaff4b5
@S    ShadowMapGroup => 6cd78766a20ef1d6d95cbbe5eec432b6
@S    ShadowMapCommon => 53c84311eb33d99ade37ecb188ae5785
@S    ShadowMapFilterBase => e42061be9b71cb375d3566138e7d8b8d
@S    Texturing => 91ef3011c1071c2e5d41cd3ee0418b18
@S    Math => 9787fbb9c5fc970c8a2b04e18943e1bd
@S    ShadowMapFilterPcf => 1d20b8b1db946ed23a9d876bb8750e3c
@S    LightClusteredPointGroup => 843aea2daf41f8d9d51dc3f2e6983510
@S    LightClustered => 33caec9577a483e38f00249390b3c928
@S    ScreenPositionBase => 2dae8708ab57eb7bfe3be30e463a947f
@S    Camera => f5d1a113ef7a27319900e8cc2e11ae0d
@S    LightPoint => 294f81466d0cada846a599c119c5fb47
@S    LightUtil => 21ef9aac4d8713802ffffd7b6a054610
@S    LightClusteredSpotGroup => 13a781ebabc5e5b807beef263ac92450
@S    LightSpot => 0c28763d5549e9d53c818622d5b67087
@S    SpotLightDataInternalShader => ebbf2821da7249148876264c5dcf660d
@S    LightSpotAttenuationDefault => e20860e5b3b88061c074f0438d523fa5
@S    LightSimpleAmbient => 1be2895cd80f1f25f0225844e19b3398
@S    MaterialSurfaceArray => 8cf3cc54fcd9949ce74f2e4caaa0d6d9
@S    MaterialSurfaceDiffuse => 959d3b90076611b0252419cb02190f99
@S    IMaterialSurfacePixel => b6013c701b8fca52da0c65754bb0a9ca
@S    ComputeColorTextureScaledOffsetDynamicSampler => da9d92ccde749e432319e9a52418d83c
@S    DynamicTexture => 67260522628b9542cfe8b510d057848c
@S    DynamicSampler => bbeefb706936f29da845bc860ab45203
@S    DynamicTextureStream => 3f37007d23787490ce6998952b052d4a
@S    MaterialSurfaceLightingAndShading => 632084a1d51a33d288c188803e2d5afc
@S    IMaterialSurfaceShading => 1c45b326cd8616074872dd56496a9b5a
@S    MaterialSurfaceShadingDiffuseLambert => 8e2e1baa4a7bcb427f3ca539f81dd15c
@S    GBufferOutputNormals => 3e4e9266b2241214d3854561b81ccfc5
@S    NormalPack => af1a18518fb63b6295c012c18fe9f9c0
@S    GBufferOutputSpecularColorRoughness => bffa7efe5b33b6017b4ec87d2c142a79
@S    Utilities => d8e15010fb2006b8edf6bdc952dd31f0
***************************
*****     Stages      *****
***************************
@G    Vertex => b9e6e64b85979fa5a87a064cb5193f2c
@G    Pixel => 31b23a399cbbacca53cc09617cc83a84
***************************
*************************/
const static int TMaxLightCount_id87 = 8;
const static int TMaxLightCount_id90 = 1;
const static int TCascadeCountBase_id154 = 1;
const static int TLightCountBase_id155 = 1;
const static int TCascadeCount_id162 = 1;
const static int TLightCount_id163 = 1;
const static bool TBlendCascades_id164 = true;
const static bool TDepthRangeAuto_id165 = true;
const static bool TCascadeDebug_id166 = false;
const static bool TComputeTransmittance_id167 = false;
const static int TFilterSize_id169 = 5;
static const float PI_id193 = 3.14159265358979323846;
const static bool TIsEnergyConservative_id194 = false;
struct PS_STREAMS 
{
    float4 ScreenPosition_id170;
    float3 normalWS_id20;
    float4 PositionWS_id23;
    float2 TexCoord_id143;
    float DepthVS_id24;
    float4 ShadingPosition_id0;
    bool IsFrontFace_id1;
    float3 meshNormalWS_id18;
    float3 viewWS_id69;
    float3 shadingColor_id74;
    float matBlend_id41;
    float3 matNormal_id52;
    float4 matColorBase_id53;
    float4 matDiffuse_id54;
    float3 matDiffuseVisible_id70;
    float3 matSpecular_id56;
    float3 matSpecularVisible_id72;
    float matSpecularIntensity_id57;
    float matGlossiness_id55;
    float alphaRoughness_id71;
    float matAmbientOcclusion_id58;
    float matAmbientOcclusionDirectLightingFactor_id59;
    float matCavity_id60;
    float matCavityDiffuse_id61;
    float matCavitySpecular_id62;
    float4 matEmissive_id63;
    float matEmissiveIntensity_id64;
    float matScatteringStrength_id65;
    float2 matDiffuseSpecularAlphaBlend_id66;
    float3 matAlphaBlendColor_id67;
    float matAlphaDiscard_id68;
    float shadingColorAlpha_id75;
    float3 lightPositionWS_id42;
    float3 lightDirectionWS_id43;
    float3 lightColor_id44;
    float3 lightColorNdotL_id45;
    float3 lightSpecularColorNdotL_id46;
    float lightAttenuation_id47;
    float3 envLightDiffuseColor_id48;
    float3 envLightSpecularColor_id49;
    float lightDirectAmbientOcclusion_id51;
    float NdotL_id50;
    float NdotV_id73;
    float thicknessWS_id85;
    float3 shadowColor_id84;
    float3 H_id76;
    float NdotH_id77;
    float LdotH_id78;
    float VdotH_id79;
    uint2 lightData_id176;
    int lightIndex_id177;
    float4 ColorTarget_id2;
    float4 ColorTarget1_id3;
    float4 ColorTarget2_id4;
};
struct PS_OUTPUT 
{
    float4 ColorTarget_id2 : SV_Target0;
    float4 ColorTarget1_id3 : SV_Target1;
    float4 ColorTarget2_id4 : SV_Target2;
};
struct PS_INPUT 
{
    float4 PositionWS_id23 : POSITION_WS;
    float4 ShadingPosition_id0 : SV_Position;
    float DepthVS_id24 : DEPTH_VS;
    float3 normalWS_id20 : NORMALWS;
    float4 ScreenPosition_id170 : SCREENPOSITION_ID170_SEM;
    float2 TexCoord_id143 : TEXCOORD0;
    bool IsFrontFace_id1 : SV_IsFrontFace;
};
struct VS_STREAMS 
{
    float4 Position_id22;
    float3 meshNormal_id17;
    float2 TexCoord_id143;
    float4 PositionH_id25;
    float3 meshNormalWS_id18;
    float4 PositionWS_id23;
    float4 ShadingPosition_id0;
    float DepthVS_id24;
    float3 normalWS_id20;
    float4 ScreenPosition_id170;
};
struct VS_OUTPUT 
{
    float4 PositionWS_id23 : POSITION_WS;
    float4 ShadingPosition_id0 : SV_Position;
    float DepthVS_id24 : DEPTH_VS;
    float3 normalWS_id20 : NORMALWS;
    float4 ScreenPosition_id170 : SCREENPOSITION_ID170_SEM;
    float2 TexCoord_id143 : TEXCOORD0;
};
struct VS_INPUT 
{
    float4 Position_id22 : POSITION;
    float3 meshNormal_id17 : NORMAL;
    float2 TexCoord_id143 : TEXCOORD0;
};
typedef uint Half2;
typedef uint2 Half4;
struct DirectionalLightData 
{
    float3 DirectionWS;
    float3 Color;
};
struct PointLightData 
{
    float3 PositionWS;
    float InvSquareRadius;
    float3 Color;
};
struct PointLightDataInternal 
{
    float3 PositionWS;
    float InvSquareRadius;
    float3 Color;
};
struct SpotLightDataInternal 
{
    float3 PositionWS;
    float3 DirectionWS;
    float3 AngleOffsetAndInvSquareRadius;
    float3 Color;
};
struct SpotLightData 
{
    float3 PositionWS;
    float3 DirectionWS;
    float3 AngleOffsetAndInvSquareRadius;
    float3 Color;
};
cbuffer PerDraw 
{
    float4x4 World_id33;
    float4x4 WorldInverse_id34;
    float4x4 WorldInverseTranspose_id35;
    float4x4 WorldView_id36;
    float4x4 WorldViewInverse_id37;
    float4x4 WorldViewProjection_id38;
    float3 WorldScale_id39;
    float4 EyeMS_id40;
};
cbuffer PerMaterial 
{
    float2 scale_id189;
    float2 offset_id190;
};
cbuffer PerView 
{
    float4x4 View_id26;
    float4x4 ViewInverse_id27;
    float4x4 Projection_id28;
    float4x4 ProjectionInverse_id29;
    float4x4 ViewProjection_id30;
    float2 ProjScreenRay_id31;
    float4 Eye_id32;
    float NearClipPlane_id171 = 1.0f;
    float FarClipPlane_id172 = 100.0f;
    float2 ZProjection_id173;
    float2 ViewSize_id174;
    float AspectRatio_id175;
    float4 _padding_PerView_Default;
    int LightCount_id86;
    DirectionalLightData Lights_id88[TMaxLightCount_id87];
    int LightCount_id89;
    DirectionalLightData Lights_id91[TMaxLightCount_id90];
    float2 ShadowMapTextureSize_id93;
    float2 ShadowMapTextureTexelSize_id94;
    float4x4 WorldToShadowCascadeUV_id156[TCascadeCountBase_id154 * TLightCountBase_id155];
    float4x4 InverseWorldToShadowCascadeUV_id157[TCascadeCountBase_id154 * TLightCountBase_id155];
    float4x4 ViewMatrices_id158[TCascadeCountBase_id154 * TLightCountBase_id155];
    float2 DepthRanges_id159[TCascadeCountBase_id154 * TLightCountBase_id155];
    float DepthBiases_id160[TLightCountBase_id155];
    float OffsetScales_id161[TLightCountBase_id155];
    float CascadeDepthSplits_id168[TCascadeCount_id162 * TLightCount_id163];
    float ClusterDepthScale_id180;
    float ClusterDepthBias_id181;
    float2 ClusterStride_id182;
    float3 AmbientLight_id185;
    float4 _padding_PerView_Lighting;
};
cbuffer Globals 
{
    float2 Texture0TexelSize_id96;
    float2 Texture1TexelSize_id98;
    float2 Texture2TexelSize_id100;
    float2 Texture3TexelSize_id102;
    float2 Texture4TexelSize_id104;
    float2 Texture5TexelSize_id106;
    float2 Texture6TexelSize_id108;
    float2 Texture7TexelSize_id110;
    float2 Texture8TexelSize_id112;
    float2 Texture9TexelSize_id114;
};
Texture2D Texture0_id95;
Texture2D Texture1_id97;
Texture2D Texture2_id99;
Texture2D Texture3_id101;
Texture2D Texture4_id103;
Texture2D Texture5_id105;
Texture2D Texture6_id107;
Texture2D Texture7_id109;
Texture2D Texture8_id111;
Texture2D Texture9_id113;
TextureCube TextureCube0_id115;
TextureCube TextureCube1_id116;
TextureCube TextureCube2_id117;
TextureCube TextureCube3_id118;
Texture3D Texture3D0_id119;
Texture3D Texture3D1_id120;
Texture3D Texture3D2_id121;
Texture3D Texture3D3_id122;
SamplerState Sampler_id123;
SamplerState PointSampler_id124 
{
    Filter = MIN_MAG_MIP_POINT;
};
SamplerState LinearSampler_id125 
{
    Filter = MIN_MAG_MIP_LINEAR;
};
SamplerState LinearBorderSampler_id126 
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Border;
    AddressV = Border;
};
SamplerComparisonState LinearClampCompareLessEqualSampler_id127 
{
    Filter = COMPARISON_MIN_MAG_LINEAR_MIP_POINT;
    AddressU = Clamp;
    AddressV = Clamp;
    ComparisonFunc = LessEqual;
};
SamplerState AnisotropicSampler_id128 
{
    Filter = ANISOTROPIC;
};
SamplerState AnisotropicRepeatSampler_id129 
{
    Filter = ANISOTROPIC;
    AddressU = Wrap;
    AddressV = Wrap;
    MaxAnisotropy = 16;
};
SamplerState PointRepeatSampler_id130 
{
    Filter = MIN_MAG_MIP_POINT;
    AddressU = Wrap;
    AddressV = Wrap;
};
SamplerState LinearRepeatSampler_id131 
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
};
SamplerState RepeatSampler_id132 
{
    AddressU = Wrap;
    AddressV = Wrap;
};
SamplerState Sampler0_id133;
SamplerState Sampler1_id134;
SamplerState Sampler2_id135;
SamplerState Sampler3_id136;
SamplerState Sampler4_id137;
SamplerState Sampler5_id138;
SamplerState Sampler6_id139;
SamplerState Sampler7_id140;
SamplerState Sampler8_id141;
SamplerState Sampler9_id142;
Texture2D Texture_id186;
SamplerState Sampler_id187;
Texture2D ShadowMapTexture_id92;
Texture3D<uint2> LightClusters_id178;
Buffer<uint> LightIndices_id179;
Buffer<float4> PointLights_id183;
Buffer<float4> SpotLights_id184;
float4 Project_id68(float4 vec, float4x4 mat)
{
    float4 vecProjected = mul(vec, mat);
    vecProjected.xyz /= vecProjected.w;
    return vecProjected;
}
float SmoothDistanceAttenuation_id101(float squaredDistance, float lightInvSquareRadius)
{
    float factor = squaredDistance * lightInvSquareRadius;
    float smoothFactor = saturate(1.0f - factor * factor);
    return smoothFactor * smoothFactor;
}
float SmoothDistanceAttenuation_id90(float squaredDistance, float lightInvSquareRadius)
{
    float factor = squaredDistance * lightInvSquareRadius;
    float smoothFactor = saturate(1.0f - factor * factor);
    return smoothFactor * smoothFactor;
}
float Get3x3FilterKernel_id78(float2 base_uv, float2 st, out float3 kernel[4])
{
    float2 uvW0 = (3 - 2 * st);
    float2 uvW1 = (1 + 2 * st);
    float2 uv0 = (2 - st) / uvW0 - 1;
    float2 uv1 = st / uvW1 + 1;
    kernel[0] = float3(base_uv + uv0 * ShadowMapTextureTexelSize_id94, uvW0.x * uvW0.y);
    kernel[1] = float3(base_uv + float2(uv1.x, uv0.y) * ShadowMapTextureTexelSize_id94, uvW1.x * uvW0.y);
    kernel[2] = float3(base_uv + float2(uv0.x, uv1.y) * ShadowMapTextureTexelSize_id94, uvW0.x * uvW1.y);
    kernel[3] = float3(base_uv + uv1 * ShadowMapTextureTexelSize_id94, uvW1.x * uvW1.y);
    return 16.0;
}
float Get5x5FilterKernel_id77(float2 base_uv, float2 st, out float3 kernel[9])
{
    float2 uvW0 = (4 - 3 * st);
    float2 uvW1 = 7;
    float2 uvW2 = (1 + 3 * st);
    float2 uv0 = (3 - 2 * st) / uvW0 - 2;
    float2 uv1 = (3 + st) / uvW1;
    float2 uv2 = st / uvW2 + 2;
    kernel[0] = float3(base_uv + uv0 * ShadowMapTextureTexelSize_id94, uvW0.x * uvW0.y);
    kernel[1] = float3(base_uv + float2(uv1.x, uv0.y) * ShadowMapTextureTexelSize_id94, uvW1.x * uvW0.y);
    kernel[2] = float3(base_uv + float2(uv2.x, uv0.y) * ShadowMapTextureTexelSize_id94, uvW2.x * uvW0.y);
    kernel[3] = float3(base_uv + float2(uv0.x, uv1.y) * ShadowMapTextureTexelSize_id94, uvW0.x * uvW1.y);
    kernel[4] = float3(base_uv + uv1 * ShadowMapTextureTexelSize_id94, uvW1.x * uvW1.y);
    kernel[5] = float3(base_uv + float2(uv2.x, uv1.y) * ShadowMapTextureTexelSize_id94, uvW2.x * uvW1.y);
    kernel[6] = float3(base_uv + float2(uv0.x, uv2.y) * ShadowMapTextureTexelSize_id94, uvW0.x * uvW2.y);
    kernel[7] = float3(base_uv + float2(uv1.x, uv2.y) * ShadowMapTextureTexelSize_id94, uvW1.x * uvW2.y);
    kernel[8] = float3(base_uv + uv2 * ShadowMapTextureTexelSize_id94, uvW2.x * uvW2.y);
    return 144.0;
}
float SampleThickness_id79(float3 shadowSpaceCoordinate, float3 pixelPositionWS, float2 depthRanges, float4x4 inverseWorldToShadowCascadeUV, bool isOrthographic)
{
    const float shadowMapDepth = ShadowMapTexture_id92.SampleLevel(LinearBorderSampler_id126, shadowSpaceCoordinate.xy, 0).r;
    float thickness;
    if (isOrthographic)
    {
        thickness = abs(shadowMapDepth - shadowSpaceCoordinate.z) * depthRanges.y;
    }
    else
    {
        float4 shadowmapPositionWorldSpace = Project_id68(float4(shadowSpaceCoordinate.xy, shadowMapDepth, 1.0), inverseWorldToShadowCascadeUV);
        thickness = distance(shadowmapPositionWorldSpace.xyz, pixelPositionWS.xyz);
    }
    return (thickness);
}
float Get7x7FilterKernel_id75(float2 base_uv, float2 st, out float3 kernel[16])
{
    float2 uvW0 = (5 * st - 6);
    float2 uvW1 = (11 * st - 28);
    float2 uvW2 = -(11 * st + 17);
    float2 uvW3 = -(5 * st + 1);
    float2 uv0 = (4 * st - 5) / uvW0 - 3;
    float2 uv1 = (4 * st - 16) / uvW1 - 1;
    float2 uv2 = -(7 * st + 5) / uvW2 + 1;
    float2 uv3 = -st / uvW3 + 3;
    kernel[0] = float3(base_uv + uv0 * ShadowMapTextureTexelSize_id94, uvW0.x * uvW0.y);
    kernel[1] = float3(base_uv + float2(uv1.x, uv0.y) * ShadowMapTextureTexelSize_id94, uvW1.x * uvW0.y);
    kernel[2] = float3(base_uv + float2(uv2.x, uv0.y) * ShadowMapTextureTexelSize_id94, uvW2.x * uvW0.y);
    kernel[3] = float3(base_uv + float2(uv3.x, uv0.y) * ShadowMapTextureTexelSize_id94, uvW3.x * uvW0.y);
    kernel[4] = float3(base_uv + float2(uv0.x, uv1.y) * ShadowMapTextureTexelSize_id94, uvW0.x * uvW1.y);
    kernel[5] = float3(base_uv + uv1 * ShadowMapTextureTexelSize_id94, uvW1.x * uvW1.y);
    kernel[6] = float3(base_uv + float2(uv2.x, uv1.y) * ShadowMapTextureTexelSize_id94, uvW2.x * uvW1.y);
    kernel[7] = float3(base_uv + float2(uv3.x, uv1.y) * ShadowMapTextureTexelSize_id94, uvW3.x * uvW1.y);
    kernel[8] = float3(base_uv + float2(uv0.x, uv2.y) * ShadowMapTextureTexelSize_id94, uvW0.x * uvW2.y);
    kernel[9] = float3(base_uv + float2(uv1.x, uv2.y) * ShadowMapTextureTexelSize_id94, uvW1.x * uvW2.y);
    kernel[10] = float3(base_uv + uv2 * ShadowMapTextureTexelSize_id94, uvW2.x * uvW2.y);
    kernel[11] = float3(base_uv + float2(uv3.x, uv2.y) * ShadowMapTextureTexelSize_id94, uvW3.x * uvW2.y);
    kernel[12] = float3(base_uv + float2(uv0.x, uv3.y) * ShadowMapTextureTexelSize_id94, uvW0.x * uvW3.y);
    kernel[13] = float3(base_uv + float2(uv1.x, uv3.y) * ShadowMapTextureTexelSize_id94, uvW1.x * uvW3.y);
    kernel[14] = float3(base_uv + float2(uv2.x, uv3.y) * ShadowMapTextureTexelSize_id94, uvW2.x * uvW3.y);
    kernel[15] = float3(base_uv + uv3 * ShadowMapTextureTexelSize_id94, uvW3.x * uvW3.y);
    return 2704.0;
}
float GetAngularAttenuation_id103(float3 lightVector, float3 lightDirection, float lightAngleScale, float lightAngleOffset)
{
    float cd = dot(lightDirection, lightVector);
    float attenuation = saturate(cd * lightAngleScale + lightAngleOffset);
    attenuation *= attenuation;
    return attenuation;
}
float GetDistanceAttenuation_id102(float lightVectorLength, float lightInvSquareRadius)
{
    float d2 = lightVectorLength * lightVectorLength;
    float attenuation = 1.0 / (max(d2, 0.01 * 0.01));
    attenuation *= SmoothDistanceAttenuation_id101(d2, lightInvSquareRadius);
    return attenuation;
}
float GetDistanceAttenuation_id92(float lightVectorLength, float lightInvSquareRadius)
{
    float d2 = lightVectorLength * lightVectorLength;
    float attenuation = 1.0 / (max(d2, 0.01 * 0.01));
    attenuation *= SmoothDistanceAttenuation_id90(d2, lightInvSquareRadius);
    return attenuation;
}
float SampleAndFilter_id83(float3 adjustedPixelPositionWS, float3 adjustedPixelPositionShadowSpace, float2 depthRanges, float4x4 inverseWorldToShadowCascadeUV, bool isOrthographic, bool isDualParaboloid = false)
{
    float2 uv = adjustedPixelPositionShadowSpace.xy * ShadowMapTextureSize_id93;
    float2 base_uv = floor(uv + 0.5);
    float2 st = uv + 0.5 - base_uv;
    base_uv *= ShadowMapTextureTexelSize_id94;
    float thickness = 0.0;
    float normalizationFactor = 1.0;

    {
        float3 kernel[9];
        normalizationFactor = Get5x5FilterKernel_id77(base_uv, st, kernel);

        [unroll]
        for (int i = 0; i < 9; ++i)
        {
            thickness += kernel[i].z * SampleThickness_id79(float3(kernel[i].xy, adjustedPixelPositionShadowSpace.z), adjustedPixelPositionWS, depthRanges, inverseWorldToShadowCascadeUV, isOrthographic);
        }
    }
    return (thickness / normalizationFactor);
}
void CalculateAdjustedShadowSpacePixelPosition_id82(float filterRadiusInPixels, float3 pixelPositionWS, float3 meshNormalWS, float4x4 worldToShadowCascadeUV, float4x4 inverseWorldToShadowCascadeUV, out float3 adjustedPixelPositionWS, out float3 adjustedPixelPositionShadowSpace)
{
    float4 bottomLeftTexelWS = Project_id68(float4(0.0, 0.0, 0.0, 1.0), inverseWorldToShadowCascadeUV);
    const float4 topRightTexelWS = Project_id68(float4(ShadowMapTextureTexelSize_id94.xy * filterRadiusInPixels, 0.0, 1.0), inverseWorldToShadowCascadeUV);
    const float texelDiagonalLength = distance(topRightTexelWS.xyz, bottomLeftTexelWS.xyz);
    const float3 positionOffsetWS = meshNormalWS * texelDiagonalLength;
    adjustedPixelPositionWS = pixelPositionWS - positionOffsetWS;
    const float4 shadowMapCoordinate = Project_id68(float4(adjustedPixelPositionWS, 1.0), worldToShadowCascadeUV);
    adjustedPixelPositionShadowSpace = shadowMapCoordinate.xyz;
}
void CalculateAdjustedShadowSpacePixelPositionPerspective_id81(float filterRadiusInPixels, float3 pixelPositionWS, float3 meshNormalWS, float4x4 worldToShadowCascadeUV, float4x4 inverseWorldToShadowCascadeUV, out float3 adjustedPixelPositionWS, out float3 adjustedPixelPositionShadowSpace)
{
    const float4 shadowMapCoordinate = Project_id68(float4(pixelPositionWS, 1.0), worldToShadowCascadeUV);
    const float4 topRightTexelWS = Project_id68(float4(shadowMapCoordinate.xy + ShadowMapTextureTexelSize_id94.xy * filterRadiusInPixels, shadowMapCoordinate.z, 1.0), inverseWorldToShadowCascadeUV);
    const float texelDiagonalLength = distance(topRightTexelWS.xyz, pixelPositionWS);
    const float3 positionOffsetWS = meshNormalWS * texelDiagonalLength;
    adjustedPixelPositionWS = pixelPositionWS - positionOffsetWS;
    const float4 adjustedShadowMapCoordinate = Project_id68(float4(adjustedPixelPositionWS, 1.0), worldToShadowCascadeUV);
    adjustedPixelPositionShadowSpace = adjustedShadowMapCoordinate.xyz;
}
float GetFilterRadiusInPixels_id80()
{

    {
        return 3.5;
    }
}
float SampleTextureAndCompare_id76(float2 position, float positionDepth)
{
    return ShadowMapTexture_id92.SampleCmpLevelZero(LinearClampCompareLessEqualSampler_id127, position, positionDepth);
}
void CalculatePCFKernelParameters_id74(float2 position, out float2 base_uv, out float2 st)
{
    float2 uv = position * ShadowMapTextureSize_id93;
    base_uv = floor(uv + 0.5);
    st = uv + 0.5 - base_uv;
    base_uv -= 0.5;
    base_uv *= ShadowMapTextureTexelSize_id94;
}
float ComputeAttenuation_id104(float3 PositionWS, float3 AngleOffsetAndInvSquareRadius, float3 DirectionWS, float3 position, inout float3 lightVectorNorm)
{
    float3 lightVector = PositionWS - position;
    float lightVectorLength = length(lightVector);
    lightVectorNorm = lightVector / lightVectorLength;
    float3 lightAngleOffsetAndInvSquareRadius = AngleOffsetAndInvSquareRadius;
    float2 lightAngleAndOffset = lightAngleOffsetAndInvSquareRadius.xy;
    float lightInvSquareRadius = lightAngleOffsetAndInvSquareRadius.z;
    float3 lightDirection = -DirectionWS;
    float attenuation = GetDistanceAttenuation_id102(lightVectorLength, lightInvSquareRadius);
    attenuation *= GetAngularAttenuation_id103(lightVectorNorm, lightDirection, lightAngleAndOffset.x, lightAngleAndOffset.y);
    return attenuation;
}
float ComputeAttenuation_id91(PointLightDataInternal light, float3 position, inout float3 lightVectorNorm)
{
    float3 lightVector = light.PositionWS - position;
    float lightVectorLength = length(lightVector);
    lightVectorNorm = lightVector / lightVectorLength;
    float lightInvSquareRadius = light.InvSquareRadius;
    return GetDistanceAttenuation_id92(lightVectorLength, lightInvSquareRadius);
}
float FilterThickness_id70(float3 pixelPositionWS, float3 meshNormalWS, float2 depthRanges, float4x4 worldToShadowCascadeUV, float4x4 inverseWorldToShadowCascadeUV, bool isOrthographic)
{
    float3 adjustedPixelPositionWS;
    float3 adjustedPixelPositionShadowSpace;
    if (isOrthographic)
    {
        CalculateAdjustedShadowSpacePixelPosition_id82(GetFilterRadiusInPixels_id80(), pixelPositionWS, meshNormalWS, worldToShadowCascadeUV, inverseWorldToShadowCascadeUV, adjustedPixelPositionWS, adjustedPixelPositionShadowSpace);
    }
    else
    {
        CalculateAdjustedShadowSpacePixelPositionPerspective_id81(GetFilterRadiusInPixels_id80(), pixelPositionWS, meshNormalWS, worldToShadowCascadeUV, inverseWorldToShadowCascadeUV, adjustedPixelPositionWS, adjustedPixelPositionShadowSpace);
    }
    return SampleAndFilter_id83(adjustedPixelPositionWS, adjustedPixelPositionShadowSpace, depthRanges, inverseWorldToShadowCascadeUV, isOrthographic);
}
float FilterShadow_id69(float2 position, float positionDepth)
{
    float shadow = 0.0f;
    float2 base_uv;
    float2 st;
    CalculatePCFKernelParameters_id74(position, base_uv, st);

    {
        float3 kernel[9];
        float normalizationFactor = Get5x5FilterKernel_id77(base_uv, st, kernel);

        [unroll]
        for (int i = 0; i < 9; ++i)
        {
            shadow += kernel[i].z * SampleTextureAndCompare_id76(kernel[i].xy, positionDepth);
        }
        shadow /= normalizationFactor;
    }
    return shadow;
}
void ProcessLight_id105(inout PS_STREAMS streams, SpotLightDataInternal light)
{
    float3 lightVectorNorm;
    float attenuation = ComputeAttenuation_id104(light.PositionWS, light.AngleOffsetAndInvSquareRadius, light.DirectionWS, streams.PositionWS_id23.xyz, lightVectorNorm);
    streams.lightColor_id44 = light.Color;
    streams.lightAttenuation_id47 = attenuation;
    streams.lightDirectionWS_id43 = lightVectorNorm;
}
void ProcessLight_id94(inout PS_STREAMS streams, PointLightDataInternal light)
{
    float3 lightVectorNorm;
    float attenuation = ComputeAttenuation_id91(light, streams.PositionWS_id23.xyz, lightVectorNorm);
    streams.lightPositionWS_id42 = light.PositionWS;
    streams.lightColor_id44 = light.Color;
    streams.lightAttenuation_id47 = attenuation;
    streams.lightDirectionWS_id43 = lightVectorNorm;
}
float ComputeThicknessFromCascade_id73(float3 pixelPositionWS, float3 meshNormalWS, int cascadeIndex, int lightIndex, bool isOrthographic)
{
    const int arrayIndex = cascadeIndex + lightIndex * TCascadeCountBase_id154;
    return FilterThickness_id70(pixelPositionWS, meshNormalWS, DepthRanges_id159[arrayIndex], WorldToShadowCascadeUV_id156[arrayIndex], InverseWorldToShadowCascadeUV_id157[arrayIndex], isOrthographic);
}
float ComputeShadowFromCascade_id72(float3 shadowPositionWS, int cascadeIndex, int lightIndex)
{
    float4 shadowPosition = mul(float4(shadowPositionWS, 1.0), WorldToShadowCascadeUV_id156[cascadeIndex + lightIndex * TCascadeCountBase_id154]);
    shadowPosition.z -= DepthBiases_id160[lightIndex];
    shadowPosition.xyz /= shadowPosition.w;
    return FilterShadow_id69(shadowPosition.xy, shadowPosition.z);
}
float3 GetShadowPositionOffset_id71(float offsetScale, float nDotL, float3 normal)
{
    float normalOffsetScale = saturate(1.0f - nDotL);
    return 2.0f * ShadowMapTextureTexelSize_id94.x * offsetScale * normalOffsetScale * normal;
}
void PrepareEnvironmentLight_id109(inout PS_STREAMS streams)
{
    streams.envLightDiffuseColor_id48 = 0;
    streams.envLightSpecularColor_id49 = 0;
}
float3 ComputeSpecularTextureProjection_id100(float3 positionWS, float3 reflectionWS, int lightIndex)
{
    return 1.0f;
}
float3 ComputeTextureProjection_id99(float3 positionWS, int lightIndex)
{
    return 1.0f;
}
float3 ComputeShadow_id98(inout PS_STREAMS streams, float3 position, int lightIndex)
{
    streams.thicknessWS_id85 = 0.0;
    return 1.0f;
}
void PrepareDirectLightCore_id97(inout PS_STREAMS streams, int lightIndexIgnored)
{
    int realLightIndex = LightIndices_id179.Load(streams.lightIndex_id177);
    streams.lightIndex_id177++;
    SpotLightDataInternal spotLight;
    float4 spotLight1 = SpotLights_id184.Load(realLightIndex * 4);
    float4 spotLight2 = SpotLights_id184.Load(realLightIndex * 4 + 1);
    float4 spotLight3 = SpotLights_id184.Load(realLightIndex * 4 + 2);
    float4 spotLight4 = SpotLights_id184.Load(realLightIndex * 4 + 3);
    spotLight.PositionWS = spotLight1.xyz;
    spotLight.DirectionWS = spotLight2.xyz;
    spotLight.AngleOffsetAndInvSquareRadius = spotLight3.xyz;
    spotLight.Color = spotLight4.xyz;
    ProcessLight_id105(streams, spotLight);
}
float3 ComputeSpecularTextureProjection_id89(float3 positionWS, float3 reflectionWS, int lightIndex)
{
    return 1.0f;
}
float3 ComputeTextureProjection_id88(float3 positionWS, int lightIndex)
{
    return 1.0f;
}
float3 ComputeShadow_id87(inout PS_STREAMS streams, float3 position, int lightIndex)
{
    streams.thicknessWS_id85 = 0.0;
    return 1.0f;
}
void PrepareDirectLightCore_id86(inout PS_STREAMS streams, int lightIndexIgnored)
{
    int realLightIndex = LightIndices_id179.Load(streams.lightIndex_id177);
    streams.lightIndex_id177++;
    PointLightDataInternal pointLight;
    float4 pointLight1 = PointLights_id183.Load(realLightIndex * 2);
    float4 pointLight2 = PointLights_id183.Load(realLightIndex * 2 + 1);
    pointLight.PositionWS = pointLight1.xyz;
    pointLight.InvSquareRadius = pointLight1.w;
    pointLight.Color = pointLight2.xyz;
    ProcessLight_id94(streams, pointLight);
}
void PrepareLightData_id93(inout PS_STREAMS streams)
{
    float projectedDepth = streams.ShadingPosition_id0.z;
    float depth = ZProjection_id173.y / (projectedDepth - ZProjection_id173.x);
    float2 texCoord = float2(streams.ScreenPosition_id170.x + 1, 1 - streams.ScreenPosition_id170.y) * 0.5;
    int slice = int(max(log2(depth * ClusterDepthScale_id180 + ClusterDepthBias_id181), 0));
    streams.lightData_id176 = LightClusters_id178.Load(int4(texCoord * ClusterStride_id182, slice, 0));
    streams.lightIndex_id177 = streams.lightData_id176.x;
}
float3 ComputeSpecularTextureProjection_id65(float3 positionWS, float3 reflectionWS, int lightIndex)
{
    return 1.0f;
}
float3 ComputeTextureProjection_id64(float3 positionWS, int lightIndex)
{
    return 1.0f;
}
float3 ComputeShadow_id63(inout PS_STREAMS streams, float3 position, int lightIndex)
{
    int cascadeIndexBase = lightIndex * TCascadeCount_id162;
    int cascadeIndex = 0;

    [unroll]
    for (int i = 0; i < TCascadeCount_id162 - 1; i++)
    {
        [flatten]
        if (streams.DepthVS_id24 > CascadeDepthSplits_id168[cascadeIndexBase + i])
        {
            cascadeIndex = i + 1;
        }
    }
    float3 shadow = 1.0;
    float tempThickness = 999.0;
    float3 shadowPosition = position.xyz;
    shadowPosition += GetShadowPositionOffset_id71(OffsetScales_id161[lightIndex], streams.NdotL_id50, streams.normalWS_id20);
    if (cascadeIndex < TCascadeCount_id162)
    {
        shadow = ComputeShadowFromCascade_id72(shadowPosition, cascadeIndex, lightIndex);
        float nextSplit = CascadeDepthSplits_id168[cascadeIndexBase + cascadeIndex];
        float splitSize = nextSplit;
        if (cascadeIndex > 0)
        {
            splitSize = nextSplit - CascadeDepthSplits_id168[cascadeIndexBase + cascadeIndex - 1];
        }
        float splitDist = (nextSplit - streams.DepthVS_id24) / splitSize;
        if (splitDist < 0.2)
        {
            float lerpAmt = smoothstep(0.0, 0.2, splitDist);
            if (cascadeIndex == TCascadeCount_id162 - 1)
            {
            }
            else if (TBlendCascades_id164)
            {
                float nextShadow = ComputeShadowFromCascade_id72(shadowPosition, cascadeIndex + 1, lightIndex);
                shadow = lerp(nextShadow, shadow, lerpAmt);
            }
        }
    }
    streams.thicknessWS_id85 = tempThickness;
    return shadow;
}
void PrepareDirectLightCore_id62(inout PS_STREAMS streams, int lightIndex)
{
    streams.lightColor_id44 = Lights_id91[lightIndex].Color;
    streams.lightDirectionWS_id43 = -Lights_id91[lightIndex].DirectionWS;
}
float3 ComputeSpecularTextureProjection_id59(float3 positionWS, float3 reflectionWS, int lightIndex)
{
    return 1.0f;
}
float3 ComputeTextureProjection_id58(float3 positionWS, int lightIndex)
{
    return 1.0f;
}
float3 ComputeShadow_id57(inout PS_STREAMS streams, float3 position, int lightIndex)
{
    streams.thicknessWS_id85 = 0.0;
    return 1.0f;
}
void PrepareDirectLightCore_id56(inout PS_STREAMS streams, int lightIndex)
{
    streams.lightColor_id44 = Lights_id88[lightIndex].Color;
    streams.lightDirectionWS_id43 = -Lights_id88[lightIndex].DirectionWS;
}
void ResetStream_id124()
{
}
void AfterLightingAndShading_id203()
{
}
float3 ComputeEnvironmentLightContribution_id196(inout PS_STREAMS streams)
{
    float3 diffuseColor = streams.matDiffuseVisible_id70;
    return diffuseColor * streams.envLightDiffuseColor_id48;
}
void PrepareEnvironmentLight_id192(inout PS_STREAMS streams)
{
    PrepareEnvironmentLight_id109(streams);
    float3 lightColor = AmbientLight_id185 * streams.matAmbientOcclusion_id58;
    streams.envLightDiffuseColor_id48 = lightColor;
    streams.envLightSpecularColor_id49 = lightColor;
}
void PrepareDirectLight_id183(inout PS_STREAMS streams, int lightIndex)
{
    PrepareDirectLightCore_id97(streams, lightIndex);
    streams.NdotL_id50 = max(dot(streams.normalWS_id20, streams.lightDirectionWS_id43), 0.0001f);
    streams.shadowColor_id84 = ComputeShadow_id98(streams, streams.PositionWS_id23.xyz, lightIndex);
    streams.lightColorNdotL_id45 = streams.lightColor_id44 * streams.lightAttenuation_id47 * streams.shadowColor_id84 * streams.NdotL_id50 * streams.lightDirectAmbientOcclusion_id51;
    streams.lightSpecularColorNdotL_id46 = streams.lightColorNdotL_id45;
    streams.lightColorNdotL_id45 *= ComputeTextureProjection_id99(streams.PositionWS_id23.xyz, lightIndex);
    float3 reflectionVectorWS = reflect(-streams.viewWS_id69, streams.normalWS_id20);
    streams.lightSpecularColorNdotL_id46 *= ComputeSpecularTextureProjection_id100(streams.PositionWS_id23.xyz, reflectionVectorWS, lightIndex);
}
int GetLightCount_id187(inout PS_STREAMS streams)
{
    return streams.lightData_id176.y >> 16;
}
int GetMaxLightCount_id186(inout PS_STREAMS streams)
{
    return streams.lightData_id176.y >> 16;
}
void PrepareDirectLights_id181()
{
}
void PrepareDirectLight_id170(inout PS_STREAMS streams, int lightIndex)
{
    PrepareDirectLightCore_id86(streams, lightIndex);
    streams.NdotL_id50 = max(dot(streams.normalWS_id20, streams.lightDirectionWS_id43), 0.0001f);
    streams.shadowColor_id84 = ComputeShadow_id87(streams, streams.PositionWS_id23.xyz, lightIndex);
    streams.lightColorNdotL_id45 = streams.lightColor_id44 * streams.lightAttenuation_id47 * streams.shadowColor_id84 * streams.NdotL_id50 * streams.lightDirectAmbientOcclusion_id51;
    streams.lightSpecularColorNdotL_id46 = streams.lightColorNdotL_id45;
    streams.lightColorNdotL_id45 *= ComputeTextureProjection_id88(streams.PositionWS_id23.xyz, lightIndex);
    float3 reflectionVectorWS = reflect(-streams.viewWS_id69, streams.normalWS_id20);
    streams.lightSpecularColorNdotL_id46 *= ComputeSpecularTextureProjection_id89(streams.PositionWS_id23.xyz, reflectionVectorWS, lightIndex);
}
int GetLightCount_id175(inout PS_STREAMS streams)
{
    return streams.lightData_id176.y & 0xFFFF;
}
int GetMaxLightCount_id174(inout PS_STREAMS streams)
{
    return streams.lightData_id176.y & 0xFFFF;
}
void PrepareDirectLights_id173(inout PS_STREAMS streams)
{
    PrepareLightData_id93(streams);
}
void PrepareDirectLight_id145(inout PS_STREAMS streams, int lightIndex)
{
    PrepareDirectLightCore_id62(streams, lightIndex);
    streams.NdotL_id50 = max(dot(streams.normalWS_id20, streams.lightDirectionWS_id43), 0.0001f);
    streams.shadowColor_id84 = ComputeShadow_id63(streams, streams.PositionWS_id23.xyz, lightIndex);
    streams.lightColorNdotL_id45 = streams.lightColor_id44 * streams.lightAttenuation_id47 * streams.shadowColor_id84 * streams.NdotL_id50 * streams.lightDirectAmbientOcclusion_id51;
    streams.lightSpecularColorNdotL_id46 = streams.lightColorNdotL_id45;
    streams.lightColorNdotL_id45 *= ComputeTextureProjection_id64(streams.PositionWS_id23.xyz, lightIndex);
    float3 reflectionVectorWS = reflect(-streams.viewWS_id69, streams.normalWS_id20);
    streams.lightSpecularColorNdotL_id46 *= ComputeSpecularTextureProjection_id65(streams.PositionWS_id23.xyz, reflectionVectorWS, lightIndex);
}
int GetLightCount_id147()
{
    return LightCount_id89;
}
int GetMaxLightCount_id148()
{
    return TMaxLightCount_id90;
}
void PrepareDirectLights_id143()
{
}
float3 ComputeDirectLightContribution_id195(inout PS_STREAMS streams)
{
    float3 diffuseColor = streams.matDiffuseVisible_id70;
    return diffuseColor / PI_id193 * streams.lightColorNdotL_id45 * streams.matDiffuseSpecularAlphaBlend_id66.x;
}
void PrepareMaterialPerDirectLight_id32(inout PS_STREAMS streams)
{
    streams.H_id76 = normalize(streams.viewWS_id69 + streams.lightDirectionWS_id43);
    streams.NdotH_id77 = saturate(dot(streams.normalWS_id20, streams.H_id76));
    streams.LdotH_id78 = saturate(dot(streams.lightDirectionWS_id43, streams.H_id76));
    streams.VdotH_id79 = streams.LdotH_id78;
}
void PrepareDirectLight_id134(inout PS_STREAMS streams, int lightIndex)
{
    PrepareDirectLightCore_id56(streams, lightIndex);
    streams.NdotL_id50 = max(dot(streams.normalWS_id20, streams.lightDirectionWS_id43), 0.0001f);
    streams.shadowColor_id84 = ComputeShadow_id57(streams, streams.PositionWS_id23.xyz, lightIndex);
    streams.lightColorNdotL_id45 = streams.lightColor_id44 * streams.lightAttenuation_id47 * streams.shadowColor_id84 * streams.NdotL_id50 * streams.lightDirectAmbientOcclusion_id51;
    streams.lightSpecularColorNdotL_id46 = streams.lightColorNdotL_id45;
    streams.lightColorNdotL_id45 *= ComputeTextureProjection_id58(streams.PositionWS_id23.xyz, lightIndex);
    float3 reflectionVectorWS = reflect(-streams.viewWS_id69, streams.normalWS_id20);
    streams.lightSpecularColorNdotL_id46 *= ComputeSpecularTextureProjection_id59(streams.PositionWS_id23.xyz, reflectionVectorWS, lightIndex);
}
int GetLightCount_id136()
{
    return LightCount_id86;
}
int GetMaxLightCount_id137()
{
    return TMaxLightCount_id87;
}
void PrepareDirectLights_id132()
{
}
void PrepareForLightingAndShading_id200()
{
}
void PrepareMaterialForLightingAndShading_id123(inout PS_STREAMS streams)
{
    streams.lightDirectAmbientOcclusion_id51 = lerp(1.0, streams.matAmbientOcclusion_id58, streams.matAmbientOcclusionDirectLightingFactor_id59);
    streams.matDiffuseVisible_id70 = streams.matDiffuse_id54.rgb * lerp(1.0f, streams.matCavity_id60, streams.matCavityDiffuse_id61) * streams.matDiffuseSpecularAlphaBlend_id66.r * streams.matAlphaBlendColor_id67;
    streams.matSpecularVisible_id72 = streams.matSpecular_id56.rgb * streams.matSpecularIntensity_id57 * lerp(1.0f, streams.matCavity_id60, streams.matCavitySpecular_id62) * streams.matDiffuseSpecularAlphaBlend_id66.g * streams.matAlphaBlendColor_id67;
    streams.NdotV_id73 = max(dot(streams.normalWS_id20, streams.viewWS_id69), 0.0001f);
    float roughness = 1.0f - streams.matGlossiness_id55;
    streams.alphaRoughness_id71 = max(roughness * roughness, 0.001);
}
void ResetLightStream_id122(inout PS_STREAMS streams)
{
    streams.lightPositionWS_id42 = 0;
    streams.lightDirectionWS_id43 = 0;
    streams.lightColor_id44 = 0;
    streams.lightColorNdotL_id45 = 0;
    streams.lightSpecularColorNdotL_id46 = 0;
    streams.lightAttenuation_id47 = 1.0f;
    streams.envLightDiffuseColor_id48 = 0;
    streams.envLightSpecularColor_id49 = 0;
    streams.lightDirectAmbientOcclusion_id51 = 1.0f;
    streams.NdotL_id50 = 0;
}
void UpdateNormalFromTangentSpace_id23(float3 normalInTangentSpace)
{
}
float4 Compute_id193(inout PS_STREAMS streams)
{
    return Texture_id186.Sample(Sampler_id187, streams.TexCoord_id143 * scale_id189 + offset_id190).rgba;
}
void ResetStream_id125(inout PS_STREAMS streams)
{
    ResetStream_id124();
    streams.matBlend_id41 = 0.0f;
}
void Compute_id239(inout PS_STREAMS streams)
{
    UpdateNormalFromTangentSpace_id23(streams.matNormal_id52);
    if (!streams.IsFrontFace_id1)
        streams.normalWS_id20 = -streams.normalWS_id20;
    ResetLightStream_id122(streams);
    PrepareMaterialForLightingAndShading_id123(streams);

    {
        PrepareForLightingAndShading_id200();
    }
    float3 directLightingContribution = 0;

    {
        PrepareDirectLights_id132();
        const int maxLightCount = GetMaxLightCount_id137();
        int count = GetLightCount_id136();

        for (int i = 0; i < maxLightCount; i++)
        {
            if (i >= count)
            {
                break;
            }
            PrepareDirectLight_id134(streams, i);
            PrepareMaterialPerDirectLight_id32(streams);

            {
                directLightingContribution += ComputeDirectLightContribution_id195(streams);
            }
        }
    }

    {
        PrepareDirectLights_id143();
        const int maxLightCount = GetMaxLightCount_id148();
        int count = GetLightCount_id147();

        for (int i = 0; i < maxLightCount; i++)
        {
            if (i >= count)
            {
                break;
            }
            PrepareDirectLight_id145(streams, i);
            PrepareMaterialPerDirectLight_id32(streams);

            {
                directLightingContribution += ComputeDirectLightContribution_id195(streams);
            }
        }
    }

    {
        PrepareDirectLights_id173(streams);
        const int maxLightCount = GetMaxLightCount_id174(streams);
        int count = GetLightCount_id175(streams);

        for (int i = 0; i < maxLightCount; i++)
        {
            if (i >= count)
            {
                break;
            }
            PrepareDirectLight_id170(streams, i);
            PrepareMaterialPerDirectLight_id32(streams);

            {
                directLightingContribution += ComputeDirectLightContribution_id195(streams);
            }
        }
    }

    {
        PrepareDirectLights_id181();
        const int maxLightCount = GetMaxLightCount_id186(streams);
        int count = GetLightCount_id187(streams);

        for (int i = 0; i < maxLightCount; i++)
        {
            if (i >= count)
            {
                break;
            }
            PrepareDirectLight_id183(streams, i);
            PrepareMaterialPerDirectLight_id32(streams);

            {
                directLightingContribution += ComputeDirectLightContribution_id195(streams);
            }
        }
    }
    float3 environmentLightingContribution = 0;

    {
        PrepareEnvironmentLight_id192(streams);

        {
            environmentLightingContribution += ComputeEnvironmentLightContribution_id196(streams);
        }
    }
    streams.shadingColor_id74 += directLightingContribution * PI_id193 + environmentLightingContribution;
    streams.shadingColorAlpha_id75 = streams.matDiffuse_id54.a;

    {
        AfterLightingAndShading_id203();
    }
}
void Compute_id222(inout PS_STREAMS streams)
{
    float4 colorBase = Compute_id193(streams);
    streams.matDiffuse_id54 = colorBase;
    streams.matColorBase_id53 = colorBase;
}
void ResetStream_id126(inout PS_STREAMS streams)
{
    ResetStream_id125(streams);
    streams.matNormal_id52 = float3(0, 0, 1);
    streams.matColorBase_id53 = 0.0f;
    streams.matDiffuse_id54 = 0.0f;
    streams.matDiffuseVisible_id70 = 0.0f;
    streams.matSpecular_id56 = 0.0f;
    streams.matSpecularVisible_id72 = 0.0f;
    streams.matSpecularIntensity_id57 = 1.0f;
    streams.matGlossiness_id55 = 0.0f;
    streams.alphaRoughness_id71 = 1.0f;
    streams.matAmbientOcclusion_id58 = 1.0f;
    streams.matAmbientOcclusionDirectLightingFactor_id59 = 0.0f;
    streams.matCavity_id60 = 1.0f;
    streams.matCavityDiffuse_id61 = 0.0f;
    streams.matCavitySpecular_id62 = 0.0f;
    streams.matEmissive_id63 = 0.0f;
    streams.matEmissiveIntensity_id64 = 0.0f;
    streams.matScatteringStrength_id65 = 1.0f;
    streams.matDiffuseSpecularAlphaBlend_id66 = 1.0f;
    streams.matAlphaBlendColor_id67 = 1.0f;
    streams.matAlphaDiscard_id68 = 0.1f;
}
float4 ComputeShadingPosition_id11(float4 world)
{
    return mul(world, ViewProjection_id30);
}
void PostTransformPosition_id6()
{
}
void PreTransformPosition_id4()
{
}
float3 EncodeNormal_id50(float3 n)
{
    return n * 0.5 + 0.5;
}
void Compute_id46(inout PS_STREAMS streams)
{

    {
        Compute_id222(streams);
    }

    {
        Compute_id239(streams);
    }
}
void ResetStream_id45(inout PS_STREAMS streams)
{
    ResetStream_id126(streams);
    streams.shadingColorAlpha_id75 = 1.0f;
}
void PostTransformPosition_id12(inout VS_STREAMS streams)
{
    PostTransformPosition_id6();
    streams.ShadingPosition_id0 = ComputeShadingPosition_id11(streams.PositionWS_id23);
    streams.PositionH_id25 = streams.ShadingPosition_id0;
    streams.DepthVS_id24 = streams.ShadingPosition_id0.w;
}
void TransformPosition_id5()
{
}
void PreTransformPosition_id10(inout VS_STREAMS streams)
{
    PreTransformPosition_id4();
    streams.PositionWS_id23 = mul(streams.Position_id22, World_id33);
}
float4 Compute_id44(inout PS_STREAMS streams)
{
    return float4(streams.matSpecularVisible_id72, sqrt(streams.alphaRoughness_id71));
}
float4 Compute_id43(inout PS_STREAMS streams)
{
    return float4(EncodeNormal_id50(streams.normalWS_id20), 1);
}
float4 Shading_id31(inout PS_STREAMS streams)
{
    streams.viewWS_id69 = normalize(Eye_id32.xyz - streams.PositionWS_id23.xyz);
    streams.shadingColor_id74 = 0;
    ResetStream_id45(streams);
    Compute_id46(streams);
    return float4(streams.shadingColor_id74, streams.shadingColorAlpha_id75);
}
void PSMain_id1()
{
}
void BaseTransformVS_id8(inout VS_STREAMS streams)
{
    PreTransformPosition_id10(streams);
    TransformPosition_id5();
    PostTransformPosition_id12(streams);
}
void VSMain_id0()
{
}
void PSMain_id3(inout PS_STREAMS streams)
{
    PSMain_id1();
    streams.ColorTarget_id2 = Shading_id31(streams);
    streams.ColorTarget1_id3 = Compute_id43(streams);
    streams.ColorTarget2_id4 = Compute_id44(streams);
}
void GenerateNormal_PS_id22(inout PS_STREAMS streams)
{
    if (dot(streams.normalWS_id20, streams.normalWS_id20) > 0)
        streams.normalWS_id20 = normalize(streams.normalWS_id20);
    streams.meshNormalWS_id18 = streams.normalWS_id20;
}
void GenerateNormal_VS_id21(inout VS_STREAMS streams)
{
    streams.meshNormalWS_id18 = mul(streams.meshNormal_id17, (float3x3)WorldInverseTranspose_id35);
    streams.normalWS_id20 = streams.meshNormalWS_id18;
}
void VSMain_id9(inout VS_STREAMS streams)
{
    VSMain_id0();
    BaseTransformVS_id8(streams);
}
void PSMain_id20(inout PS_STREAMS streams)
{
    GenerateNormal_PS_id22(streams);
    PSMain_id3(streams);
}
void VSMain_id19(inout VS_STREAMS streams)
{
    VSMain_id9(streams);
    GenerateNormal_VS_id21(streams);
}
PS_OUTPUT PSMain(PS_INPUT __input__)
{
    PS_STREAMS streams = (PS_STREAMS)0;
    streams.PositionWS_id23 = __input__.PositionWS_id23;
    streams.ShadingPosition_id0 = __input__.ShadingPosition_id0;
    streams.DepthVS_id24 = __input__.DepthVS_id24;
    streams.normalWS_id20 = __input__.normalWS_id20;
    streams.ScreenPosition_id170 = __input__.ScreenPosition_id170;
    streams.TexCoord_id143 = __input__.TexCoord_id143;
    streams.IsFrontFace_id1 = __input__.IsFrontFace_id1;
    streams.ScreenPosition_id170 /= streams.ScreenPosition_id170.w;
    PSMain_id20(streams);
    PS_OUTPUT __output__ = (PS_OUTPUT)0;
    __output__.ColorTarget_id2 = streams.ColorTarget_id2;
    __output__.ColorTarget1_id3 = streams.ColorTarget1_id3;
    __output__.ColorTarget2_id4 = streams.ColorTarget2_id4;
    return __output__;
}
VS_OUTPUT VSMain(VS_INPUT __input__)
{
    VS_STREAMS streams = (VS_STREAMS)0;
    streams.Position_id22 = __input__.Position_id22;
    streams.meshNormal_id17 = __input__.meshNormal_id17;
    streams.TexCoord_id143 = __input__.TexCoord_id143;
    VSMain_id19(streams);
    streams.ScreenPosition_id170 = streams.ShadingPosition_id0;
    VS_OUTPUT __output__ = (VS_OUTPUT)0;
    __output__.PositionWS_id23 = streams.PositionWS_id23;
    __output__.ShadingPosition_id0 = streams.ShadingPosition_id0;
    __output__.DepthVS_id24 = streams.DepthVS_id24;
    __output__.normalWS_id20 = streams.normalWS_id20;
    __output__.ScreenPosition_id170 = streams.ScreenPosition_id170;
    __output__.TexCoord_id143 = streams.TexCoord_id143;
    return __output__;
}
