//#define ALWAYS_PERSISTENT
//#define NO_TEMP

#if AR_UNSAFE_MEMORY_TRACKING
#define TRACK_MEMORY
#endif

using Unity.Collections;

namespace Awaken.Utility.Collections {
    public static class ARAlloc {
#if ALWAYS_PERSISTENT
        public const Allocator Persistent = Allocator.Persistent;
        public const Allocator Domain = Allocator.Domain;
        public const Allocator Temp = Allocator.Persistent;
        public const Allocator TempJob = Allocator.Persistent;
        public const Allocator InJobTempJob = Allocator.TempJob;
        public const Allocator InJobTemp = Allocator.Temp;
#elif NO_TEMP || TRACK_MEMORY
        public const Allocator Persistent = Allocator.Persistent;
        public const Allocator Domain = Allocator.Domain;
        public const Allocator Temp = Allocator.TempJob;
        public const Allocator TempJob = Allocator.TempJob;
        public const Allocator InJobTempJob = Allocator.TempJob;
        public const Allocator InJobTemp = Allocator.Temp;
#else
        public const Allocator Persistent = Allocator.Persistent;
        public const Allocator Domain = Allocator.Domain;
        public const Allocator Temp = Allocator.Temp;
        public const Allocator TempJob = Allocator.TempJob;
        public const Allocator InJobTempJob = Allocator.TempJob;
        public const Allocator InJobTemp = Allocator.Temp;
#endif
    }
}
