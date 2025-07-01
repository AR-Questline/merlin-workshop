using System;
using System.Runtime.CompilerServices;
using Awaken.PackageUtilities.Collections;
using Awaken.Utility.Debugging;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IL2CPP.CompilerServices;
using Unity.Mathematics;

namespace Awaken.Utility.LowLevel.Collections {
    public unsafe partial struct UnsafeSparseBitmask : IDisposable {
        const int MaxEmptyBucketsInRowCount = 1;

        [NativeDisableUnsafePtrRestriction] internal ulong* _rangedBuckets;
        [NativeDisableUnsafePtrRestriction] internal MaskRange* _ranges;
        internal uint _rangesCount;
        internal uint _rangedBucketsCount;
        internal uint _rangesAllocCount;
        internal uint _rangedBucketsAllocCount;
        internal readonly Allocator _allocator;
        
        public uint RangesCount => _rangesCount;
        public uint RangedBucketsCount => _rangedBucketsCount;
        public bool IsCreated => _allocator > Allocator.None;

        public UnsafeSparseBitmask(Allocator allocator, uint rangesPreallocateCount = 1, uint maskBucketsPreallocateCount = 1) {
            _allocator = allocator;
            if (rangesPreallocateCount > 0) {
                _ranges = AllocationsTracker.Malloc<MaskRange>(rangesPreallocateCount, _allocator);
                UnsafeUtility.MemClear(_ranges, sizeof(MaskRange) * rangesPreallocateCount);
            } else {
                _ranges = null;
            }

            if (maskBucketsPreallocateCount > 0) {
                _rangedBuckets = AllocationsTracker.Malloc<ulong>(maskBucketsPreallocateCount, _allocator);
                UnsafeUtility.MemClear(_rangedBuckets, sizeof(ulong) * maskBucketsPreallocateCount);
            } else {
                _rangedBuckets = null;
            }

            _rangesCount = 0;
            _rangedBucketsCount = 0;
            _rangesAllocCount = rangesPreallocateCount;
            _rangedBucketsAllocCount = maskBucketsPreallocateCount;
        }

