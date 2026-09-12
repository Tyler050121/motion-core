#ifndef DISSOLVE_INCLUDED
#define DISSOLVE_INCLUDED

float DissolveHash31(float3 value)
{
    value = frac(value * 0.1031);
    value += dot(value, value.yzx + 33.33);
    return frac((value.x + value.y) * value.z);
}

float DissolveNoise(float3 positionOS)
{
    float3 cell = floor(positionOS * (float)_NoiseScale);
    float3 fraction = frac(positionOS * (float)_NoiseScale);
    fraction = fraction * fraction * (3 - 2 * fraction);

    float n000 = DissolveHash31(cell);
    float n100 = DissolveHash31(cell + float3(1, 0, 0));
    float n010 = DissolveHash31(cell + float3(0, 1, 0));
    float n110 = DissolveHash31(cell + float3(1, 1, 0));
    float n001 = DissolveHash31(cell + float3(0, 0, 1));
    float n101 = DissolveHash31(cell + float3(1, 0, 1));
    float n011 = DissolveHash31(cell + float3(0, 1, 1));
    float n111 = DissolveHash31(cell + float3(1, 1, 1));

    float x00 = lerp(n000, n100, fraction.x);
    float x10 = lerp(n010, n110, fraction.x);
    float x01 = lerp(n001, n101, fraction.x);
    float x11 = lerp(n011, n111, fraction.x);
    return lerp(lerp(x00, x10, fraction.y), lerp(x01, x11, fraction.y), fraction.z);
}

half DissolveEdge(float dissolveNoise)
{
    half edgeAlpha = 1.0h - smoothstep(0.1h, 0.6h, _DissolveThreshold);
    half edge = step(0.001h, _DissolveThreshold) * (1.0h - smoothstep(
        _DissolveThreshold,
        _DissolveThreshold + _EdgeWidth,
        dissolveNoise));
    return edge * edgeAlpha;
}

#endif
