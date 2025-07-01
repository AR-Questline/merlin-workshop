#ifndef VERTEXANIMATIONUTILS_INCLUDED
#define VERTEX_ANIMATION_INCLUDED

#include "VectorEncodingDecoding.hlsl"
#include "SampleTexture2DArrayLOD.hlsl"

float2 PixelPosToUV(uint x, uint y, uint textureWidth, uint textureHeight)
{
    return float2(x / (float)textureWidth, y / (float)textureHeight);
}

float3 Slerp(float3 start, float3 end, float t)
{
    const float theta = acos(dot(start, end));
    if (theta > 1e-4)
    {
        // Compute the sine of the angle to avoid division by zero issues
        const float sinTheta = sin(theta);
        const float3 result = (sin((1.0 - t) * theta) * start + sin(t * theta) * end) / sinTheta;
        return normalize(result);
    }
    // If the angle is very small, perform a simple linear interpolation to avoid division by zero
    return normalize(lerp(start, end, t));
}

void GetPixelData(SamplerState texSampler, Texture2DArray positionsTextureArray, uint textureWidth, uint textureHeight,
                  uint texturePixelsCount, uint pixelAbsoluteIndex,
                  out float3 position, out float3 normal)
{
    const uint pixelIndex = pixelAbsoluteIndex % texturePixelsCount;
    const uint pixelX = pixelIndex % textureWidth;
    const uint pixelY = pixelIndex / textureWidth;
    const float2 pixelHalfSize = float2(((float)1 / textureWidth) * 0.5f, ((float)1 / textureHeight) * 0.5f);
    const float2 pixelUv = PixelPosToUV(pixelX, pixelY, textureWidth, textureHeight) + pixelHalfSize;
    const uint pixelTextureIndex = pixelAbsoluteIndex / texturePixelsCount;
    float4 pixelData;
    SampleTexture2DArrayLOD_float(positionsTextureArray, pixelUv, texSampler, pixelTextureIndex, 0, pixelData);

    position = pixelData.xyz;
    DecodeFloat1ToFloat3(pixelData.w, normal);
}


float GetAnimationOffsetValue(float4 animationsOffset, uint animationOffsetsIndex, uint animationIndex,
                              uint animationsOffsetsGroup)
{
    return animationsOffset[animationIndex % 4] * (animationsOffsetsGroup == animationOffsetsIndex ? 1 : 0);
}

void VA_ARRAY_float(SamplerState texSampler, Texture2DArray positionsTextureArray, float4 animationsOffsets0,
                    float4 animationsOffsets1, float4 animationsOffsets2, float4 animationsOffsets3,
                    uint textureWidth, uint textureHeight, uint fps, int verticesCount, int vertexIndex,
                    float time, uint animationIndex,
                    out float3 position, out float3 normal)
{
    const uint texturePixelsCount = textureWidth * textureHeight;
    const uint animationOffsetsGroup = animationIndex / 4;
    const float animationOffset = GetAnimationOffsetValue(animationsOffsets0, 0, animationIndex, animationOffsetsGroup) +
        GetAnimationOffsetValue(animationsOffsets1, 1, animationIndex, animationOffsetsGroup) +
        GetAnimationOffsetValue(animationsOffsets2, 2, animationIndex, animationOffsetsGroup) +
        GetAnimationOffsetValue(animationsOffsets3, 3, animationIndex, animationOffsetsGroup);
    const uint animationPixelsStartAbsoluteIndex = (int)animationOffset;
    const float frame = time * fps;
    float3 position0, position1;
    float3 normal0, normal1;
    {
        const uint frame0Index = floor(frame);
        const uint pixel0AbsoluteIndex = animationPixelsStartAbsoluteIndex + (frame0Index * verticesCount +
            vertexIndex);
        GetPixelData(texSampler, positionsTextureArray, textureWidth, textureHeight, texturePixelsCount,
                     pixel0AbsoluteIndex, position0, normal0);
    }
    {
        const uint frame1Index = ceil(frame);
        const uint pixel1AbsoluteIndex = animationPixelsStartAbsoluteIndex + (frame1Index * verticesCount +
            vertexIndex);
        GetPixelData(texSampler, positionsTextureArray, textureWidth, textureHeight, texturePixelsCount,
                     pixel1AbsoluteIndex, position1, normal1);
    }
    const float animationBlend = frac(frame);
    position = lerp(position0, position1, animationBlend);
    normal = Slerp(normal0, normal1, animationBlend);
    normal = normal0;
}

#endif
