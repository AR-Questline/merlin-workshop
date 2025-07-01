using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Awaken.PackageUtilities.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Debugging.MemorySnapshots;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.IL2CPP.CompilerServices;

namespace Awaken.Utility.LowLevel.Collections {
    [DebuggerTypeProxy(typeof(UnsafeInfiniteBitmaskDebugView))]
    public unsafe struct UnsafeInfiniteBitmask : IMemorySnapshotProvider {
        [NativeDisableUnsafePtrRestriction] UnsafeBitmask* _bitmask;
        readonly Allocator _allocator;

        public readonly bool IsCreated => _bitmask != null;
        
        public UnsafeInfiniteBitmask(uint initialElementsLength, Allocator allocator = Allocator.Persistent) {
            _bitmask = AllocationsTracker.Malloc<UnsafeBitmask>(1, allocator);
            *_bitmask = new UnsafeBitmask(initialElementsLength, allocator);
            _allocator = allocator;
        }

        public void Dispose() {
#if AR_DEBUG || UNITY_EDITOR
            if (_allocator == Allocator.Invalid) {
                UnityEngine.Debug.LogError($"Calling Dispose on already Disposed {nameof(UnsafeInfiniteBitmask)}");
            }
#endif
            if (_allocator > Allocator.None) {
                _bitmask->Dispose();
                AllocationsTracker.Free(_bitmask, _allocator);
            }

            this = default;
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public bool this[uint index] {
            get {
                var bucket = UnsafeBitmask.Bucket(index);
                if (bucket >= AllocationsTracker.Access(_bitmask)->BucketsLength) {
                    return false;
                }
                var masked = _bitmask->_masks[bucket] & UnsafeBitmask.IndexInBucketMask(index);
                return masked > 0;
            }
            set {
                var bucket = UnsafeBitmask.Bucket(index);

                if (bucket >= AllocationsTracker.Access(_bitmask)->BucketsLength) {
                    if (value) {
                        EnsureIndex(index);
                    } else {
                        return;
                    }
                }

                if (value) {
                    _bitmask->_masks[bucket] |= UnsafeBitmask.IndexInBucketMask(index);
                } else {
                    _bitmask->_masks[bucket] &= ~UnsafeBitmask.IndexInBucketMask(index);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public void UpNoChecks(uint index) {
            AllocationsTracker.Access(_bitmask)->Up(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public void DownNoChecks(uint index) {
            AllocationsTracker.Access(_bitmask)->Down(index);
        }

        public bool TryChange(uint index, bool value) {
            var bucket = UnsafeBitmask.Bucket(index);

            if (bucket >= AllocationsTracker.Access(_bitmask)->BucketsLength) {
                if (value) {
                    EnsureIndex(index);
                } else {
                    return false;
                }
            }

            var mask = UnsafeBitmask.IndexInBucketMask(index);
            var masked = _bitmask->_masks[bucket] & mask;
            var currentValue = masked > 0;
            if (currentValue == value) {
                return false;
            }
            if (value) {
                _bitmask->_masks[bucket] |= mask;
            } else {
                _bitmask->_masks[bucket] &= ~mask;
            }
            return true;
        }

        public void Flip(uint index) {
            var bucket = UnsafeBitmask.Bucket(index);

            if (bucket >= AllocationsTracker.Access(_bitmask)->BucketsLength) {
                EnsureIndex(index);
            }
            
            _bitmask->_masks[bucket] ^= UnsafeBitmask.IndexInBucketMask(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public uint CountOnes() {
            return AllocationsTracker.Access(_bitmask)->CountOnes();
        }

        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public void Zero() {
            AllocationsTracker.Access(_bitmask)->Zero();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AnySet() {
            return AllocationsTracker.Access(_bitmask)->AnySet();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false), Il2CppSetOption(Option.NullChecks, false)]
        public void All() {
            AllocationsTracker.Access(_bitmask)->All();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int FirstZero() {
            return AllocationsTracker.Access(_bitmask)->FirstZero();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int FirstOne() {
            return AllocationsTracker.Access(_bitmask)->FirstOne();
        }

        public void EnsureIndex(uint elementIndex) {
            AllocationsTracker.Access(_bitmask)->EnsureIndex(elementIndex);
        }

        public void EnsureCapacity(uint elementsLength) {
            AllocationsTracker.Access(_bitmask)->EnsureCapacity(elementsLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeBitmask.OnesEnumerator EnumerateOnes() {
            return AllocationsTracker.Access(_bitmask)->EnumerateOnes();
        }

        public int GetMemorySnapshot(Memory<MemorySnapshot> memoryBuffer, Memory<MemorySnapshot> ownPlace) {
            var bytes = (ulong)(IntPtr.Size + UnsafeUtility.SizeOf<Allocator>());
            ownPlace.Span[0] = new("InfiniteBitmask", bytes, bytes, memoryBuffer[..1]);
            _bitmask->GetMemorySnapshot("Bitmask", memoryBuffer[..1]);
            return 1;
        }

        internal sealed class UnsafeInfiniteBitmaskDebugView {
            UnsafeInfiniteBitmask _data;

            public UnsafeInfiniteBitmaskDebugView(UnsafeInfiniteBitmask data) {
                _data = data;
            }

            public bool[] Items {
                get {
                    bool[] result = new bool[_data._bitmask->_elementsLength];

                    var bucketsLength = _data._bitmask->BucketsLength;
                    var i = 0;
                    for (var j = 0; j < bucketsLength; ++j) {
                        var bucket = _data._bitmask->_masks[j];
                        for (int k = 0; k < 64; k++) {
                            result[i] = (bucket & ((ulong)1 << k)) > 0;
                            ++i;
                        }
                    }

                    return result;
                }
            }
        }
    }
}
