using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Awaken.PackageUtilities.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.Maths.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IL2CPP.CompilerServices;
using Unity.Mathematics;

namespace Awaken.Utility.LowLevel.Collections {
    [DebuggerDisplay("Buckets = {BucketsLength}"), DebuggerTypeProxy(typeof(UnsafeBitmaskDebugView))]
    public unsafe struct UnsafeBitmask : INamedLeafMemorySnapshotProvider {
        internal const uint IndexMask = 63;
        internal const int BucketOffset = 6;

        [NativeDisableUnsafePtrRestriction] internal ulong* _masks;
        internal ulong _lastMaskComplement;
        internal uint _elementsLength;
        internal Allocator _allocator;

        public readonly bool IsCreated => _masks != null;
        public readonly ushort BucketsLength => BucketLength(_elementsLength);
        public readonly uint ElementsLength => _elementsLength;

        public UnsafeBitmask(uint elementsLength, Allocator allocator) {
            _elementsLength = elementsLength;
            _allocator = allocator;
            _lastMaskComplement = ~0u; // Not real value, will be recalculated in separate method
            var bucketLength = BucketLength(_elementsLength);

            _masks = AllocationsTracker.Malloc<ulong>(bucketLength, _allocator);

            UnsafeUtility.MemClear(_masks, UnsafeUtility.SizeOf<ulong>() * bucketLength);
            RecalculateLastMaskComplement();
        }

