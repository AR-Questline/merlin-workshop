#pragma once

// === Data structures
struct SampledPositionAndUV
{
    float3 position;
    float2 uv;
};

struct TriangleWithUV
{
    float3 v0Pos;
    float2 v0UV;

    float3 v1Pos;
    float2 v1UV;

    float3 v2Pos;
    float2 v2UV;
};

// === Functions
inline uint WangHash(uint seed)
{
    seed = (seed ^ 61) ^ (seed >> 16);
    seed = seed + (seed << 3);
    seed = seed ^ (seed >> 4);
    seed = seed * 0x27d4eb2d;
    seed = seed ^ (seed >> 15);
    return seed;
}

inline float HashToFloat(uint hash)
{
    return (hash & 0xFFFFFF) / 16777216.0f;
}

inline float RandomFloat(uint seed)
{
    return HashToFloat(WangHash(seed));
}

inline uint RandomUInt(uint seed, uint maxValue)
{
    return (uint)(RandomFloat(seed) * maxValue);
}

int FindIndexOfArea(StructuredBuffer<float> accumulatedTriangleArea, float area, uint length, uint localSeed)
{
    int min = 0;
    int max = length - 1;
    int mid = max >> 1;

    while (max >= min)
    {
        if (mid > length)
        {
            return RandomUInt(localSeed, length);
        }

        float midArea = accumulatedTriangleArea[mid];
        if (midArea >= area && (mid == 0 || accumulatedTriangleArea[mid - 1] < area))
        {
            return mid;
        }
        if (area < midArea)
        {
            max = mid - 1;
        }
        else
        {
            min = mid + 1;
        }
        mid = (min + max) >> 1;
    }
    return RandomUInt(localSeed, length);
}