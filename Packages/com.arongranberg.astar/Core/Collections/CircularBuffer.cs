using System.Collections;
using System.Collections.Generic;
using Pathfinding.Pooling;
using Unity.Profiling;
using System.Runtime.CompilerServices;

namespace Pathfinding.Collections {
	/// <summary>
	/// Implements an efficient circular buffer that can be appended to in both directions.
	///
	/// See: <see cref="NativeCircularBuffer"/>
	/// </summary>
	public struct CircularBuffer<T> : IReadOnlyList<T>, IReadOnlyCollection<T> {
		internal T[] data;
		internal int head;
		int length;

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

		/// <summary>First item in the buffer, throws if the buffer is empty</summary>
		public readonly ref T First {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			[IgnoredByDeepProfiler]
			get {
				return ref data[head & (data.Length-1)];
			}
		}

		/// <summary>Last item in the buffer, throws if the buffer is empty</summary>
		public readonly ref T Last {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			[IgnoredByDeepProfiler]
			get {
				return ref data[(head+length-1) & (data.Length-1)];
			}
		}

		readonly int IReadOnlyCollection<T>.Count {
			[IgnoredByDeepProfiler]
			get {
				return length;
			}
		}

		/// <summary>Create a new buffer with the given capacity</summary>
		public CircularBuffer(int initialCapacity) : this()
        {
        }

        /// <summary>
        /// Create a new buffer using the given array as an internal store.
        /// This will take ownership of the given array.
        /// </summary>
        public CircularBuffer(T[] backingArray) : this()
        {
        }

        /// <summary>Resets the buffer's length to zero. Does not clear the current allocation</summary>
        public void Clear () {
        }

        /// <summary>Appends a list of items to the end of the buffer</summary>
        public void AddRange (List<T> items) {
        }

        /// <summary>Pushes a new item to the start of the buffer</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		[IgnoredByDeepProfiler]
		public void PushStart (T item) {
        }

        /// <summary>Pushes a new item to the end of the buffer</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		[IgnoredByDeepProfiler]
		public void PushEnd (T item) {
        }

        /// <summary>Pushes a new item to the start or the end of the buffer</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		[IgnoredByDeepProfiler]
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
        [IgnoredByDeepProfiler]
		public T Pop (bool fromStart) {
            return default;
        }

        /// <summary>Return either the first element or the last element</summary>
        public readonly T GetBoundaryValue (bool start) {
            return default;
        }

        /// <summary>Inserts an item at the given absolute index</summary>
        public void InsertAbsolute (int index, T item) {
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
			[MethodImpl(MethodImplOptions.AggressiveInlining)][IgnoredByDeepProfiler]
			readonly get {
#if UNITY_EDITOR
				if ((uint)index >= length) throw new System.ArgumentOutOfRangeException();
#endif
				return data[(index+head) & (data.Length-1)];
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)][IgnoredByDeepProfiler]
			set {
#if UNITY_EDITOR
				if ((uint)index >= length) throw new System.ArgumentOutOfRangeException();
#endif
				data[(index+head) & (data.Length-1)] = value;
			}
		}

		/// <summary>
		/// Indexes the buffer using absolute indices.
		/// When pushing to and popping from the buffer, the absolute indices do not change.
		/// So e.g. after doing PushStart(x) on an empty buffer, GetAbsolute(-1) will get the newly pushed element.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)][IgnoredByDeepProfiler]
		public readonly T GetAbsolute (int index) {
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)][IgnoredByDeepProfiler]
		public readonly void SetAbsolute (int index, T value) {
        }

        public void EnsureSize (int size) {
        }

        void Grow () {
        }

        void GrowTo(int requiresLength) {
        }

        /// <summary>Release the backing array of this buffer back into an array pool</summary>
        public void Pool () {
        }

        public IEnumerator<T> GetEnumerator()
        {
            return default;
        }

        IEnumerator IEnumerable.GetEnumerator () {
            return default;
        }

        public CircularBuffer<T> Clone()
        {
            return default;
        }
    }
}
