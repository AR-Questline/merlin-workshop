using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Awaken.Utility.LowLevel.Collections {
    [BurstCompile]
    public unsafe struct UnsafeArrayMemClearJob<T> : IJob where T : unmanaged {
        public UnsafeArray<T> array;

        public void Execute() {
            UnsafeUtility.MemClear(array.Ptr, array.Length * UnsafeUtility.SizeOf<T>());
        }
    }
}
