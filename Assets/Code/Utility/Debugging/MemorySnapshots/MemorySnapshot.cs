using System;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.LowLevel.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Utility.Debugging.MemorySnapshots {
    public readonly struct MemorySnapshot {
        public readonly string name;
        public readonly string additionalInfo;
        public readonly ulong selfByteSize;
        public readonly ulong usedBytesSize;
        public readonly ReadOnlyMemory<MemorySnapshot> children;

        public MemorySnapshot(string name, ulong selfByteSize, ulong usedBytesSize, ReadOnlyMemory<MemorySnapshot> children = default, string additionalInfo = "") {
            this.name = name;
            this.additionalInfo = additionalInfo;
            this.selfByteSize = selfByteSize;
            this.usedBytesSize = usedBytesSize;
            this.children = children;
        }

        public MemorySnapshot(string name, long selfByteSize, ulong usedBytesSize, ReadOnlyMemory<MemorySnapshot> children = default, string additionalInfo = "") :
            this(name, (ulong)selfByteSize, usedBytesSize, children, additionalInfo) {}

        public MemorySnapshot(string name, long selfByteSize, long usedBytesSize, ReadOnlyMemory<MemorySnapshot> children = default, string additionalInfo = "") :
            this(name, (ulong)selfByteSize, (ulong)usedBytesSize, children, additionalInfo) {}

        public ulong TotalByteSize => selfByteSize + SumTotalSizes(children.Span);
        public ulong TotalUsedByteSize => usedBytesSize + SumTotalUsedSizes(children.Span);

        public string HumanReadableSelfByteSize => M.HumanReadableBytes(selfByteSize);
        public string HumanReadableSelfUsedByteSize => $"{M.HumanReadableBytes(usedBytesSize)}";
        public string SelfUsedPercentage => $"{usedBytesSize/(double)math.max(selfByteSize, 0.0001f):P0}";
        public string HumanReadableTotalByteSize => M.HumanReadableBytes(TotalByteSize);
        public string HumanReadableTotalUsedByteSize => $"{M.HumanReadableBytes(TotalUsedByteSize)}";
        public string TotalUsedPercentage => $"{TotalUsedByteSize/(double)TotalByteSize:P0}";

        static ulong SumTotalSizes(ReadOnlySpan<MemorySnapshot> snapshots) {
            ulong sum = 0;
            foreach (var child in snapshots) {
                sum += child.TotalByteSize;
            }
            return sum;
        }

        static ulong SumTotalUsedSizes(ReadOnlySpan<MemorySnapshot> snapshots) {
            ulong sum = 0;
            foreach (var child in snapshots) {
                sum += child.TotalUsedByteSize;
            }
            return sum;
        }
    }

    public static class MemorySnapshotUtils {
        public static void TakeSnapshot(string name, GraphicsBuffer buffer, uint usedElements, Memory<MemorySnapshot> ownPlace) {
            var stride = (ulong)(buffer?.stride ?? 0);
            var selfSize = (ulong)(buffer?.count ?? 0) * stride;
            var usedSize = (ulong)usedElements * stride;
            ownPlace.Span[0] = new MemorySnapshot(name, selfSize, usedSize, ReadOnlyMemory<MemorySnapshot>.Empty);
        }

        public static void TakeSnapshot<T, U>(string name, UnsafeHashMap<T, U> hashMap, Memory<MemorySnapshot> ownPlace)
            where T : unmanaged, IEquatable<T> where U : unmanaged {
            var selfSize = HashMapSize<T, U>(hashMap.Capacity);
            var usedSize = HashMapSize<T, U>(hashMap.Count);
            ownPlace.Span[0] = new MemorySnapshot(name, selfSize, usedSize, ReadOnlyMemory<MemorySnapshot>.Empty);
        }

        public static unsafe void TakeSnapshot<T>(string name, NativeArray<T> array, Memory<MemorySnapshot> ownPlace) where T : unmanaged {
            var size = (uint)array.Length * (ulong)sizeof(T);
            ownPlace.Span[0] = new MemorySnapshot(name, size, size, ReadOnlyMemory<MemorySnapshot>.Empty);
        }

        public static unsafe void TakeSnapshot<T>(string name, UnsafeArray<T>.Span array, Memory<MemorySnapshot> ownPlace) where T : unmanaged {
            var size = array.Length * (ulong)sizeof(T);
            ownPlace.Span[0] = new MemorySnapshot(name, size, size, ReadOnlyMemory<MemorySnapshot>.Empty);
        }

        public static ulong HashMapSize<T, U>(int length) where T : unmanaged, IEquatable<T> where U : unmanaged {
            return HashMapSize<T, U>((uint)length);
        }

        public static unsafe ulong HashMapSize<T, U>(uint length) where T : unmanaged, IEquatable<T> where U : unmanaged {
            var tSize = sizeof(T);
            var uSize = sizeof(U);
            var intSize = sizeof(int);

            var valuesSize = (ulong)(uSize * length);
            var keysSize = (ulong)(tSize * length);
            var nextSize = (ulong)(intSize * length);
            var bucketSize = (ulong)(intSize * length) * 2; // From Unity code
            return valuesSize + keysSize + nextSize + bucketSize;
        }

        public static ulong BitsToBytes(uint bits) {
            return (bits + 7) / 8;
        }
    }
}