        public readonly UnsafeSparseBitmask DeepClone(Allocator allocator) {
            if (_rangesCount == 0) {
                return new UnsafeSparseBitmask(allocator, 0, 0);
            }

            var copy = new UnsafeSparseBitmask(allocator, _rangesCount, _rangedBucketsCount);
            copy._rangesCount = _rangesCount;
            copy._rangedBucketsCount = _rangedBucketsCount;
            UnsafeUtility.MemCpy(copy._ranges, _ranges, sizeof(MaskRange) * _rangesCount);
            UnsafeUtility.MemCpy(copy._rangedBuckets, _rangedBuckets, sizeof(ulong) * _rangedBucketsCount);
            return copy;
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public bool this[uint index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                if (TryGetExistingRangedBucketIndex(UnsafeBitmask.Bucket(index), out var rangedBucketIndex) == false) {
                    return false;
                }

                var masked = *AllocationsTracker.Access(_rangedBuckets, rangedBucketIndex) & UnsafeBitmask.IndexInBucketMask(index);
                return masked > 0;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set {
                if (value) {
                    uint rangedBucketIndex = GetOrCreateRangedBucketIndex(UnsafeBitmask.Bucket(index));
                    *AllocationsTracker.Access(_rangedBuckets, rangedBucketIndex) |= UnsafeBitmask.IndexInBucketMask(index);
                } else if (TryGetExistingRangedBucketIndex(UnsafeBitmask.Bucket(index), out var rangedBucketIndex)) {
                    *AllocationsTracker.Access(_rangedBuckets, rangedBucketIndex) &= ~UnsafeBitmask.IndexInBucketMask(index);
                }
            }
        }

        public bool HasBucketForIndex(uint index) {
            return TryGetExistingRangedBucketIndex(UnsafeBitmask.Bucket(index), out _);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public void Up(uint index) {
            var rangedBucketIndex = GetOrCreateRangedBucketIndex(UnsafeBitmask.Bucket(index));
            *AllocationsTracker.Access(_rangedBuckets, rangedBucketIndex) |= UnsafeBitmask.IndexInBucketMask(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public void Down(uint index) {
            if (TryGetExistingRangedBucketIndex(UnsafeBitmask.Bucket(index), out var rangedBucketIndex) == false) {
                return;
            }

            *AllocationsTracker.Access(_rangedBuckets, rangedBucketIndex) &= ~UnsafeBitmask.IndexInBucketMask(index);
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public void Or(in UnsafeSparseBitmask other) {
            for (int rangeIndex = 0; rangeIndex < other._rangesCount; rangeIndex++) {
                var otherRange = other._ranges[rangeIndex];
                var otherRangeEnd = otherRange.startBucketIndex + otherRange.bucketsCount;
                other.TryGetExistingRangedBucketIndex(otherRange.startBucketIndex, out var otherRangedBucketStartIndex);
                for (ushort i = otherRange.startBucketIndex; i < otherRangeEnd; i++) {
                    var thisRangedBucketIndex = GetOrCreateRangedBucketIndex(i);
                    var otherBucket = other._rangedBuckets[otherRangedBucketStartIndex + i];
                    _rangedBuckets[thisRangedBucketIndex] |= otherBucket;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public readonly bool HasOnes() {
            for (int i = 0; i < _rangedBucketsCount; i++) {
                if (_rangedBuckets[i] > 0) {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public readonly uint CountOnes() {
            var count = 0u;
            for (int i = 0; i < _rangedBucketsCount; i++) {
                count += (uint)math.countbits(_rangedBuckets[i]);
            }

            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public void Clear() {
            UnsafeUtility.MemClear(_rangedBuckets, sizeof(long) * _rangedBucketsCount);
            UnsafeUtility.MemClear(_ranges, sizeof(MaskRange) * _rangesCount);
            _rangedBucketsCount = 0;
            _rangesCount = 0;
        }

        public static UnsafeSparseBitmask Xor(in UnsafeSparseBitmask previous, in UnsafeSparseBitmask current, Allocator allocator) {
            if (previous.RangesCount == 0) {
                return current.DeepClone(allocator);
            }

            if (current.RangesCount == 0) {
                return previous.DeepClone(allocator);
            }

            var changedBitsMask = new UnsafeSparseBitmask(allocator, current._rangesCount, current._rangedBucketsCount);
            for (int rangeIndex = 0; rangeIndex < current._rangesCount; rangeIndex++) {
                var range = current._ranges[rangeIndex];
                current.TryGetExistingRangedBucketIndex(range.startBucketIndex, out var currentRangedBucketBaseIndex);
                for (ushort bucketOffset = 0; bucketOffset < range.bucketsCount; bucketOffset++) {
                    var bucketIndex = (ushort)(range.startBucketIndex + bucketOffset);
                    var changedRangedBucketIndex = changedBitsMask.GetOrCreateRangedBucketIndex(bucketIndex);
                    // If bucket is in current and in previous
                    if (previous.TryGetExistingRangedBucketIndex(bucketIndex, out var prevRangedBucketIndex)) {
                        var prevBucket = previous._rangedBuckets[prevRangedBucketIndex];
                        var currentBucket = current._rangedBuckets[currentRangedBucketBaseIndex + bucketOffset];
                        changedBitsMask._rangedBuckets[changedRangedBucketIndex] = prevBucket ^ currentBucket;
                    } else {
                        // If bucket is in the current but not in previous
                        var currentBucket = current._rangedBuckets[currentRangedBucketBaseIndex + bucketOffset];
                        changedBitsMask._rangedBuckets[changedRangedBucketIndex] = currentBucket;
                    }
                }
            }

            for (int i = 0; i < previous._rangesCount; i++) {
                var range = previous._ranges[i];
                previous.TryGetExistingRangedBucketIndex(range.startBucketIndex, out var prevRangedBucketBaseIndex);
                for (int bucketOffset = 0; bucketOffset < range.bucketsCount; bucketOffset++) {
                    var bucketIndex = (ushort)(range.startBucketIndex + bucketOffset);
                    // Case where bucket is both in previous and current was handled in previous loop
                    if (current.TryGetExistingRangedBucketIndex(bucketIndex, out _)) {
                        continue;
                    }

                    // If bucket was in previous but not in current
                    var prevBucket = previous._rangedBuckets[prevRangedBucketBaseIndex + bucketOffset];
                    var changedRangedBucketIndex = changedBitsMask.GetOrCreateRangedBucketIndex(bucketIndex);
                    changedBitsMask._rangedBuckets[changedRangedBucketIndex] = prevBucket;
                }
            }

            return changedBitsMask;
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public void Or(in UnsafeBitmask other) {
            int bucketsCount = other.BucketsLength;
            for (ushort otherBucketIndex = 0; otherBucketIndex < bucketsCount; otherBucketIndex++) {
                var otherBucket = other._masks[otherBucketIndex];
                if (otherBucket == 0) {
                    continue;
                }

                var thisRangedBucketIndex = GetOrCreateRangedBucketIndex(otherBucketIndex);
                _rangedBuckets[thisRangedBucketIndex] |= otherBucket;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly OnesEnumerator EnumerateOnes() => new OnesEnumerator(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        internal readonly bool TryGetExistingRangedBucketIndex(ushort bucketIndex, out uint rangedBucketIndex) {
            return TryGetExistingRangedBucketIndex(bucketIndex, _rangesCount, _ranges, out rangedBucketIndex);
        }

        static bool TryGetExistingRangedBucketIndex(ushort bucketIndex, uint rangesCount, MaskRange* ranges, out uint rangedBucketIndex) {
            uint rangedBucketStartIndex = 0;
            for (int i = 0; i < rangesCount; i++) {
                var range = ranges[i];
                if (bucketIndex >= range.startBucketIndex && bucketIndex < range.startBucketIndex + range.bucketsCount) {
                    rangedBucketIndex = rangedBucketStartIndex + (uint)(bucketIndex - range.startBucketIndex);
                    return true;
                }

                rangedBucketStartIndex += range.bucketsCount;
            }

            rangedBucketIndex = rangedBucketStartIndex;
            return false;
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        uint GetOrCreateRangedBucketIndex(ushort bucketIndex) {
            if (TryGetExistingRangedBucketIndex(bucketIndex, out var rangedBucketIndex)) {
                return rangedBucketIndex;
            }

            uint rangedBucketsCount = _rangedBucketsCount;

            if (_rangesCount > 0) {
                // If already existing ranges do not contain bucketIndex, try to append it to the last range
                var lastRange = _ranges[_rangesCount - 1];
                var bucketOffsetFromLastRangeEnd = (bucketIndex - (lastRange.startBucketIndex + lastRange.bucketsCount)) + 1;
                // If bucketIndex is not before last range and adding it to the last range will not create
                // more than MaxEmptyBucketsInRowCount empty buckets - then increase last range 
                if (bucketOffsetFromLastRangeEnd > 0 && bucketOffsetFromLastRangeEnd <= MaxEmptyBucketsInRowCount + 1) {
                    lastRange.bucketsCount = (ushort)(lastRange.bucketsCount + (ushort)(bucketOffsetFromLastRangeEnd));
                    _ranges[_rangesCount - 1] = lastRange;
                    var newRangedBucketsCount = (rangedBucketsCount + (uint)bucketOffsetFromLastRangeEnd);
                    EnsureSizeForRangedBuckets(newRangedBucketsCount);
                    _rangedBucketsCount = newRangedBucketsCount;
                    return newRangedBucketsCount - 1;
                }
            }

            // If bucketIndex is before last range or will create too much empty buckets if added to last bucket
            // - then create a new range
            EnsureSizeForRanges(_rangesCount + 1);
            _ranges[_rangesCount] = new MaskRange(bucketIndex, 1);
            _rangesCount++;
            EnsureSizeForRangedBuckets(rangedBucketsCount + 1);
            _rangedBucketsCount = rangedBucketsCount + 1;
            return rangedBucketsCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        void EnsureSizeForRangedBuckets(uint newCount) {
            var prevCount = _rangedBucketsAllocCount;
            if (newCount <= prevCount) {
                return;
            }

            _rangedBucketsAllocCount = newCount;
            var newBuckets = AllocationsTracker.Malloc<ulong>(newCount, _allocator);
            UnsafeUtility.MemCpy(newBuckets, _rangedBuckets, sizeof(ulong) * prevCount);
            UnsafeUtility.MemClear(newBuckets + prevCount, sizeof(ulong) * (newCount - prevCount));
            if (_rangedBuckets != null) {
                AllocationsTracker.Free(_rangedBuckets, _allocator);
            }

            _rangedBuckets = newBuckets;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        void EnsureSizeForRanges(uint newCount) {
            var prevCount = _rangesAllocCount;
            if (newCount <= prevCount) {
                return;
            }

            _rangesAllocCount = newCount;
            var newRanges = AllocationsTracker.Malloc<MaskRange>(newCount, _allocator);
            UnsafeUtility.MemCpy(newRanges, _ranges, sizeof(MaskRange) * prevCount);
            UnsafeUtility.MemClear(newRanges + prevCount, sizeof(MaskRange) * (newCount - prevCount));

            if (_ranges != null) {
                AllocationsTracker.Free(_ranges, _allocator);
            }

            _ranges = newRanges;
        }

        public void Dispose() {
#if AR_DEBUG || UNITY_EDITOR
            if (_allocator == Allocator.Invalid) {
                UnityEngine.Debug.LogError($"Calling Dispose on already Disposed {nameof(UnsafeSparseBitmask)}");
            }
#endif
            if (_allocator > Allocator.None) {
                AllocationsTracker.Free(_ranges, _allocator);
                AllocationsTracker.Free(_rangedBuckets, _allocator);
            }

            this = default;
        }

        public struct SerializationAccess {
            public static ref ulong* RangedBuckets(ref UnsafeSparseBitmask bitmask) => ref bitmask._rangedBuckets;
            public static ref MaskRange* Ranges(ref UnsafeSparseBitmask bitmask) => ref bitmask._ranges;
            public static ref uint RangesCount(ref UnsafeSparseBitmask bitmask) => ref bitmask._rangesCount;
            public static ref uint RangedBucketsCount(ref UnsafeSparseBitmask bitmask) => ref bitmask._rangedBucketsCount;
            public static ref uint RangesAllocCount(ref UnsafeSparseBitmask bitmask) => ref bitmask._rangesAllocCount;
            public static ref uint RangedBucketsAllocCount(ref UnsafeSparseBitmask bitmask) => ref bitmask._rangedBucketsAllocCount;
            public static Allocator Allocator(ref UnsafeSparseBitmask bitmask) => bitmask._allocator;
        }

        [Serializable]
        public struct MaskRange {
            public ushort startBucketIndex;
            public ushort bucketsCount;

            public MaskRange(ushort startBucketIndex, ushort bucketsCount) {
                this.startBucketIndex = startBucketIndex;
                this.bucketsCount = bucketsCount;
            }

            public override string ToString() {
                return $"{nameof(MaskRange)} start: {startBucketIndex}, count: {bucketsCount}";
            }
        }
        
        public ref struct OnesEnumerator {
            readonly ulong* _rangedBuckets;
            readonly MaskRange* _ranges;
            readonly uint _rangesCount;
            ulong* _currentBucketPtr;
            ulong _mask;
            int _index;
            uint _currentRangeIndex;
            ushort _bucketIndexInRange;
            ushort _currentRangeStart;
            ushort _currentRangeEnd;

            public OnesEnumerator(in UnsafeSparseBitmask data) {
                _rangedBuckets = data._rangedBuckets;
                _ranges = data._ranges;
                _rangesCount = data._rangesCount;
                _index = -1;
                _currentRangeIndex = 0;
                _mask = ulong.MaxValue;
                _bucketIndexInRange = 0;
                _currentBucketPtr = null;
                _currentRangeStart = 0;
                _currentRangeEnd = 0;

                if (_rangesCount > 0) {
                    InitializeRange();
                }
            }

            public bool MoveNext() {
                _index = NextOne();
                if (_index != -1) {
                    _mask ^= 1ul << _index;
                    return true;
                }

                return false;
            }

            public OnesEnumerator GetEnumerator() => this;

            [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
            public uint Current => (uint)(_index + (_currentRangeStart + _bucketIndexInRange) * (sizeof(ulong) * 8));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
            void InitializeRange() {
                if (_currentRangeIndex < _rangesCount) {
                    var currentRange = _ranges[_currentRangeIndex];
                    _currentRangeStart = currentRange.startBucketIndex;
                    _currentRangeEnd = (ushort)(_currentRangeStart + currentRange.bucketsCount);
                    _bucketIndexInRange = 0;
                    if (UnsafeSparseBitmask.TryGetExistingRangedBucketIndex(currentRange.startBucketIndex, _rangesCount, _ranges,
                            out var rangedBucketIndex) == false) {
                        Log.Important?.Error($"Cannot find ranged bucket index for range {currentRange}");
                        _currentBucketPtr = null;
                        return;
                    }

                    _currentBucketPtr = &_rangedBuckets[rangedBucketIndex];
                    _mask = ulong.MaxValue;
                }
            }

            [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
            int NextOne() {
                if (_currentBucketPtr == null) {
                    return -1;
                }

                while (_currentRangeIndex < _rangesCount) {
                    while (_bucketIndexInRange < _currentRangeEnd - _currentRangeStart) {
                        var masked = _currentBucketPtr[_bucketIndexInRange] & _mask;
                        if (masked != 0) {
                            return math.tzcnt(masked);
                        }

                        _mask = ulong.MaxValue;
                        _bucketIndexInRange++;
                    }

                    _currentRangeIndex++;
                    if (_currentRangeIndex < _rangesCount) {
                        InitializeRange();
                    }
                }

                return -1;
            }
        }
    }
}