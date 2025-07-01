using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Rendering
{
    internal unsafe struct ThreadLocalAABB
    {
        private const int kAABBNumFloats = 6;
        private const int kCacheLineNumFloats = JobsUtility.CacheLineSize / 4;
        private const int kCacheLinePadding = kCacheLineNumFloats - kAABBNumFloats;

        public MinMaxAABB AABB;
        // Pad the size of this struct to a single cache line, to ensure that thread local updates
        // don't cause false sharing
        public fixed float CacheLinePadding[kCacheLinePadding];

        public static void AssertCacheLineSize()
        {
            Assert.IsTrue(UnsafeUtility.SizeOf<ThreadLocalAABB>() == JobsUtility.CacheLineSize,
                "ThreadLocalAABB should have a size equal to the CPU cache line size");
        }
    }

    [BurstCompile]
    internal unsafe struct ZeroThreadLocalAABBJob : IJobParallelFor
    {
        public NativeArray<ThreadLocalAABB> ThreadLocalAABBs;

        public void Execute(int index)
        {
            var threadLocalAABB = ((ThreadLocalAABB*) ThreadLocalAABBs.GetUnsafePtr()) + index;
            threadLocalAABB->AABB = MinMaxAABB.Empty;
        }
    }

}
