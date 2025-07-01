// #define DEBUG_ALLOCATOR
namespace Pathfinding.Collections {
	using Unity.Mathematics;
	using Unity.Collections;
	using Unity.Collections.LowLevel.Unsafe;

	/// <summary>
	/// A tiny slab allocator.
	/// Allocates spans of type T in power-of-two sized blocks.
	///
	/// Note: This allocator has no support for merging adjacent freed blocks.
	/// Therefore it is best suited for similarly sized allocations which are relatively small.
	///
	/// Can be used in burst jobs.
	///
	/// This is faster than allocating NativeArrays using the Temp allocator, and significantly faster
	/// than allocating them using the Persistent allocator.
	/// </summary>
	public struct SlabAllocator<T> where T : unmanaged {
		/// <summary>Allocation which is always invalid</summary>
		public const int InvalidAllocation = -2;
		/// <summary>Allocation representing a zero-length array</summary>
		public const int ZeroLengthArray = -1;

		// The max number of items we are likely to need to allocate comes from the connections array of each hierarchical node.
		// If you have a ton (thousands) of off-mesh links next to each other, then that array can get large.
		public const int MaxAllocationSizeIndex = 12;
		public const int MaxAllocationSize = 1 << MaxAllocationSizeIndex;

		internal static int SizeIndexToElements (int sizeIndex) {
            return default;
        }

        internal static int ElementsToSizeIndex(int nElements)
        {
            return default;
        }

        const uint UsedBit = 1u << 31;
		const uint AllocatedBit = 1u << 30;
		const uint LengthMask = AllocatedBit - 1;
		public bool IsDebugAllocator => false;

		[NativeDisableUnsafePtrRestriction]
		unsafe AllocatorData* data;

		struct AllocatorData {
			public UnsafeList<byte> mem;
			public unsafe fixed int freeHeads[MaxAllocationSizeIndex+1];
		}

		struct Header {
			public uint length;
		}

		struct NextBlock {
			public int next;
		}

		public bool IsCreated {
			get {
				unsafe {
					return data != null;
				}
			}
		}

		public int ByteSize {
			get {
				unsafe {
					return data->mem.Length;
				}
			}
		}

		public SlabAllocator(int initialCapacityBytes, AllocatorManager.AllocatorHandle allocator) : this()
        {
        }

        /// <summary>
        /// Frees all existing allocations.
        /// Does not free the underlaying unmanaged memory. Use <see cref="Dispose"/> for that.
        /// </summary>
        public void Clear () {
        }


        /// <summary>
        /// Get the span representing the given allocation.
        /// The returned array does not need to be disposed.
        /// It is only valid until the next call to <see cref="Allocate"/>, <see cref="Free"/> or <see cref="Dispose"/>.
        /// </summary>
        public UnsafeSpan<T> GetSpan (int allocatedIndex) {
            return default;
        }

        public void Realloc (ref int allocatedIndex, int nElements) {
        }

        /// <summary>
        /// Allocates an array big enough to fit the given values and copies them to the new allocation.
        /// Returns: An ID for the new allocation.
        /// </summary>
        public int Allocate (System.Collections.Generic.List<T> values) {
            return default;
        }

        /// <summary>
        /// Allocates an array big enough to fit the given values and copies them to the new allocation.
        /// Returns: An ID for the new allocation.
        /// </summary>
        public int Allocate (NativeList<T> values) {
            return default;
        }

        /// <summary>
        /// Allocates an array of type T with length nElements.
        /// Must later be freed using <see cref="Free"/> (or <see cref="Dispose)"/>.
        ///
        /// Returns: An ID for the new allocation.
        /// </summary>
        public int Allocate (int nElements) {
            return default;
        }

        /// <summary>Frees a single allocation</summary>
        public void Free(int allocatedIndex)
        {
        }

        public void CopyTo(SlabAllocator<T> other)
        {
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void CheckDisposed()
        {
        }

        /// <summary>Frees all unmanaged memory associated with this container</summary>
        public void Dispose()
        {
        }

        public List GetList (int allocatedIndex) {
            return default;
        }

        public ref struct List {
			public UnsafeSpan<T> span;
			SlabAllocator<T> allocator;
			// TODO: Can be derived from span
			public int allocationIndex;

			public List(SlabAllocator<T> allocator, int allocationIndex) : this()
            {
            }

            public void Add(T value)
            {
            }

            public void RemoveAt(int index)
            {
            }

            public void Clear () {
            }

            public int Length => span.Length;

			public ref T this[int index] {
				[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
				get {
					return ref span[index];
				}
			}
		}
	}

	public static class SlabListExtensions {
		public static void Remove<T>(ref this SlabAllocator<T>.List list, T value) where T : unmanaged, System.IEquatable<T> {
        }
    }
}
