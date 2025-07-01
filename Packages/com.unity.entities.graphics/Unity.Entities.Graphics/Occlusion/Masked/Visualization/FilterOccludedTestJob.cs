#if ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)

using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Rendering.Occlusion.Masked.Dots;
using UnityEngine.Rendering;

namespace Unity.Rendering.Occlusion.Masked.Visualization
{
    /* Take in all tests (i.e. occludees), and only return the ones which the occlusion system identifies as being fully
       occluded by other geometry. */
    [BurstCompile]
    unsafe struct FilterOccludedTestJob : IJobParallelFor
    {
        [ReadOnly] public BatchCullingProjectionType ProjectionType;
        [ReadOnly] public int NumTilesX;
        [ReadOnly] public v128 HalfSize;
        [ReadOnly] public v128 PixelCenter;
        [ReadOnly] public v128 ScreenSize;
        [ReadOnly, NativeDisableUnsafePtrRestriction] public Tile* Tiles;
        [ReadOnly] public NativeArray<OcclusionTest> AllTests;

        [WriteOnly] public NativeQueue<OcclusionTest>.ParallelWriter culledTestsQueue;

        public void Execute(int i)
        {
            OcclusionTest test = AllTests[i];

            CullingResult cullingResult = TestJob.TestRect(
                test.screenMin.xy,
                test.screenMax.xy,
                test.screenMin.w,
                Tiles,
                ProjectionType,
                NumTilesX,
                ScreenSize,
                HalfSize,
                PixelCenter
            );

            if (cullingResult == CullingResult.OCCLUDED)
            {
                culledTestsQueue.Enqueue(test);
            }
        }
    }
}

#endif // ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)
