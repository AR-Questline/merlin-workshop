using System;
using System.Threading;
using Awaken.PackageUtilities.Collections;
using Awaken.Utility.Debugging;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Awaken.Utility.LowLevel.Collections {
    public readonly unsafe struct UnsafeAtomicSemaphore {
        [NativeDisableUnsafePtrRestriction]
        readonly int* _counter;
        readonly Allocator _allocator;

        public bool Taken => *AllocationsTracker.Access(_counter) > 0;

        public UnsafeAtomicSemaphore(Allocator allocator) {
            _allocator = allocator;
            _counter = AllocationsTracker.Malloc<int>(1, _allocator);
            *_counter = 0;
        }

        [UnityEngine.Scripting.Preserve] 
        public void Dispose() {
            AllocationsTracker.Free(_counter, _allocator);
        }

        public void Take() {
            Interlocked.Increment(ref *AllocationsTracker.Access(_counter));
        }

        public void Release() {
#if UNITY_EDITOR
            if (*_counter < 1) {
                throw new Exception("Semaphore counter is already at 0, cannot release more.");
            }
#endif
            Interlocked.Decrement(ref *AllocationsTracker.Access(_counter));
        }

        public JobHandle Release(JobHandle dependency) {
            return new ReleaseJob {semaphore = this}.Schedule(dependency);
        }

        struct ReleaseJob : IJob {
            public UnsafeAtomicSemaphore semaphore;

            public void Execute() {
                semaphore.Release();
            }
        }
    }
}
