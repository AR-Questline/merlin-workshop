#if ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)

using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using Unity.Rendering.Occlusion.Masked.Dots;
using Unity.Transforms;

namespace Unity.Rendering.Occlusion.Masked.Visualization
{
    /* Return a single mesh containing all of the input meshes, with a stable random color assigned to each source mesh. */
    [BurstCompile]
    unsafe struct MeshAggregationJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<OcclusionMesh> Meshes;
        [ReadOnly] public NativeArray<LocalToWorld> LocalToWorlds;
        [ReadOnly] public NativeArray<int> VertOffsets;
        [ReadOnly] public NativeArray<int> IndexOffsets;

        [WriteOnly, NativeDisableContainerSafetyRestriction] public NativeArray<Vector4> Verts;
        [WriteOnly, NativeDisableContainerSafetyRestriction] public NativeArray<Color32> Colors;
        [WriteOnly, NativeDisableContainerSafetyRestriction] public NativeArray<int> Indices;

        public void Execute(int m)
        {
            OcclusionMesh mesh = Meshes[m];
            var srcVerts = (float3*) mesh.vertexData.GetUnsafePtr();

            // Create a random fully saturated color from the mesh index
            Color32 col;
            {
                float hue = Random.CreateFromIndex((uint) m).NextFloat();
                float h6 = 6f * hue;
                float3 c = math.saturate(
                    new float3(
                        2f - math.abs(h6 - 4f),
                        math.abs(h6 - 3f) - 1f,
                        2f - math.abs(h6 - 2f)
                    )
                );
                col = new Color32((byte) (c.x * 255), (byte) (c.y * 255), (byte) (c.z * 255), 255);
            }
            // Copy over all the vertices, transforming them into world space. Also assign a random color.
            for (int i = 0; i < mesh.vertexCount; i++)
            {
                // We discarded the last row of the occluder matrix to save memory bandwidth because it is always (0, 0, 0,1).
                // However, to perform the actual math, we still need a 4x4 matrix. So we reintroduce the row here.
                float4x4 occluderMtx = new float4x4(
                    new float4(mesh.localTransform.c0, 0f),
                    new float4(mesh.localTransform.c1, 0f),
                    new float4(mesh.localTransform.c2, 0f),
                    new float4(mesh.localTransform.c3, 1f)
                );
                Verts[VertOffsets[m] + i] = math.mul(math.mul(LocalToWorlds[m].Value, occluderMtx), new float4(srcVerts[i], 1.0f));
                Colors[VertOffsets[m] + i] = col;
            }

            // Copy over all the indices, while adding an offset
            var srcIndices = (int*) mesh.indexData.GetUnsafePtr();
            for (int i = 0; i < mesh.indexCount; i++)
            {
                Indices[IndexOffsets[m] + i] = VertOffsets[m] + srcIndices[i];
            }
        }
    }
}

#endif // ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)
