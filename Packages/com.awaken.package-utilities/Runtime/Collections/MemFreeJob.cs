using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Awaken.PackageUtilities.Collections {
    [BurstCompile]
    public readonly unsafe struct MemFreeJob : IJob {
        [NativeDisableUnsafePtrRestriction]
        readonly void* _ptr;
        readonly Allocator _allocator;

        public MemFreeJob(void* ptr, Allocator allocator) {
            this._ptr = ptr;
            this._allocator = allocator;
        }
        
        public static JobHandle Schedule(void* ptr, Allocator allocator, JobHandle dependsOn = default) {
            var job = new MemFreeJob(ptr, allocator);
            return job.Schedule(dependsOn);
        }

        public void Execute() {
            AllocationsTracker.Free(_ptr, _allocator);
        }
    }
}