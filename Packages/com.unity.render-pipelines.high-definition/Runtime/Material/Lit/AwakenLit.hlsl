#ifndef AWAKEN_LIT
#define AWAKEN_LIT

#include "AwakenLitData.hlsl"

void AwakenPostProcessSurfaceData(inout SurfaceData surfaceData) 
{
    float luminance = dot(surfaceData.baseColor.rgb, float3(0.299, 0.587, 0.114));
    surfaceData.baseColor.rgb = lerp(luminance.xxx, surfaceData.baseColor.rgb, GlobalSaturation());
}

#endif