        public void Dispose() {
#if AR_DEBUG || UNITY_EDITOR
            if (_allocator == Allocator.Invalid) {
                UnityEngine.Debug.LogError($"Calling Dispose on already Disposed {nameof(UnsafeBitmask)}");
            }
#endif
            if (_allocator > Allocator.None) {
                AllocationsTracker.Free(_masks, _allocator);
            }

            this = default;
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public readonly bool this[uint index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                var bucket = Bucket(index);
                CheckBucket(bucket);
                var masked = *AllocationsTracker.Access(_masks, bucket) & IndexInBucketMask(index);
                return masked > 0;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set {
                var bucket = Bucket(index);
                CheckBucket(bucket);
                if (value) {
                    *AllocationsTracker.Access(_masks, bucket) |= IndexInBucketMask(index);
                } else {
                    *AllocationsTracker.Access(_masks, bucket) &= ~IndexInBucketMask(index);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public readonly bool4 LoadSIMD(uint index) {
            var bucket = Bucket(index);
            CheckBucket(bucket);
            var indexInBucket = (int)IndexInBucket(index);
            var mask = 0b1111ul << indexInBucket;
            var masked = *AllocationsTracker.Access(_masks, bucket) & mask;
            var valueMask = (uint)(masked >> indexInBucket);
            return (valueMask & new uint4(1, 2, 4, 8)) > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public readonly void StoreSIMD(uint index, bool4 value) {
            var bucket = Bucket(index);
            CheckBucket(bucket);
            var indexInBucket = (int)IndexInBucket(index);
            var mask = 0b1111ul << indexInBucket;
            var maskedValue = ((ulong)math.bitmask(value)) << indexInBucket;
            ref var maskBucket = ref *AllocationsTracker.Access(_masks, bucket);
            maskBucket = (maskBucket & ~mask) | maskedValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public readonly void Up(uint index) {
            var bucket = Bucket(index);
            CheckBucket(bucket);
            *AllocationsTracker.Access(_masks, bucket) |= IndexInBucketMask(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public readonly void Down(uint index) {
            var bucket = Bucket(index);
            CheckBucket(bucket);
            *AllocationsTracker.Access(_masks, bucket) &= ~IndexInBucketMask(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public readonly void Down(uint index, uint length) {
            var startBucket = Bucket(index);
            var endBucket = Bucket(index + length - 1);

            CheckBucket(startBucket);
            CheckBucket(endBucket);

            var startMask = (1ul << (int)IndexInBucket(index)) - 1;
            var endIndex = (int)IndexInBucket(index + length);
            var endMask = endIndex != 0 ? ~((1ul << endIndex) - 1) : 0;

            if (startBucket == endBucket) {
                *AllocationsTracker.Access(_masks, startBucket) &= startMask | endMask;
            } else {
                *AllocationsTracker.Access(_masks, startBucket) &= startMask;
                *AllocationsTracker.Access(_masks, endBucket) &= endMask;

                for (var i = startBucket + 1; i < endBucket; i++) {
                    *AllocationsTracker.Access(_masks, i) = 0;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public readonly void Zero() {
            if (BucketsLength == 0) {
                return;
            }
            UnsafeUtility.MemClear(AllocationsTracker.Access(_masks), UnsafeUtility.SizeOf<ulong>() * BucketsLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public readonly void All() {
            if (BucketsLength == 0) {
                return;
            }
            UnsafeUtility.MemSet(AllocationsTracker.Access(_masks), byte.MaxValue, UnsafeUtility.SizeOf<ulong>() * BucketsLength);
            *AllocationsTracker.Access(_masks, BucketsLength - 1) &= ~_lastMaskComplement;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public readonly bool HasOne(uint index) {
            var bucket = Bucket(index);
            if (BucketsLength <= bucket) {
                return false;
            }

            var masked = *AllocationsTracker.Access(_masks, bucket) & IndexInBucketMask(index);
            return masked > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public readonly uint CountOnes() {
            var bucketsLength = BucketsLength;
            if (bucketsLength == 0) {
                return 0;
            }

            var count = 0u;
            for (var i = 0; i < bucketsLength; i++) {
                count += (uint)math.countbits(*AllocationsTracker.Access(_masks, i));
            }

            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public readonly uint CountOnes(uint start, uint end) {
            var startBucket = Bucket(start);
            var endBucket = Bucket(end);

            var startBucketIndex = IndexInBucket(start);
            var firstMask = ~((1ul << (int)startBucketIndex) - 1);

            var endBucketIndex = IndexInBucket(end);
            var endMask = (1ul << (int)endBucketIndex) - 1;

            if (endBucket == startBucket) {
                var mask = firstMask & endMask;
                return (uint)math.countbits(*AllocationsTracker.Access(_masks, startBucket) & mask);
            } else {
                var count = (uint)math.countbits(*AllocationsTracker.Access(_masks, startBucket) & firstMask);

                for (var i = startBucket + 1; i < endBucket; i++) {
                    count += (uint)math.countbits(*AllocationsTracker.Access(_masks, i));
                }

                count += (uint)math.countbits(*AllocationsTracker.Access(_masks, endBucket) & endMask);

                return count;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool AnySet() {
            var bucketsLength = BucketsLength;
            for (var i = 0; i < bucketsLength; i++) {
                if (math.countbits(*AllocationsTracker.Access(_masks, i)) > 0) {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool AllSet() {
            var bucketsLength = BucketsLength;
            for (var i = 0; i < bucketsLength - 1; i++) {
                if (*AllocationsTracker.Access(_masks, i) != ulong.MaxValue) {
                    return false;
                }
            }

            return (*AllocationsTracker.Access(_masks, bucketsLength - 1) | _lastMaskComplement) == ulong.MaxValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool NoneSet() {
            var bucketsLength = BucketsLength;
            for (var i = 0; i < bucketsLength - 1; i++) {
                if (*AllocationsTracker.Access(_masks, i) != 0) {
                    return false;
                }
            }

            return *AllocationsTracker.Access(_masks, bucketsLength - 1) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool AllSame() {
            var bucketsLength = BucketsLength;
            for (var i = 0; i < bucketsLength - 1; i++) {
                if ((*AllocationsTracker.Access(_masks, i) != 0) & (*AllocationsTracker.Access(_masks, i) != ulong.MaxValue)) {
                    return false;
                }

                if ((*AllocationsTracker.Access(_masks, i) & 1) != (*AllocationsTracker.Access(_masks, i+1) & 1)) {
                    return false;
                }
            }

            var lastMask = *AllocationsTracker.Access(_masks, bucketsLength - 1);
            return !((lastMask != 0) & ((lastMask | _lastMaskComplement) != ulong.MaxValue));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int FirstZero() {
            var bucketsLength = BucketsLength;
            if (bucketsLength == 0) {
                return -1;
            }

            var lastBucket = bucketsLength - 1;
            for (var i = 0; i < lastBucket; i++) {
                var mask = *AllocationsTracker.Access(_masks, i);
                if (mask != ulong.MaxValue) {
                    return i * 64 + math.tzcnt(~mask);
                }
            }

            var lastMask = *AllocationsTracker.Access(_masks, lastBucket);
            if ((lastMask | _lastMaskComplement) != ulong.MaxValue) {
                return lastBucket * 64 + math.tzcnt(~lastMask);
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int FirstOne() {
            var bucketsLength = BucketsLength;
            for (var i = 0; i < bucketsLength; i++) {
                var mask = *AllocationsTracker.Access(_masks, i);
                if (mask != 0) {
                    return i * 64 + math.tzcnt(mask);
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int LastOne() {
            var bucketsLength = BucketsLength;

            for (var i = bucketsLength - 1; i >= 0; i--) {
                var mask = *AllocationsTracker.Access(_masks, i);
                if (mask != 0) {
                    return i * 64 + (63 - math.lzcnt(mask));
                }
            }

            return -1;
        }

        public static void Or(ref UnsafeBitmask left, in UnsafeBitmask right) {
            if (left.ElementsLength < right.ElementsLength) {
                Log.Important?.Error($"{nameof(Or)} can operate only on the bitmasks if left is not smaller than right bitmask. Using fallback function {nameof(OrWithChangeSize)}");
                OrWithChangeSize(ref left, in right);
                return;
            }

            var rightBucketsLength = right.BucketsLength;
            for (int i = 0; i < rightBucketsLength; i++) {
                left._masks[i] |= right._masks[i];
            }
        }

        public static void OrWithChangeSize(ref UnsafeBitmask left, in UnsafeBitmask right) {
            if (left.ElementsLength >= right.ElementsLength) {
                Or(ref left, in right);
                return;
            }

            var rightBucketsLength = right.BucketsLength;
            var maxBucketsCount = math.max((uint)left.BucketsLength, rightBucketsLength);
            left.EnsureCapacity(maxBucketsCount * 64);
            for (int i = 0; i < rightBucketsLength; i++) {
                left._masks[i] |= right._masks[i];
            }
        }

        public static UnsafeBitmask Xor(in UnsafeBitmask left, in UnsafeBitmask right, Allocator allocator) {
            if (left.ElementsLength != right.ElementsLength) {
                Log.Important?.Error($"{nameof(Xor)} can operate only on the bitmasks of the same size. Using fallback function {nameof(XorAssumePadZeros)}");
                return XorAssumePadZeros(in left, in right, allocator);
            }

            var elementsCount = left.ElementsLength;
            if (elementsCount == 0) {
                throw new Exception($"Using two invalid bitmasks with {nameof(ElementsLength)} = 0.");
            }

            var bucketsCount = left.BucketsLength;
            var resultBitmask = new UnsafeBitmask(elementsCount, allocator);
            for (uint i = 0; i < bucketsCount; i++) {
                resultBitmask._masks[i] = left._masks[i] ^ right._masks[i];
            }

            return resultBitmask;
        }

        /// <summary>
        /// Xor which can operate on bitmasks of different sizes by assuming that smaller bitmask is padded with zeros up to
        /// the size of bigger bitmask. Uses <see cref="Xor"/> if sizes are equal.
        /// </summary>
        /// <returns>New bitmask with bit 1 where the value of bitmasks is different</returns>
        public static UnsafeBitmask XorAssumePadZeros(in UnsafeBitmask left, in UnsafeBitmask right, Allocator allocator) {
            if (left.ElementsLength == right.ElementsLength) {
                return Xor(in left, in right, allocator);
            }

            if (right.ElementsLength == 0) {
                throw new Exception($"{nameof(right)} bitmask is invalid with {nameof(ElementsLength)} = 0.");
            }

            if (left.ElementsLength == 0) {
                throw new Exception($"{nameof(left)} bitmask is invalid with {nameof(ElementsLength)} = 0.");
            }

            var rightBucketsLength = right.BucketsLength;
            var leftBucketsLength = left.BucketsLength;
            uint smallerBucketsLength, biggerBucketsLength;
            ulong* smallerMasks, biggerMasks;
            if (rightBucketsLength > leftBucketsLength) {
                smallerBucketsLength = leftBucketsLength;
                biggerBucketsLength = rightBucketsLength;
                smallerMasks = left._masks;
                biggerMasks = right._masks;
            } else {
                smallerBucketsLength = rightBucketsLength;
                biggerBucketsLength = leftBucketsLength;
                smallerMasks = right._masks;
                biggerMasks = left._masks;
            }

            var resultBitmask = new UnsafeBitmask(biggerBucketsLength * 64, allocator);
            for (uint i = 0; i < smallerBucketsLength; i++) {
                resultBitmask._masks[i] = smallerMasks[i] ^ biggerMasks[i];
            }

            for (uint i = smallerBucketsLength; i < biggerBucketsLength; i++) {
                resultBitmask._masks[i] = biggerMasks[i];
            }

            return resultBitmask;
        }
        
        public static UnsafeBitmask And(in UnsafeBitmask left, in UnsafeBitmask right, Allocator allocator) {
            if (left.ElementsLength != right.ElementsLength) {
                Log.Important?.Error($"{nameof(And)} can operate only on the bitmasks of the same size. Using fallback function {nameof(AndAssumePadZeros)}");
                return AndAssumePadZeros(in left, in right, allocator);
            }

            var elementsCount = left.ElementsLength;
            if (elementsCount == 0) {
                throw new Exception($"Using two invalid bitmasks with {nameof(ElementsLength)} = 0.");
            }

            var bucketsCount = left.BucketsLength;
            var resultBitmask = new UnsafeBitmask(elementsCount, allocator);
            for (uint i = 0; i < bucketsCount; i++) {
                resultBitmask._masks[i] = left._masks[i] & right._masks[i];
            }
            return resultBitmask;
        }

        public static void And(ref UnsafeBitmask left, in UnsafeBitmask right) {
            var elementsCount = left.ElementsLength;
            if (elementsCount != right.ElementsLength) {
                throw new Exception($"{nameof(And)} with ref left parameter can operate only on the bitmasks of the same size. {elementsCount} != {right.ElementsLength}");
            }
            var bucketsCount = left.BucketsLength;
            for (uint i = 0; i < bucketsCount; i++) {
                left._masks[i] &= right._masks[i];
            }
        }
        
        public static UnsafeBitmask AndWithInvertRightBitmask(in UnsafeBitmask left, in UnsafeBitmask right, Allocator allocator) {
            var elementsCount = left.ElementsLength;
            if (elementsCount != right.ElementsLength) {
                throw new Exception($"{nameof(AndWithInvertRightBitmask)} can operate only on the bitmasks of the same size. {elementsCount} != {right.ElementsLength}");
            }

            if (elementsCount == 0) {
                throw new Exception($"Using two invalid bitmasks with {nameof(ElementsLength)} = 0.");
            }

            var bucketsCount = left.BucketsLength;
            var resultBitmask = new UnsafeBitmask(elementsCount, allocator);
            for (uint i = 0; i < bucketsCount; i++) {
                resultBitmask._masks[i] = (left._masks[i]) & (~right._masks[i]);
            }
            return resultBitmask;
        }
        
        public static void AndWithInvertRightBitmask(ref UnsafeBitmask left, in UnsafeBitmask right) {
            var elementsCount = left.ElementsLength;
            if (elementsCount != right.ElementsLength) {
                throw new Exception($"{nameof(AndWithInvertRightBitmask)} can operate only on the bitmasks of the same size. {elementsCount} != {right.ElementsLength}");
            }

            if (elementsCount == 0) {
                throw new Exception($"Using two invalid bitmasks with {nameof(ElementsLength)} = 0.");
            }

            var bucketsCount = left.BucketsLength;
            for (uint i = 0; i < bucketsCount; i++) {
                left._masks[i] &= (~right._masks[i]);
            }
        }
        
        public static UnsafeBitmask AndAssumePadZeros(in UnsafeBitmask left, in UnsafeBitmask right, Allocator allocator) {
            if (left.ElementsLength == right.ElementsLength) {
                return And(in left, in right, allocator);
            }

            if (right.ElementsLength == 0) {
                throw new Exception($"{nameof(right)} bitmask is invalid with {nameof(ElementsLength)} = 0.");
            }

            if (left.ElementsLength == 0) {
                throw new Exception($"{nameof(left)} bitmask is invalid with {nameof(ElementsLength)} = 0.");
            }

            var rightBucketsLength = right.BucketsLength;
            var leftBucketsLength = left.BucketsLength;
            uint smallerBucketsLength, biggerBucketsLength;
            ulong* smallerMasks, biggerMasks;
            if (rightBucketsLength > leftBucketsLength) {
                smallerBucketsLength = leftBucketsLength;
                biggerBucketsLength = rightBucketsLength;
                smallerMasks = left._masks;
                biggerMasks = right._masks;
            } else {
                smallerBucketsLength = rightBucketsLength;
                biggerBucketsLength = leftBucketsLength;
                smallerMasks = right._masks;
                biggerMasks = left._masks;
            }

            var resultBitmask = new UnsafeBitmask(biggerBucketsLength * 64, allocator);
            for (uint i = 0; i < smallerBucketsLength; i++) {
                resultBitmask._masks[i] = smallerMasks[i] & biggerMasks[i];
            }

            for (uint i = smallerBucketsLength; i < biggerBucketsLength; i++) {
                resultBitmask._masks[i] = biggerMasks[i];
            }

            return resultBitmask;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAtSwapBack(uint removeAtIndex) {
            Asserts.IndexInRange(removeAtIndex, _elementsLength);

            var bucketsCount = BucketsLength;
            var removedElementBucketIndex = Bucket(removeAtIndex);
            int removedElementIndexInBucket = (int)IndexInBucket(removeAtIndex);
            var lastBucketIndex = bucketsCount - 1;
            var lastBucketLastElementIndex = (_elementsLength - 1) % 64;
            var lastBucketLastElementBitMask = (1ul << (int)lastBucketLastElementIndex);
            if ((removedElementBucketIndex != lastBucketIndex || removedElementIndexInBucket != lastBucketLastElementIndex)) {
                ulong lastBitValue = (_masks[lastBucketIndex] & lastBucketLastElementBitMask) > 0 ? 1ul : 0ul;
                var modifiedBucketWithUnsetRemovedElement = _masks[removedElementBucketIndex] & (~(1ul << removedElementIndexInBucket));
                _masks[removedElementBucketIndex] = modifiedBucketWithUnsetRemovedElement | (lastBitValue << removedElementIndexInBucket);
            }
            _masks[lastBucketIndex] &= ~lastBucketLastElementBitMask;

            _elementsLength--;
            RecalculateLastMaskComplement();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly UnsafeBitmask DeepClone(Allocator allocator) {
            var copy = new UnsafeBitmask(_elementsLength, allocator);
            UnsafeUtility.MemCpy(copy._masks, AllocationsTracker.Access(_masks), BucketsLength * sizeof(ulong));
            return copy;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyFrom(in UnsafeBitmask other) {
            var copyBucketsLength = other.BucketsLength < BucketsLength ? other.BucketsLength : BucketsLength;
            UnsafeUtility.MemCpy(AllocationsTracker.Access(_masks), AllocationsTracker.Access(other._masks), UnsafeUtility.SizeOf<ulong>() * copyBucketsLength);
        }

        public void Clear() {
            UnsafeUtility.MemClear(AllocationsTracker.Access(_masks), BucketsLength * sizeof(ulong));
        }

        public void EnsureIndex(uint elementIndex) {
            EnsureCapacity(elementIndex + 1u);
        }

        public void EnsureCapacity(uint elementsLength) {
            var neededBucketsCount = Bucket(elementsLength - 1) + 1;

            if (BucketsLength < neededBucketsCount) {
                Resize(elementsLength);
            } else {
                _elementsLength = math.max(elementsLength, _elementsLength);
            }

            RecalculateLastMaskComplement();
        }

        public void Invert() {
            uint bucketsCount = BucketsLength;
            for (uint i = 0; i < bucketsCount; i++) {
                ref var currentMask = ref *AllocationsTracker.Access(_masks, i);
                currentMask = ~currentMask;
            }

            RecalculateLastMaskComplement();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly OnesEnumerator EnumerateOnes() => new OnesEnumerator(this);

        void Resize(uint elementsLength) {
            var oldBucketLength = BucketsLength;
            _elementsLength = elementsLength;
            var newBucketLength = BucketLength(elementsLength);

            var newMask = AllocationsTracker.Malloc<ulong>(newBucketLength, _allocator);

            UnsafeUtility.MemCpy(newMask, _masks, UnsafeUtility.SizeOf<ulong>() * oldBucketLength);
            UnsafeUtility.MemClear(newMask + oldBucketLength, UnsafeUtility.SizeOf<ulong>() * (newBucketLength - oldBucketLength));

            AllocationsTracker.Free(_masks, _allocator);

            _masks = newMask;
        }

        void RecalculateLastMaskComplement() {
            var complementIndex = _elementsLength % 64;
            _lastMaskComplement = complementIndex == 0 ? 0ul : ~((1ul << (int)complementIndex) - 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly void CheckBucket(uint bucket) {
            Asserts.IndexInRange(bucket, BucketsLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ushort Bucket(uint index) {
            return (ushort)(index >> BucketOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ushort BucketLength(uint elementsCount) {
            return (ushort)(((elementsCount - 1) >> BucketOffset) + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint IndexInBucket(uint index) {
            return index & IndexMask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong IndexInBucketMask(uint index) {
            return 1ul << (int)IndexInBucket(index);
        }

        public void GetMemorySnapshot(string name, Memory<MemorySnapshot> ownPlace) {
            var bytes = (ulong)(UnsafeUtility.SizeOf<ulong>() * BucketsLength);
            ownPlace.Span[0] = new MemorySnapshot(name, bytes, bytes);
        }

        public struct SerializationAccess {
            public static ref ulong* Ptr(ref UnsafeBitmask bitmask) => ref bitmask._masks;
            public static ref uint Length(ref UnsafeBitmask bitmask) => ref bitmask._elementsLength;
            public static ref Allocator Allocator(ref UnsafeBitmask bitmask) => ref bitmask._allocator;
        }

        public ref struct OnesEnumerator {
            readonly ulong* _masks;
            ulong _mask;
            int _index;
            readonly ushort _bucketsLength;
            ushort _bucketIndex;

            public OnesEnumerator(in UnsafeBitmask data) {
                _masks = data._masks;
                _bucketsLength = data.BucketsLength;
                _bucketIndex = 0;
                _mask = ulong.MaxValue;
                _index = -1;
            }

            public bool MoveNext() {
                _index = NextOne();
                if (_index != -1) {
                    _mask ^= 1ul << _index;
                    return true;
                }

                return false;
            }

            public uint Current => (uint)(_index + _bucketIndex * 64);

            public OnesEnumerator GetEnumerator() => this;

            int NextOne() {
                for (; _bucketIndex < _bucketsLength; _bucketIndex++) {
                    var masked = *AllocationsTracker.Access(_masks, _bucketIndex) & _mask;
                    if (masked != 0) {
                        return math.tzcnt(masked);
                    }

                    _mask = ulong.MaxValue;
                }

                return -1;
            }
        }

        internal sealed class UnsafeBitmaskDebugView {
            UnsafeBitmask _data;

            public UnsafeBitmaskDebugView(UnsafeBitmask data) {
                _data = data;
            }

            public bool[] Items {
                get {
                    bool[] result = new bool[_data._elementsLength];

                    var i = 0;
                    var bucketsLength = _data.BucketsLength;
                    for (var j = 0; j < bucketsLength; ++j) {
                        var bucket = _data._masks[j];
                        for (int k = 0; (i < _data._elementsLength) & (k < 64); k++) {
                            result[i] = (bucket & ((ulong)1 << k)) > 0;
                            ++i;
                        }
                    }

                    return result;
                }
            }
        }
    }

    [BurstCompile]
    public static class UnsafeBitmaskExtensions {
        [BurstCompile]
        public static void ToIndicesOfOneArray(in this UnsafeBitmask bitmask, Allocator allocator, out UnsafeArray<uint> result) {
            result = new UnsafeArray<uint>(bitmask.CountOnes(), allocator);
            var i = 0u;
            foreach (var index in bitmask.EnumerateOnes()) {
                result[i++] = index;
            }
        }
    }
}