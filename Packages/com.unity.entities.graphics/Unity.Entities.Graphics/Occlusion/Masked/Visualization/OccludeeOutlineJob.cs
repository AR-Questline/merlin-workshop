#if ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)

using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Rendering.Occlusion.Masked.Dots;

namespace Unity.Rendering.Occlusion.Masked.Visualization
{
    /* Return a mesh with four quad forming an outline for each test (i.e. occludee). */
    [BurstCompile]
    struct OccludeeOutlineJob : IJobParallelFor
    {
        [ReadOnly] public float2 InvResolution;
        [ReadOnly] public NativeArray<OcclusionTest> AllTests;

        [WriteOnly, NativeDisableContainerSafetyRestriction] public NativeArray<Vector3> Verts;
        [WriteOnly, NativeDisableContainerSafetyRestriction] public NativeArray<uint> Indices;

        public void Execute(int i)
        {
            var test = AllTests[i];

            int vertBase = 16 * i;
            int indexBase = 24 * i;

            float px = 2f * InvResolution.x; // Size of 1 pixel in texture space
            float py = 2f * InvResolution.y;
            float xmin = test.screenMin.x;
            float ymin = test.screenMin.y;
            float xmax = test.screenMax.x;
            float ymax = test.screenMax.y;

            // Left edge
            Verts[vertBase] = new Vector3(xmin, -ymin, 0.5f);
            Verts[vertBase + 1] = new Vector3(xmin + px, -ymin, 0.5f);
            Verts[vertBase + 2] = new Vector3(xmin, -ymax, 0.5f);
            Verts[vertBase + 3] = new Vector3(xmin + px, -ymax, 0.5f);

            // Right edge
            Verts[vertBase + 4] = new Vector3(xmax - px, -ymin, 0.5f);
            Verts[vertBase + 5] = new Vector3(xmax, -ymin, 0.5f);
            Verts[vertBase + 6] = new Vector3(xmax - px, -ymax, 0.5f);
            Verts[vertBase + 7] = new Vector3(xmax, -ymax, 0.5f);

            // Top edge
            Verts[vertBase + 8] = new Vector3(xmin + px, -ymin, 0.5f);
            Verts[vertBase + 9] = new Vector3(xmax - px, -ymin, 0.5f);
            Verts[vertBase + 10] = new Vector3(xmin + px, -(ymin + py), 0.5f);
            Verts[vertBase + 11] = new Vector3(xmax - px, -(ymin + py), 0.5f);

            // Bottom edge
            Verts[vertBase + 12] = new Vector3(xmin + px, -(ymax - py), 0.5f);
            Verts[vertBase + 13] = new Vector3(xmax - px, -(ymax - py), 0.5f);
            Verts[vertBase + 14] = new Vector3(xmin + px, -ymax, 0.5f);
            Verts[vertBase + 15] = new Vector3(xmax - px, -ymax, 0.5f);

            Indices[indexBase] = (uint) vertBase;
            Indices[indexBase + 1] = (uint) (vertBase + 1);
            Indices[indexBase + 2] = (uint) (vertBase + 2);
            Indices[indexBase + 3] = (uint) (vertBase + 2);
            Indices[indexBase + 4] = (uint) (vertBase + 1);
            Indices[indexBase + 5] = (uint) (vertBase + 3);

            Indices[indexBase + 6] = (uint) (vertBase + 4);
            Indices[indexBase + 7] = (uint) (vertBase + 5);
            Indices[indexBase + 8] = (uint) (vertBase + 6);
            Indices[indexBase + 9] = (uint) (vertBase + 6);
            Indices[indexBase + 10] = (uint) (vertBase + 5);
            Indices[indexBase + 11] = (uint) (vertBase + 7);

            Indices[indexBase + 12] = (uint) (vertBase + 8);
            Indices[indexBase + 13] = (uint) (vertBase + 9);
            Indices[indexBase + 14] = (uint) (vertBase + 10);
            Indices[indexBase + 15] = (uint) (vertBase + 10);
            Indices[indexBase + 16] = (uint) (vertBase + 9);
            Indices[indexBase + 17] = (uint) (vertBase + 11);

            Indices[indexBase + 18] = (uint) (vertBase + 12);
            Indices[indexBase + 19] = (uint) (vertBase + 13);
            Indices[indexBase + 20] = (uint) (vertBase + 14);
            Indices[indexBase + 21] = (uint) (vertBase + 14);
            Indices[indexBase + 22] = (uint) (vertBase + 13);
            Indices[indexBase + 23] = (uint) (vertBase + 15);
        }
    }
}

#endif // ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)
