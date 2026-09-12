#ifndef LIT_DISSOLVE_FORWARD_INCLUDED
#define LIT_DISSOLVE_FORWARD_INCLUDED

// Keep the official URP Lit forward implementation and only wrap its fragment entry point.
#ifndef REQUIRES_WORLD_SPACE_POS_INTERPOLATOR
#define REQUIRES_WORLD_SPACE_POS_INTERPOLATOR
#endif

#define LitPassFragment OriginalLitPassFragment
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
#undef LitPassFragment

#include "Dissolve.hlsl"

void LitPassFragment(
    Varyings input
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out float4 outRenderingLayers : SV_Target1
#endif
)
{
    half dissolveEdge = 0.0h;

    UNITY_BRANCH if (_DissolveThreshold > 0.0h)
    {
        float3 positionOS = TransformWorldToObject(input.positionWS);
        float dissolveNoise = DissolveNoise(positionOS);
        clip(dissolveNoise - _DissolveThreshold);
        dissolveEdge = DissolveEdge(dissolveNoise);
    }

    OriginalLitPassFragment(
        input,
        outColor
#ifdef _WRITE_RENDERING_LAYERS
        , outRenderingLayers
#endif
    );
    outColor.rgb += _EdgeColor.rgb * dissolveEdge * _EdgeColor.a;
}

#endif
