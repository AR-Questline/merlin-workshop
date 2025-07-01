#if ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)

using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering.Occlusion.Masked.Dots;
using Unity.Transforms;
using UnityEngine.Rendering;

namespace Unity.Rendering.Occlusion.Masked
{
    [BurstCompile(DisableSafetyChecks = true)]
    unsafe struct MeshTransformJob : IJobFor
    {
        [ReadOnly] public float4x4 ViewProjection;
        [ReadOnly] public BatchCullingProjectionType ProjectionType;
        [ReadOnly] public float NearClip;
        [ReadOnly, NativeDisableUnsafePtrRestriction] public v128* FrustumPlanes;
        [ReadOnly] public v128 HalfWidth;
        [ReadOnly] public v128 HalfHeight;
        [ReadOnly] public v128 PixelCenterX;
        [ReadOnly] public v128 PixelCenterY;
        [ReadOnly] public NativeArray<LocalToWorld> LocalToWorlds;
        [ReadOnly] public NativeArray<OcclusionMesh> Meshes;

        public NativeArray<float4> TransformedVerts;
        public int TransformedVertsStride;
        public NativeArray<float3> ClippedVerts;
        public NativeArray<float4> ClippedTriExtents;
        public NativeArray<ClippedOccluder> ClippedOccluders;

        [NativeSetThreadIndex]
        internal int m_ThreadIndex;
        
        public void Execute(int i)
        {
            var mesh = Meshes[i];
            // We discarded the last row of the occluder matrix to save memory bandwidth because it is always (0, 0, 0,1).
            // However, to perform the actual math, we still need a 4x4 matrix. So we reintroduce the row here.
            float4x4 occluderMtx = new float4x4(
                new float4(mesh.localTransform.c0, 0f),
                new float4(mesh.localTransform.c1, 0f),
                new float4(mesh.localTransform.c2, 0f),
                new float4(mesh.localTransform.c3, 1f)
            );
            float4x4 mvp = math.mul(ViewProjection, math.mul(LocalToWorlds[i].Value, occluderMtx));

            var clipped = (ClippedOccluder*)ClippedOccluders.GetUnsafePtr();
            
            float4* transformedVertsPtr = &((float4*)TransformedVerts.GetUnsafePtr())[m_ThreadIndex * TransformedVertsStride];
            
            mesh.Transform(mvp, ProjectionType, NearClip, FrustumPlanes, HalfWidth.Float0, HalfHeight.Float0,
                PixelCenterX.Float0, PixelCenterY.Float0, transformedVertsPtr, ClippedVerts, ClippedTriExtents, &clipped[i]);
        }
    }
}

#endif // ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)
