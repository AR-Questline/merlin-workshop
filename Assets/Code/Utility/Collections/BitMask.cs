using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.Utility.Collections {
    [Serializable]
    public struct BitMask {
        const int BitsPerBucket = 64;
        const int IndexMask = 0b111111;
        const int BucketOffset = 6;
        
        [SerializeField, HideInInspector] int length;
        [SerializeField, HideInInspector] ulong[] mask;

        public readonly int Length => length;
        
        public BitMask(int length) {
            this.length = length;
            mask = new ulong[Bucket(length - 1) + 1];
        }

        public BitMask(bool[] bits) : this(bits.Length) {
            int bitIndex = 0;
            int bucketIndex = 0;
            do {
                ulong bucket = 0;
                for (int i = 0; i < BitsPerBucket && bitIndex < bits.Length; i++) {
                    bucket |= bits[bitIndex++] ? 1ul << i : 0;
                }
                mask[bucketIndex++] = bucket;
            } while (bitIndex < bits.Length);
        }

        public bool this[int index] {
            readonly get => (mask[Bucket(index)] & BucketMask(index)) != 0;
            set {
                if (value) {
                    mask[Bucket(index)] |= BucketMask(index);
                } else {
                    mask[Bucket(index)] &= ~BucketMask(index);
                }
            }
        }

        public void Zero() {
            Array.Clear(mask, 0, mask.Length);
        }

        public void One() {
            Array.Fill(mask, ulong.MaxValue);
        }

        public unsafe void AggregateOr(in BitMask other) {
            fixed (ulong* ptr = this.mask) {
                fixed (ulong* otherPtr = other.mask) {
                    int size = Bucket(math.min(length, other.length) - 1) + 1;
                    for (int i = 0; i < size; i++) {
                        *(ptr + i) |= *(otherPtr + i);
                    }
                }
            }
        }

        public unsafe void AggregateAnd(in BitMask other) {
            fixed (ulong* ptr = this.mask) {
                fixed (ulong* otherPtr = other.mask) {
                    int size = Bucket(math.min(length, other.length) - 1) + 1;
                    for (int i = 0; i < size; i++) {
                        *(ptr + i) &= *(otherPtr + i);
                    }
                }
            }
        }

        public int CountOnes() {
            int count = 0;
            for (int i = 0; i < mask.Length; i++) {
                count += math.countbits(mask[i]);
            }
            return count;
        }

        public int CountZeros() {
            return Length - CountOnes();
        }
        
        public ulong[] GetRawData() {
            return mask;
        }

        public void SetFromRawData(ulong[] data, int length) {
            mask = data;
            this.length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int Bucket(int index) {
            return index >> BucketOffset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int BucketIndex(int index) {
            return index & IndexMask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ulong BucketMask(int index) {
            return 1ul << BucketIndex(index);
        }
    }
}