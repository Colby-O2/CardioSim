#ifndef SHADERGRAPH_PREVIEW
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#endif

void ECG_float(

    UnityTexture2D VoltageTex,
    UnitySamplerState Sampler,
    float2 UV,
    float UVScale,
    float LineThickness,
    float YScale,
    float Presistence,
    float LeadLength,
    float HeadPosition,
    float Resolution,
    float AspectRatio,

    out float Voltage,
    out float LeadMask,
    out float GlowMask
)
{
    float dx = (1.0 / Resolution);
    float sampleX = UV.x * UVScale;
    float idx = sampleX * Resolution;

    int center = int(floor(idx));

    float2 P = float2(UV.x, (UV.y - 0.5) * AspectRatio);
    float minDstSq = 1e9;

    int searchRadius = (int) ceil((LineThickness + dx) * Resolution / UVScale);
    searchRadius = min(searchRadius, 32);

    [loop]
    for (int o = -searchRadius; o <= searchRadius; o++)
    {
        int i0 = clamp(center + o, 0, int(Resolution) - 2);
        int i1 = i0 + 1;

        float u0 = i0 * dx;
        float u1 = i1 * dx;

        float v0 = YScale * SAMPLE_TEXTURE2D(VoltageTex, Sampler, float2(u0, 0)).r;
        float v1 = YScale * SAMPLE_TEXTURE2D(VoltageTex, Sampler, float2(u1, 0)).r;

        float2 A = float2(u0 / UVScale, v0 * AspectRatio);
        float2 B = float2(u1 / UVScale, v1 * AspectRatio);

        float2 PA = P - A;
        float2 BA = B - A;
        float h = saturate(dot(PA, BA) / (dot(BA, BA) + 1e-6));
        float2 D = PA - BA * h;

        minDstSq = min(minDstSq, dot(D, D));
    }

    float l = 1.0 - smoothstep(
        0.0,
        LineThickness * LineThickness,
        minDstSq
    );
    
    float headDst = frac(1.0 + UV.x - HeadPosition);
    float fade = pow(headDst, Presistence);
    
    float distFromHead = sqrt((UV.x - HeadPosition) * (UV.x - HeadPosition) + (UV.y - 0.5) * (UV.y - 0.5));
    float headGlow = saturate(1.0 - (distFromHead * 2.0));
    
    Voltage = fade * l;
    LeadMask = smoothstep(LeadLength, 0.0, 1.0 - headDst);
    GlowMask = pow(headGlow, 3.0);
}

void ECG_half(

    UnityTexture2D VoltageTex,
    UnitySamplerState Sampler,
    half2 UV,
    half UVScale,
    half LineThickness,
    half YScale,
    half Presistence,
    half LeadLength,
    half HeadPosition,
    half Resolution,
    half AspectRatio,

    out half Voltage,
    out half LeadMask,
    out half GlowMask
)
{
    half dx = (1.0 / Resolution);
    half sampleX = UV.x * UVScale;
    half idx = sampleX * Resolution;

    int center = int(floor(idx));

    half2 P = half2(UV.x, (UV.y - 0.5) * AspectRatio);
    half minDstSq = 1e9;

    int searchRadius = (int) ceil((LineThickness + dx) * Resolution / UVScale);
    searchRadius = min(searchRadius, 32);

    [loop]
    for (int o = -searchRadius; o <= searchRadius; o++)
    {
        int i0 = clamp(center + o, 0, int(Resolution) - 2);
        int i1 = i0 + 1;

        half u0 = i0 * dx;
        half u1 = i1 * dx;

        half v0 = YScale * SAMPLE_TEXTURE2D(VoltageTex, Sampler, half2(u0, 0)).r;
        half v1 = YScale * SAMPLE_TEXTURE2D(VoltageTex, Sampler, half2(u1, 0)).r;

        half2 A = half2(u0 / UVScale, v0 * AspectRatio);
        half2 B = half2(u1 / UVScale, v1 * AspectRatio);

        half2 PA = P - A;
        half2 BA = B - A;
        half h = saturate(dot(PA, BA) / (dot(BA, BA) + 1e-6));
        half2 D = PA - BA * h;

        minDstSq = min(minDstSq, dot(D, D));
    }

    half l = 1.0 - smoothstep(
        0.0,
        LineThickness * LineThickness,
        minDstSq
    );
    
    half headDst = frac(1.0 + UV.x - HeadPosition);
    half fade = pow(headDst, Presistence);
    
    half distFromHead = sqrt((UV.x - HeadPosition) * (UV.x - HeadPosition) + (UV.y - 0.5) * (UV.y - 0.5));
    half headGlow = saturate(1.0 - (distFromHead * 2.0));
    
    Voltage = fade * l;
    LeadMask = smoothstep(LeadLength, 0.0, 1.0 - headDst);
    GlowMask = pow(headGlow, 3.0);
}