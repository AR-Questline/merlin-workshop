using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;
using Unity.Profiling;

namespace Pathfinding.Collections {
	/// <summary>
	/// Implements an efficient circular buffer that can be appended to in both directions.
	///
	/// See: <see cref="CircularBuffer"/>
	/// </summary>
	public struct NativeCircularBuffer<T> : IReadOnlyList<T>, IReadOnlyCollection<T> where T : unmanaged {
		[NativeDisableUnsafePtrRestriction]
		internal unsafe T* data;
		internal int head;
		int length;
		/// <summary>Capacity of the allocation minus 1. Invariant: (a power of two) minus 1</summary>
		int capacityMask;

		/// <summary>The allocator used to create the internal buffer.</summary>
		public AllocatorManager.AllocatorHandle Allocator;
		/// <summary>Number of items in the buffer</summary>
		public readonly int Length {
			[IgnoredByDeepProfiler]
			get {
				return length;
			}
		}

		/// <summary>Absolute index of the first item in the buffer, may be negative or greater than <see cref="Length"/></summary>
		public readonly int AbsoluteStartIndex => head;
		/// <summary>Absolute index of the last item in the buffer, may be negative or greater than <see cref="Length"/></summary>
		public readonly int AbsoluteEndIndex => head + length - 1;

		/// <summary>First item in the buffer throws if the buffer is empty</summary>
		public readonly ref T First {
			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			[IgnoredByDeepProfiler]
			get {
				unsafe {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
					if (length == 0) throw new System.InvalidOperationException();
#endif
					return ref data[head & capacityMask];
				}
			}
		}

		/// <summary>Last item in the buffer, throws if the buffer is empty</summary>
		public readonly ref T Last {
			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			[IgnoredByDeepProfiler]
			get {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
				if (length == 0) throw new System.InvalidOperationException();
#endif
				unsafe { return ref data[(head+length-1) & capacityMask]; }
			}
		}

		readonly int IReadOnlyCollection<T>.Count => Length;

		public readonly bool IsCreated {
			get {
				unsafe {
					return data != null;
				}
			}
		}

		/// <summary>Create a new empty buffer</summary>

		public NativeCircularBuffer(AllocatorManager.AllocatorHandle allocator) : this()
        {
        }

        /// <summary>Create a new buffer with the given capacity</summary>
        public NativeCircularBuffer(int initialCapacity, AllocatorManager.AllocatorHandle allocator) : this()
        {
        }

        unsafe public NativeCircularBuffer(CircularBuffer<T> buffer, out ulong gcHandle) : this(buffer.data, buffer.head, buffer.Length, out gcHandle)
        {
        }

        unsafe public NativeCircularBuffer(T[] data, int head, int length, out ulong gcHandle) : this()
        {
	        gcHandle = default;
        }

        /// <summary>Resets the buffer's length to zero. Does not clear the current allocation</summary>
        public void Clear () {
        }

        /// <summary>Appends a list of items to the end of the buffer</summary>
        public void AddRange (List<T> items) {
        }

        /// <summary>Pushes a new item to the start of the buffer</summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		[IgnoredByDeepProfiler]
		public void PushStart (T item) {
        }

        /// <summary>Pushes a new item to the end of the buffer</summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		[IgnoredByDeepProfiler]
		public void PushEnd (T item) {
        }

        /// <summary>Pushes a new item to the start or the end of the buffer</summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public void Push (bool toStart, T item) {
        }

        /// <summary>Removes and returns the first element</summary>
        [IgnoredByDeepProfiler]
		public T PopStart () {
            return default;
        }

        /// <summary>Removes and returns the last element</summary>
        [IgnoredByDeepProfiler]
		public T PopEnd () {
            return default;
        }

        /// <summary>Pops either from the start or from the end of the buffer</summary>
        public T Pop (bool fromStart) {
            return default;
        }

        /// <summary>Return either the first element or the last element</summary>
        public readonly T GetBoundaryValue (bool start) {
            return default;
        }

        /// <summary>Lowers the length of the buffer to the given value, and does nothing if the given value is greater or equal to the current length</summary>

        public void TrimTo (int length) {
        }

        /// <summary>Removes toRemove items from the buffer, starting at startIndex, and then inserts the toInsert items at startIndex</summary>

        public void Splice (int startIndex, int toRemove, List<T> toInsert) {
        }

        /// <summary>Like <see cref="Splice"/>, but startIndex is an absolute index</summary>

        public void SpliceAbsolute (int startIndex, int toRemove, List<T> toInsert) {
        }

        /// <summary>Like <see cref="Splice"/>, but the newly inserted items are left in an uninitialized state</summary>
        public void SpliceUninitialized (int startIndex, int toRemove, int toInsert) {
        }

        /// <summary>Like <see cref="SpliceUninitialized"/>, but startIndex is an absolute index</summary>
        public void SpliceUninitializedAbsolute (int startIndex, int toRemove, int toInsert) {
        }

        void MoveAbsolute (int startIndex, int endIndex, int deltaIndex) {
        }

        /// <summary>Indexes the buffer, with index 0 being the first element</summary>
        public T this[int index] {
			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			[IgnoredByDeepProfiler]
			readonly get {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
				if ((uint)index >= length) throw new System.ArgumentOutOfRangeException();
#endif
				unsafe {
					return data[(index+head) & capacityMask];
				}
			}
			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			[IgnoredByDeepProfiler]
			set {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
				if ((uint)index >= length) throw new System.ArgumentOutOfRangeException();
#endif
				unsafe {
					data[(index+head) & capacityMask] = value;
				}
			}
		}

		/// <summary>
		/// Indexes the buffer using absolute indices.
		/// When pushing to and popping from the buffer, the absolute indices do not change.
		/// So e.g. after doing PushStart(x) on an empty buffer, GetAbsolute(-1) will get the newly pushed element.
		/// </summary>
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		[IgnoredByDeepProfiler]
		public readonly T GetAbsolute (int index) {
            return default;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
		void Grow () {
        }

        /// <summary>Releases the unmanaged memory held by this container</summary>
        public void Dispose () {
        }

        public IEnumerator<T> GetEnumerator () {
            return default;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return default;
        }

        public NativeCircularBuffer<T> Clone()
        {
            return default;
        }
    }
}
