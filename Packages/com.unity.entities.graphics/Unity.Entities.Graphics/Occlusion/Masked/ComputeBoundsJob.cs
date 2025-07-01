#if ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)

using Unity.Assertions;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering.Occlusion.Masked.Dots;
using UnityEngine.Rendering;

namespace Unity.Rendering.Occlusion.Masked
{
    [BurstCompile]
    unsafe struct ComputeBoundsJob : IJobChunk
    {
        [ReadOnly] public ComponentTypeHandle<RenderBounds> Bounds;
        [ReadOnly] public ComponentTypeHandle<LocalToWorld> LocalToWorld;
        public ComponentTypeHandle<OcclusionTest> OcclusionTest;
        public ComponentTypeHandle<ChunkOcclusionTest> ChunkOcclusionTest;

        [ReadOnly] public float4x4 ViewProjection;
        [ReadOnly] public float NearClip;
        [ReadOnly] public BatchCullingProjectionType ProjectionType;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            const float EPSILON = 1E-12f;
            // This job is not written to support queries with enableable component types.
            Assert.IsFalse(useEnabledMask);

            var bounds = chunk.GetNativeArray(ref Bounds);
            var localToWorld = chunk.GetNativeArray(ref LocalToWorld);
            var tests = chunk.GetNativeArray(ref OcclusionTest);

            var verts = stackalloc float4[16];

            var edges = stackalloc int2[]
            {
                new int2(0,1), new int2(1,3), new int2(3,2), new int2(2,0),
                new int2(4,6), new int2(6,7), new int2(7,5), new int2(5,4),
                new int2(4,0), new int2(2,6), new int2(1,5), new int2(7,3)
            };

            float4 screenMin = float.MaxValue;
            float4 screenMax = float.MinValue;

            for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
            {
                var aabb = bounds[entityIndex].Value;
                var occlusionTest = tests[entityIndex];
                var local = localToWorld[entityIndex].Value;

                occlusionTest.screenMin = float.MaxValue;
                occlusionTest.screenMax = -float.MaxValue;

                // TODO: There's likely still room for optimization here. Investigate more approximate bounding box
                // calculations which use less ALU ops.
                var mvp = math.mul(ViewProjection, local);

                float4x2 u = new float4x2(mvp.c0 * aabb.Min.x, mvp.c0 * aabb.Max.x);
                float4x2 v = new float4x2(mvp.c1 * aabb.Min.y, mvp.c1 * aabb.Max.y);
                float4x2 w = new float4x2(mvp.c2 * aabb.Min.z, mvp.c2 * aabb.Max.z);

                for (int corner = 0; corner < 8; corner++)
                {
                    float4 p = u[corner & 1] + v[(corner & 2) >> 1] + w[(corner & 4) >> 2] + mvp.c3;
                    p.y = -p.y;
                    verts[corner] = p;
                }

                int vertexCount = 8;
                float clipW = NearClip;
                for (int i = 0; i < 12; i++)
                {
                    var e = edges[i];
                    var a = verts[e.x];
                    var b = verts[e.y];

                    if ((a.w < clipW) != (b.w < clipW))
                    {
                        var p = math.lerp(a, b, (clipW - a.w) / (b.w - a.w));
                        verts[vertexCount++] = p;
                    }
                }

                if (ProjectionType == BatchCullingProjectionType.Orthographic)
                {
                    for (int i = 0; i < vertexCount; i++)
                    {
                        float4 p = verts[i];
                        p.w = p.z;
                        occlusionTest.screenMin = math.min(occlusionTest.screenMin, p);
                        occlusionTest.screenMax = math.max(occlusionTest.screenMax, p);
                    }
                }
                else
                {
                    for (int i = 0; i < vertexCount; i++)
                    {
                        float4 p = verts[i];
                        if (p.w >= EPSILON)
                        {
                            p.xyz /= p.w;
                            occlusionTest.screenMin = math.min(occlusionTest.screenMin, p);
                            occlusionTest.screenMax = math.max(occlusionTest.screenMax, p);
                        }
                    }
                }

                screenMin = math.min(screenMin, occlusionTest.screenMin);
                screenMax = math.max(screenMax, occlusionTest.screenMax);

                tests[entityIndex] = occlusionTest;
            }

            var combined = new ChunkOcclusionTest();
            combined.screenMin = screenMin;
            combined.screenMax = screenMax;
            chunk.SetChunkComponentData(ref ChunkOcclusionTest, combined);
        }
    }
}

#endif // ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)
