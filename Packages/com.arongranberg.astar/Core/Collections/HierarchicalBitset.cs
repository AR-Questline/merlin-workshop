using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Pathfinding.Collections {
	/// <summary>
	/// Thread-safe hierarchical bitset.
	///
	/// Stores an array of bits. Each bit can be set or cleared individually from any thread.
	///
	/// Note: Setting the capacity is not thread-safe, nor is iterating over the bitset while it is being modified.
	/// </summary>
	[BurstCompile]
	public struct HierarchicalBitset {
		UnsafeSpan<ulong> l1;
		UnsafeSpan<ulong> l2;
		UnsafeSpan<ulong> l3;
		Allocator allocator;

		const int Log64 = 6;

		public HierarchicalBitset (int size, Allocator allocator) : this()
        {
        }

        public bool IsCreated => Capacity > 0;

		public void Dispose () {
        }

        public int Capacity {
			get {
				return l1.Length << Log64;
			}
			set {
				if (value < Capacity) throw new System.ArgumentException("Shrinking the bitset is not supported");
				if (value == Capacity) return;
				var b = new HierarchicalBitset(value, allocator);

				// Copy the old data
				l1.CopyTo(b.l1);
				l2.CopyTo(b.l2);
				l3.CopyTo(b.l3);

				Dispose();
				this = b;
			}
		}

		/// <summary>Number of set bits in the bitset</summary>
		public int Count () {
            return default;
        }

        /// <summary>True if the bitset is empty</summary>
        public bool IsEmpty {
			get {
				for (int i = 0; i < l3.Length; i++) {
					if (l3[i] != 0) return false;
				}
				return true;
			}
		}

		/// <summary>Clear all bits</summary>
		public void Clear () {
        }

        public void GetIndices (NativeList<int> result) {
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		static bool SetAtomic (ref UnsafeSpan<ulong> span, int index) {
            return default;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        static bool ResetAtomic(ref UnsafeSpan<ulong> span, int index)
        {
            return default;
        }

        /// <summary>Get the value of a bit</summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool Get(int index)
        {
            return default;
        }

        /// <summary>Set a given bit to 1</summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void Set(int index)
        {
        }

        /// <summary>Set a given bit to 0</summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void Reset(int index)
        {
        }

        /// <summary>Get an iterator over all set bits.</summary>
        /// <param name="scratchBuffer">A buffer to use for temporary storage. A slice of this buffer will be returned on each iteration, filled with the indices of the set bits.</param>
        public Iterator GetIterator(UnsafeSpan<int> scratchBuffer)
        {
            return default;
        }

        [BurstCompile]
        public struct Iterator : IEnumerator<UnsafeSpan<int>>, IEnumerable<UnsafeSpan<int>>
        {
            HierarchicalBitset bitSet;
            UnsafeSpan<int> result;
            int resultCount;
            int l3index;
            int l3bitIndex;
            int l2bitIndex;

            public UnsafeSpan<int> Current => result.Slice(0, resultCount);

            object IEnumerator.Current => throw new System.NotImplementedException();

            public void Reset() => throw new System.NotImplementedException();

            public void Dispose()
            {
            }

            public IEnumerator<UnsafeSpan<int>> GetEnumerator() => this;

            IEnumerator IEnumerable.GetEnumerator() => throw new System.NotImplementedException();

            static int l2index(int l3index, int l3bitIndex) => (l3index << Log64) + l3bitIndex;
            static int l1index(int l2index, int l2bitIndex) => (l2index << Log64) + l2bitIndex;

            public Iterator(HierarchicalBitset bitSet, UnsafeSpan<int> result) : this()
            {
            }

            public bool MoveNext()
            {
                return default;
            }

            [BurstCompile]
            public static bool MoveNextBurst(ref Iterator iter)
            {
                return default;
            }

            // Inline
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            bool MoveNextInternal()
            {
                return default;
            }
        }
	}
}
