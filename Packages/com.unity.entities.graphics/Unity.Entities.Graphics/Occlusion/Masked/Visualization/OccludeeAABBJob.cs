#if ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)

using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using Unity.Rendering.Occlusion.Masked.Dots;

namespace Unity.Rendering.Occlusion.Masked.Visualization
{
    /* Return a mesh with one quad for each test (i.e. occludee). */
    [BurstCompile]
    struct OccludeeAABBJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<OcclusionTest> CulledTests;

        [WriteOnly, NativeDisableContainerSafetyRestriction] public NativeArray<Vector3> Verts;
        [WriteOnly, NativeDisableContainerSafetyRestriction] public NativeArray<ushort> Indices;

        public void Execute(int i)
        {
            var test = CulledTests[i];

            int vertBase = 4 * i;
            int indexBase = 6 * i;

            Verts[vertBase] = new Vector3(test.screenMin.x, 0f - test.screenMin.y, 0.5f);
            Verts[vertBase + 1] = new Vector3(test.screenMax.x, 0f - test.screenMin.y, 0.5f);
            Verts[vertBase + 2] = new Vector3(test.screenMin.x, 0f - test.screenMax.y, 0.5f);
            Verts[vertBase + 3] = new Vector3(test.screenMax.x, 0f - test.screenMax.y, 0.5f);

            Indices[indexBase] = (ushort) vertBase;
            Indices[indexBase + 1] = (ushort) (vertBase + 1);
            Indices[indexBase + 2] = (ushort) (vertBase + 2);
            Indices[indexBase + 3] = (ushort) (vertBase + 2);
            Indices[indexBase + 4] = (ushort) (vertBase + 1);
            Indices[indexBase + 5] = (ushort) (vertBase + 3);
        }

    }
}

#endif // ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)
