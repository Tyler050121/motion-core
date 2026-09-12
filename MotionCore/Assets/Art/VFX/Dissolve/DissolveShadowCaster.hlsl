#ifndef DISSOLVE_SHADOW_CASTER_INCLUDED
#define DISSOLVE_SHADOW_CASTER_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Dissolve.hlsl"

#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

float3 _LightDirection;
float3 _LightPosition;

struct DissolveShadowAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 texcoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct DissolveShadowVaryings
{
#if defined(_ALPHATEST_ON)
    float2 uv : TEXCOORD0;
#endif
    float3 positionWS : TEXCOORD1;
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

float4 GetDissolveShadowPosition(DissolveShadowAttributes input, float3 positionWS)
{
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

#if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif

    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

#if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif

    return positionCS;
}

DissolveShadowVaryings DissolveShadowPassVertex(DissolveShadowAttributes input)
{
    DissolveShadowVaryings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.positionWS = positionWS;

#if defined(_ALPHATEST_ON)
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
#endif

    output.positionCS = GetDissolveShadowPosition(input, positionWS);
    return output;
}

half4 DissolveShadowPassFragment(DissolveShadowVaryings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);

    UNITY_BRANCH if (_DissolveThreshold > 0.0h)
    {
        float3 positionOS = TransformWorldToObject(input.positionWS);
        clip(DissolveNoise(positionOS) - _DissolveThreshold);
    }

#if defined(_ALPHATEST_ON)
    Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
#endif

#if defined(LOD_FADE_CROSSFADE)
    LODFadeCrossFade(input.positionCS);
#endif

    return 0;
}

#endif
