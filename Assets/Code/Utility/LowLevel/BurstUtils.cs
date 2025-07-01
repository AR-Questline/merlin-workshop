using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Burst;
using Unity.Jobs.LowLevel.Unsafe;

namespace Awaken.Utility.LowLevel {
    public static class BurstUtils {
        public static bool IsBurstCompiled {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                bool burst = true;
                Managed(ref burst);
                return burst;

                [BurstDiscard]
                static void Managed(ref bool burst) => burst = false;
            }
        }

        public static int ThreadId {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                int id = JobsUtility.ThreadIndex;
                Managed(ref id);
                return id;

                [BurstDiscard]
                static void Managed(ref int id) => id = Thread.CurrentThread.ManagedThreadId;
            }
        }
    }
}