#if AR_UNSAFE_MEMORY_TRACKING
#define TRACK_MEMORY
#if AR_UNSAFE_MEMORY_TRACKING_WITH_STACK
#define WITH_STACK
#endif
#endif

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using SpinLock = Awaken.PackageUtilities.Threading.SpinLock;

namespace Awaken.PackageUtilities.Collections {
    public static unsafe class AllocationsTracker {
        const int Verbosity = (int)Allocator.FirstUserIndex;

        static readonly SharedStatic<UnsafeList<Allocation>> SharedAllocations = SharedStatic<UnsafeList<Allocation>>.GetOrCreate<Allocation>();
        static readonly SharedStatic<SpinLock> SharedLock = SharedStatic<SpinLock>.GetOrCreate<SpinLock>();
        static readonly SharedStatic<MainThreadId> SharedMainThreadId = SharedStatic<MainThreadId>.GetOrCreate<MainThreadId>();

        public static void Init() {
#if TRACK_MEMORY
            Debug.LogError("Starting AllocationsTracker...");
            SharedAllocations.Data = new UnsafeList<Allocation>(2048, Allocator.Domain);
            SharedLock.Data = new SpinLock();
            SharedMainThreadId.Data = new MainThreadId (Thread.CurrentThread.ManagedThreadId);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* Malloc<T>(uint length, Allocator allocator) where T : unmanaged {
#if TRACK_MEMORY
            var byteCount = (uint)(UnsafeUtility.SizeOf<T>() * length);
            var ptr = (T*)UnsafeUtility.MallocTracked(byteCount, UnsafeUtility.AlignOf<T>(), allocator, 1);

            var intPtr = (IntPtr)ptr;
            var stackTrace = BurstStacktrace.GetStackTrace();
            var allocation = new Allocation(intPtr, byteCount, allocator, stackTrace);

            try {
                SharedLock.Data.EnterWrite();
                ref var sharedAllocations = ref SharedAllocations.Data;
                var index = sharedAllocations.BinarySearch(allocation);
                if (index >= 0) {
                    var existingAllocation = sharedAllocations[index];
                    if (existingAllocation.byteCount != 0 && length != 0) {
                        MakeNotBurstedInfo_DoubleAllocation(existingAllocation, allocation);
                        throw new InvalidOperationException("Double allocation detected");
                    } else {
                        return ptr;
                    }
                }

                index = ~index;
                sharedAllocations.InsertRange(index, 1);
                sharedAllocations[index] = allocation;

                if (Verbosity <= (int)allocator) {
                    Debug.LogError($"Allocation {intPtr.ToInt64()} with allocator {allocator} and size {byteCount}");
                }
            } finally {
                SharedLock.Data.ExitWrite();
            }

            return ptr;
#else
            return (T*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>() * length, UnsafeUtility.AlignOf<T>(), allocator);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free(void* ptr, Allocator allocator) {
#if TRACK_MEMORY
            var intPtr = (IntPtr)ptr;
            var allocation = new Allocation(intPtr, 0, allocator, default);

            try {
                SharedLock.Data.EnterWrite();
                ref var sharedAllocations = ref SharedAllocations.Data;
                var index = sharedAllocations.BinarySearch(allocation);
                if (index < 0) {
                    MakeNotBurstedInfo_DoubleFree(intPtr);
                    throw new InvalidOperationException("Double free detected");
                }
                sharedAllocations.RemoveAt(index);

                if (Verbosity <= (int)allocator) {
                    Debug.LogError($"Free {intPtr.ToInt64()} with allocator {allocator}");
                }
            } finally {
                SharedLock.Data.ExitWrite();
            }

            UnsafeUtility.FreeTracked(ptr, allocator);
#else
            UnsafeUtility.Free(ptr, allocator);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CustomAllocation<T>(T* ptr, uint length, Allocator allocator) where T : unmanaged {
#if TRACK_MEMORY
            var byteCount = (uint)(UnsafeUtility.SizeOf<T>() * length);

            var intPtr = (IntPtr)ptr;
            var stackTrace = BurstStacktrace.GetStackTrace();
            var allocation = new Allocation(intPtr, byteCount, allocator, stackTrace);

            try {
                SharedLock.Data.EnterWrite();
                ref var sharedAllocations = ref SharedAllocations.Data;
                var index = sharedAllocations.BinarySearch(allocation);
                if (index >= 0) {
                    var existingAllocation = sharedAllocations[index];
                    if (existingAllocation.byteCount != 0 && length != 0) {
                        MakeNotBurstedInfo_DoubleAllocation(existingAllocation, allocation);
                        throw new InvalidOperationException("Double allocation detected");
                    } else {
                        return;
                    }
                }

                index = ~index;
                sharedAllocations.InsertRange(index, 1);
                sharedAllocations[index] = allocation;

                if (Verbosity <= (int)allocator) {
                    Debug.LogError($"Allocation {intPtr.ToInt64()} with allocator {allocator} and size {byteCount}");
                }
            } finally {
                SharedLock.Data.ExitWrite();
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CustomAllocation<T>(in NativeArray<T> array) where T : unmanaged {
#if TRACK_MEMORY
            var ptr = (T*)array.GetUnsafePtr();
            var length = (uint)array.Length;
            var allocator = Allocator.None;

            CustomAllocation(ptr, length, allocator);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CustomFree(void* ptr, Allocator allocator) {
#if TRACK_MEMORY
            var intPtr = (IntPtr)ptr;
            var allocation = new Allocation(intPtr, 0, allocator, default);

            try {
                SharedLock.Data.EnterWrite();
                ref var sharedAllocations = ref SharedAllocations.Data;
                var index = sharedAllocations.BinarySearch(allocation);
                if (index < 0) {
                    MakeNotBurstedInfo_DoubleFree(intPtr);
                    throw new InvalidOperationException("Double free detected");
                }
                sharedAllocations.RemoveAt(index);

                if (Verbosity <= (int)allocator) {
                    Debug.LogError($"Free {intPtr.ToInt64()} with allocator {allocator}");
                }
            } finally {
                SharedLock.Data.ExitWrite();
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CustomFree<T>(in NativeArray<T> array) where T : unmanaged {
#if TRACK_MEMORY
            var ptr = (T*)array.GetUnsafePtr();
            var allocator = Allocator.None;

            CustomFree(ptr, allocator);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JobHandle CustomFreeSchedule(void* ptr, Allocator allocator, JobHandle dependencies = default) {
#if TRACK_MEMORY
            return new CustomFreeJob {
                ptr = ptr,
                allocator = allocator
            }.Schedule(dependencies);
#else
            return dependencies;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JobHandle CustomFreeSchedule<T>(in NativeArray<T> array, JobHandle dependencies = default) where T : unmanaged {
#if TRACK_MEMORY
            var ptr = (T*)array.GetUnsafePtr();
            var allocator = Allocator.None;

            return CustomFreeSchedule(ptr, allocator);
#else
            return dependencies;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* Access<T>(T* ptr, long offset) where T : unmanaged {
#if TRACK_MEMORY
            var allocation = new Allocation((IntPtr)(ptr+offset), 0, Allocator.Invalid, default);

            try {
                SharedLock.Data.EnterRead();
                ref var sharedAllocations = ref SharedAllocations.Data;
                var index = sharedAllocations.BinarySearch(allocation, new AccessComparer());
                if (index < 0) {
                    MakeNotBurstedInfo_InvalidAccess(allocation.ptr);
                    throw new InvalidOperationException("Accessing unallocated memory detected");
                }
            } finally {
                SharedLock.Data.ExitRead();
            }
#endif
            return ptr + offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* Access<T>(T* ptr) where T : unmanaged {
#if TRACK_MEMORY
            return Access(ptr, 0);
#endif
            return ptr;
        }

        readonly struct Allocation : IEquatable<Allocation>, IComparable<Allocation> {
            public readonly IntPtr ptr;
            public readonly uint byteCount;
            public readonly Allocator allocator;
#if WITH_STACK
            public readonly FixedString512Bytes stackTrace;
#endif

            public long StartRange => ptr.ToInt64();
            public long EndRange => StartRange + byteCount;

            public Allocation(IntPtr ptr, uint byteCount, Allocator allocator, FixedString512Bytes stackTrace) {
                this.ptr = ptr;
                this.byteCount = byteCount;
                this.allocator = allocator;
#if WITH_STACK
                this.stackTrace = stackTrace;
#endif
            }

            public bool Equals(Allocation other) {
                return ptr == other.ptr && allocator == other.allocator;
            }

            public int CompareTo(Allocation other) {
                return ptr.ToInt64().CompareTo(other.ptr.ToInt64());
            }

            public override bool Equals(object obj) {
                return obj is Allocation other && Equals(other);
            }

            public override int GetHashCode() {
                unchecked {
                    return (ptr.GetHashCode() * 397) ^ (int)allocator;
                }
            }

            public override string ToString() {
#if WITH_STACK
                return $"[{StartRange}-{EndRange}] {allocator} From: {stackTrace}";
#else
                return $"[{StartRange}-{EndRange}] {allocator}";
#endif
            }
        }

        readonly struct AccessComparer : IComparer<Allocation> {
            public int Compare(Allocation x, Allocation y) {
                if (x.byteCount == 0 && y.byteCount == 0) {
                    return x.ptr.ToInt64().CompareTo(y.ptr.ToInt64());
                }

                var addressAlloc = x.byteCount == 0 ? x.ptr.ToInt64() : y.ptr.ToInt64();
                var (startRange, endRange) =  x.byteCount == 0 ? (y.StartRange, y.EndRange) : (x.StartRange, x.EndRange);

                if (startRange <= addressAlloc && addressAlloc < endRange) {
                    return 0;
                }

                return x.ptr.ToInt64().CompareTo(y.ptr.ToInt64());
            }
        }

        readonly struct MainThreadId : IEquatable<MainThreadId> {
            public readonly int id;

            public MainThreadId(int id) {
                this.id = id;
            }

            public bool Equals(MainThreadId other) {
                return id == other.id;
            }
        }

        [BurstDiscard]
        static void MakeNotBurstedInfo_DoubleAllocation(in Allocation existingAllocation, in Allocation newAllocation) {
            Debug.LogError($"Double allocation detected:\nExisting: {existingAllocation}\nNew: {newAllocation}");
        }

        [BurstDiscard]
        static void MakeNotBurstedInfo_DoubleFree(IntPtr ptr) {
            Debug.LogError($"Double free detected for address {ptr.ToInt64()}.");
        }

        [BurstDiscard]
        static void MakeNotBurstedInfo_InvalidAccess(in IntPtr address) {
            Debug.LogError($"Want access to {address.ToInt64()}, but no allocation found for it.");
        }

        struct CustomFreeJob : IJob {
            [NativeDisableUnsafePtrRestriction] public void* ptr;
            public Allocator allocator;

            public void Execute() {
                AllocationsTracker.CustomFree(ptr, allocator);
            }
        }

        // === EDITOR
#if UNITY_EDITOR
#if TRACK_MEMORY
        [UnityEditor.InitializeOnLoadMethod]
        static void SetDetectionMode() {
            if (UnsafeUtility.GetLeakDetectionMode() != NativeLeakDetectionMode.EnabledWithStackTrace) {
                UnsafeUtility.SetLeakDetectionMode(NativeLeakDetectionMode.EnabledWithStackTrace);
            }
        }
#endif

        [UnityEditor.MenuItem("TG/Debug/Allocations Tracking", true)]
        static bool IsTrackingEnabled() {
#if TRACK_MEMORY
            UnityEditor.Menu.SetChecked("TG/Debug/Allocations Tracking", true);
#else
            UnityEditor.Menu.SetChecked("TG/Debug/Allocations Tracking", false);
#endif
            return true;
        }

        [UnityEditor.MenuItem("TG/Debug/Allocations Tracking")]
        static void ToggleTracking() {
            UnityEditor.PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Standalone,
                out var defines);
#if TRACK_MEMORY
            var toRemove = Array.IndexOf(defines, "AR_UNSAFE_MEMORY_TRACKING");
            defines[toRemove] = defines[^1];
            Array.Resize(ref defines, defines.Length - 1);
#else
            Array.Resize(ref defines, defines.Length + 1);
            defines[^1] = "AR_UNSAFE_MEMORY_TRACKING";
#endif
            UnityEditor.PlayerSettings.SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Standalone,
                defines);
        }

        [UnityEditor.MenuItem("TG/Debug/Print Allocations", true)]
        static bool IsPrintAllocationsAvailable() {
#if TRACK_MEMORY
            return true;
#else
            return false;
#endif
        }

        [UnityEditor.MenuItem("TG/Debug/Print Allocations")]
        static void PrintAllocations() {
            foreach (var allocation in SharedAllocations.Data) {
                Debug.Log($"Has {allocation}");
            }
        }
#endif
    }

    public static unsafe class BurstStacktrace {
        public static FixedString512Bytes GetStackTrace() {
            FixedString512Bytes fixedStackStr = default;
            ManagedStackTrace(ref fixedStackStr);

            return fixedStackStr;
        }

        [BurstDiscard]
        static void ManagedStackTrace(ref FixedString512Bytes buffer) {
#if WITH_STACK
            buffer.CopyFromTruncated(Environment.StackTrace);
#endif
        }
    }
}
