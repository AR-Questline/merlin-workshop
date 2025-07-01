using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Awaken.Utility.LowLevel {
    public static class UnsafeUtils {
        public static JobHandle ReleasePinnedArray(ulong gcHandle, JobHandle dependencies) {
            return new ReleasePinnedArrayJob {
                gcHandle = gcHandle
            }.Schedule(dependencies);
        }

        struct ReleasePinnedArrayJob : IJob {
            public ulong gcHandle;

            public void Execute() {
                UnsafeUtility.ReleaseGCObject(gcHandle);
            }
        }
    }
}