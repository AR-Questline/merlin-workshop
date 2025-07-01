#ifndef COMPUTE_WORLD_SPACE_FROM_DEPTH_INCLUDED
#define COMPUTE_WORLD_SPACE_FROM_DEPTH_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

void ComputeWorldSpaceFromDepth_float(float2 positionNDC, float deviceDepth, out float3 worldSpacePosition)
{
    worldSpacePosition = GetAbsolutePositionWS(ComputeWorldSpacePosition(positionNDC, deviceDepth, UNITY_MATRIX_I_VP));
}

void ComputeDepthUVFromWorldSpace_float(float3 positionWS, float4x4 VP, out float2 uv)
{
    uv = ComputeNormalizedDeviceCoordinates(positionWS, VP);
}

#endif
