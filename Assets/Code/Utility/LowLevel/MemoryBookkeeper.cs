using System;
using Awaken.CommonInterfaces;
using Awaken.PackageUtilities.Collections;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.LowLevel.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace Awaken.Utility.LowLevel {
    [BurstCompile]
    public unsafe struct MemoryBookkeeper : ILeafMemorySnapshotProvider {
        FixedString32Bytes _name;
        ARUnsafeList<MemoryRegion> _freeMemoryRegions;

        readonly uint _capacity;
        uint _peakUsage;

        public uint LastBinStart => _freeMemoryRegions[^1].start;
        public uint PeakUsage => _peakUsage;
        public uint Capacity => _capacity;
        public FixedString32Bytes Name => _name;

        public uint MaxFreeElements {
            get {
                uint maxFree = 0;
                for (var i = 0; i < _freeMemoryRegions.Length; i++) {
                    var region = _freeMemoryRegions.Ptr[i];
                    if (region.length > maxFree) {
                        maxFree = region.length;
                    }
                }

                return maxFree;
            }
        }

        public MemoryBookkeeper(string name, uint capacity, int stateInitialCapacity, Allocator allocator) {
            _name = name;
            _freeMemoryRegions = new ARUnsafeList<MemoryRegion>(stateInitialCapacity, allocator);
            _freeMemoryRegions.Add(new MemoryRegion {start = 0, length = capacity});
            _peakUsage = 0;
            _capacity = capacity;
        }

        public void Dispose() {
            _freeMemoryRegions.Dispose();
        }

        public bool FindFreeRegion(uint requiredLength, out MemoryRegion freeRegion) {
            var regionIndex = FindMemoryRegionIndex(requiredLength);
            if (regionIndex == -1) {
                freeRegion = default;
                return false;
            }

            ref var region = ref _freeMemoryRegions.Ptr[regionIndex];
            freeRegion = new MemoryRegion {
                start = region.start,
                length = requiredLength
            };
            return true;
        }

        public void TakeFreeRegion(in MemoryRegion freeRegion) {
            var regionIndex = FindMemoryRegionIndex(freeRegion);
            Asserts.IsGreaterThan(regionIndex, -1);

            ref var region = ref _freeMemoryRegions.Ptr[regionIndex];
            Asserts.IsGreaterOrEqual(region.length, freeRegion.length);

            region.start += freeRegion.length;
            region.length -= freeRegion.length;
            if (!region.IsValid) {
                _freeMemoryRegions.RemoveAt(regionIndex);
            }

            if (freeRegion.End > _peakUsage) {
                _peakUsage = freeRegion.End;
            }
        }

        public bool Take(uint requiredLength, out MemoryRegion takenRegion) {
            var regionIndex = FindMemoryRegionIndex(requiredLength);
            if (regionIndex == -1) {
                takenRegion = default;
                Log.Critical?.Error($"Cannot allocate {requiredLength} elements for {_name} heroPosition: {HeroPosition.Value.ToString()}");
                return false;
            }

            ref var region = ref _freeMemoryRegions.Ptr[regionIndex];

            takenRegion = new MemoryRegion {
                start = region.start,
                length = requiredLength
            };
            region.start += requiredLength;
            region.length -= requiredLength;
            if (!region.IsValid) {
                _freeMemoryRegions.RemoveAt(regionIndex);
            }

            if (takenRegion.End > _peakUsage) {
                _peakUsage = takenRegion.End;
            }

            return true;
        }

        public void Return(in MemoryRegion returnedRegion) {
            AddEmptyRegion(returnedRegion, ref _freeMemoryRegions);
        }

        int FindMemoryRegionIndex(uint minSize) {
            for (var i = 0; i < _freeMemoryRegions.Length; i++) {
                if (_freeMemoryRegions.Ptr[i].length >= minSize) {
                    return i;
                }
            }

            return -1;
        }

        int FindMemoryRegionIndex(in MemoryRegion freeRegion) {
            for (var i = 0; i < _freeMemoryRegions.Length; i++) {
                if (_freeMemoryRegions.Ptr[i].start == freeRegion.start) {
                    return i;
                }
            }

            return -1;
        }

        [BurstCompile]
        static void AddEmptyRegion(in MemoryRegion freedRegion, ref ARUnsafeList<MemoryRegion> emptyRegions) {
            var addedIndex = -1;
            for (var i = 0; (addedIndex == -1) & (i < emptyRegions.Length); i++) {
                var region = emptyRegions.Ptr[i];
                if (region.start >= freedRegion.End) {
                    emptyRegions.InsertRange(i, 1);
                    emptyRegions.Ptr[i] = freedRegion;
                    addedIndex = i;
                }
            }

            if (addedIndex == -1) {
                emptyRegions.Add(freedRegion);
                addedIndex = emptyRegions.Length - 1;
            }
            ConsolidateMemoryRegions(ref emptyRegions, addedIndex);
        }

        static void ConsolidateMemoryRegions(ref ARUnsafeList<MemoryRegion> emptyRegions, int changeIndex) {
            var startIndex = math.max(changeIndex - 1, 0);
            var endIndex = math.min(changeIndex + 1, emptyRegions.Length - 1);
            for (var i = startIndex; i < endIndex; i++) {
                ref var region = ref emptyRegions.Ptr[i];
                ref var nextRegion = ref emptyRegions.Ptr[i + 1];
                if (region.End == nextRegion.start) {
                    region.length += nextRegion.length;
                    emptyRegions.RemoveAt(i + 1);
                    i--;
                }
            }
        }

        public struct MemoryRegion : IEquatable<MemoryRegion>, IComparable<MemoryRegion> {
            public uint start;
            public uint length;

            public uint End => start + length;
            public bool IsValid => length > 0;

            public bool Equals(MemoryRegion other) {
                return start == other.start && length == other.length;
            }

            public int CompareTo(MemoryRegion other) {
                var startComparison = start.CompareTo(other.start);
                if (startComparison != 0) {
                    return startComparison;
                }
                return length.CompareTo(other.length);
            }

            public override string ToString() {
                return $"[{start}, {End})<{length}>";
            }
        }

        // IMemorySnapshotProvider
        public void GetMemorySnapshot(Memory<MemorySnapshot> ownPlace) {
            var memoryRegionSize = sizeof(MemoryRegion);
            var allBytes = sizeof(FixedString32Bytes) + _freeMemoryRegions.Capacity * memoryRegionSize;
            var usedBytes = sizeof(FixedString32Bytes) + _freeMemoryRegions.Length * memoryRegionSize;

            var capacity = _freeMemoryRegions[^1].End;
            var currentUsage = 0u;
            var previousEnd = 0u;
            for (var i = 0; i < _freeMemoryRegions.Length; i++) {
                var region = _freeMemoryRegions.Ptr[i];
                var gap = region.start - previousEnd;
                currentUsage += gap;
                previousEnd = region.End;
            }
            var currentUsagePercentage = (float)currentUsage / capacity;
            var peakUsagePercentage = (float)_peakUsage / capacity;

            ownPlace.Span[0] = new MemorySnapshot(_name.ToString(), allBytes, usedBytes,
                additionalInfo: $"Peak: {_peakUsage}[{peakUsagePercentage:P2}]; Current: {currentUsage}[{currentUsagePercentage:P2}]");
        }

        public readonly struct EditorAccess {
            readonly MemoryBookkeeper _bookkeeper;

            public UnsafeArray<MemoryRegion>.Span FreeMemoryRegions => _bookkeeper._freeMemoryRegions.AsUnsafeSpan();

            public EditorAccess(MemoryBookkeeper bookkeeper) {
                _bookkeeper = bookkeeper;
            }
        }
    }
}