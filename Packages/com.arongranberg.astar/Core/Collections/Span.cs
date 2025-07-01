using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.CompilerServices;
using Awaken.PackageUtilities.Collections;

namespace Pathfinding.Collections {
	/// <summary>
	/// Replacement for System.Span which is compatible with earlier versions of C#.
	///
	/// Warning: These spans do not in any way guarantee that the memory they refer to is valid. It is up to the user to make sure
	/// the memory is not deallocated before usage. It should never be used to refer to managed heap memory without pinning it, since unpinned managed memory can be moved by some runtimes.
	///
	/// This has several benefits over e.g. UnsafeList:
	/// - It is faster to index into a span than into an UnsafeList, especially from C#. In fact, indexing into an UnsafeSpan is as fast as indexing into a native C# array.
	///    - As a comparison, indexing into a NativeArray can easily be 10x slower, and indexing into an UnsafeList is at least a few times slower.
	/// - You can create a UnsafeSpan from a C# array by pinning it.
	/// - It can be sliced efficiently.
	/// - It supports ref returns for the indexing operations.
	/// </summary>
	public readonly struct UnsafeSpan<T> where T : unmanaged {
		[NativeDisableUnsafePtrRestriction]
		internal readonly unsafe T* ptr;
		internal readonly uint length;

		/// <summary>Number of elements in this span</summary>
		public int Length => (int)length;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe UnsafeSpan(void* ptr, int length) : this()
        {
        }

        /// <summary>
        /// Creates a new UnsafeSpan from a C# array.
        /// The array is pinned to ensure it does not move while the span is in use.
        ///
        /// You must unpin the pinned memory using UnsafeUtility.ReleaseGCObject when you are done with the span.
        /// </summary>
        public unsafe UnsafeSpan(T[] data, out ulong gcHandle) : this()
        {
	        gcHandle = default;
        }

        /// <summary>
        /// Creates a new UnsafeSpan from a 2D C# array.
        /// The array is pinned to ensure it does not move while the span is in use.
        ///
        /// You must unpin the pinned memory using UnsafeUtility.ReleaseGCObject when you are done with the span.
        /// </summary>
        public unsafe UnsafeSpan(T[,] data, out ulong gcHandle) : this()
        {
	        gcHandle = default;
        }

        /// <summary>
        /// Allocates a new UnsafeSpan with the specified length.
        /// The memory is not initialized.
        ///
        /// You are responsible for freeing the memory using the same allocator when you are done with it.
        /// </summary>
        public UnsafeSpan(Allocator allocator, int length) : this()
        {
        }

        public ref T this[int index] {
			// With aggressive inlining the performance of indexing is essentially the same as indexing into a native C# array
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			[IgnoredByDeepProfiler]
			get {
				unsafe {
					if ((uint)index >= length) throw new System.IndexOutOfRangeException();
					Unity.Burst.CompilerServices.Hint.Assume(ptr != null);
					return ref *(ptr + index);
				}
			}
		}

		public ref T this[uint index] {
			// With aggressive inlining the performance of indexing is essentially the same as indexing into a native C# array
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			[IgnoredByDeepProfiler]
			get {
				unsafe {
					if (index >= length) throw new System.IndexOutOfRangeException();
					Unity.Burst.CompilerServices.Hint.Assume(ptr != null);
					Unity.Burst.CompilerServices.Hint.Assume(ptr + index != null);
					return ref *(ptr + index);
				}
			}
		}

		/// <summary>
		/// Returns a copy of this span, but with a different data-type.
		/// The new data-type must have the same size as the old one.
		///
		/// In burst, this should effectively be a no-op, except possibly a branch.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public UnsafeSpan<U> Reinterpret<U> () where U : unmanaged {
            return default;
        }

        /// <summary>
        /// Returns a copy of this span, but with a different data-type.
        /// The new data-type does not need to have the same size as the old one.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public UnsafeSpan<U> Reinterpret<U>(int expectedOriginalTypeSize) where U : unmanaged {
            return default;
        }

        /// <summary>
        /// Creates a new span which is a slice of this span.
        /// The new span will start at the specified index and have the specified length.
        /// </summary>
        public UnsafeSpan<T> Slice (int start, int length) {
            return default;
        }

        /// <summary>
        /// Creates a new span which is a slice of this span.
        /// The new span will start at the specified index and continue to the end of this span.
        /// </summary>
        public UnsafeSpan<T> Slice (int start) {
            return default;
        }

        /// <summary>Copy the range [startIndex,startIndex+count) to [toIndex,toIndex+count)</summary>
        public void Move (int startIndex, int toIndex, int count) {
        }

        /// <summary>
        /// Copies the memory of this span to another span.
        /// The other span must be large enough to hold the contents of this span.
        ///
        /// Note: Assumes the other span does not alias this one.
        /// </summary>
        public void CopyTo (UnsafeSpan<T> other) {
        }

        /// <summary>Appends all elements in this span to the given list</summary>
        public void CopyTo (List<T> buffer) {
        }

        /// <summary>
        /// Creates a new copy of the span allocated using the given allocator.
        ///
        /// You are responsible for freeing this memory using the same allocator when you are done with it.
        /// </summary>
        public UnsafeSpan<T> Clone (Allocator allocator) {
            return default;
        }

        /// <summary>Converts the span to a managed array</summary>
        public T[] ToArray()
        {
            return default;
        }

        /// <summary>
        /// Moves this data to a new NativeArray.
        ///
        /// This transfers ownership of the memory to the NativeArray, without any copying.
        /// The NativeArray must be disposed when you are done with it.
        ///
        /// Warning: This span must have been allocated using the specified allocator.
        /// </summary>
        public unsafe NativeArray<T> MoveToNativeArray(Allocator allocator)
        {
            return default;
        }

        /// <summary>
        /// Frees the underlaying memory.
        ///
        /// Warning: The span must have been allocated using the specified allocator.
        ///
        /// Warning: You must never use this span (or any other span referencing the same memory) again after calling this method.
        /// </summary>
        public unsafe void Free(Allocator allocator)
        {
        }

        /// <summary>
        /// Returns a new span with a different size, copies the current data over to it, and frees this span.
        ///
        /// The new span may be larger or smaller than the current span. If it is larger, the new elements will be uninitialized.
        ///
        /// Warning: The span must have been allocated using the specified allocator.
        ///
        /// Warning: You must never use the old span (or any other span referencing the same memory) again after calling this method.
        ///
        /// Returns: The new span.
        /// </summary>
        public unsafe UnsafeSpan<T> Reallocate(Allocator allocator, int newSize)
        {
            return default;
        }
    }

	public static class SpanExtensions {
		public static void FillZeros<T>(this UnsafeSpan<T> span) where T : unmanaged {
        }

        public static void Fill<T>(this UnsafeSpan<T> span, T value) where T : unmanaged {
        }

        /// <summary>
        /// Copies the contents of a NativeArray to this span.
        /// The span must be large enough to hold the contents of the array.
        /// </summary>
        public static void CopyFrom<T>(this UnsafeSpan<T> span, NativeArray<T> array) where T : unmanaged {
        }

        /// <summary>
        /// Copies the contents of another span to this span.
        /// The span must be large enough to hold the contents of the array.
        /// </summary>
        public static void CopyFrom<T>(this UnsafeSpan<T> span, UnsafeSpan<T> other) where T : unmanaged {
        }

        /// <summary>
        /// Copies the contents of an array to this span.
        /// The span must be large enough to hold the contents of the array.
        /// </summary>
        public static void CopyFrom<T>(this UnsafeSpan<T> span, T[] array) where T : unmanaged {
        }

        /// <summary>
        /// Converts an UnsafeAppendBuffer to a span.
        /// The buffer must be a multiple of the element size.
        ///
        /// The span is a view of the buffer memory, so do not dispose the buffer while the span is in use.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static UnsafeSpan<T> AsUnsafeSpan<T>(this UnsafeAppendBuffer buffer) where T : unmanaged {
            return default;
        }

        /// <summary>
        /// Converts a NativeList to a span.
        ///
        /// The span is a view of the list memory, so do not dispose the list while the span is in use.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static UnsafeSpan<T> AsUnsafeSpan<T>(this NativeList<T> list) where T : unmanaged {
            return default;
        }

        /// <summary>
        /// Converts a NativeArray to a span.
        ///
        /// The span is a view of the array memory, so do not dispose the array while the span is in use.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static UnsafeSpan<T> AsUnsafeSpan<T>(this NativeArray<T> arr) where T : unmanaged {
            return default;
        }

        /// <summary>
        /// Converts a NativeArray to a span without performing any checks.
        ///
        /// The span is a view of the array memory, so do not dispose the array while the span is in use.
        /// This method does not perform any checks to ensure that the array is safe to write to or read from.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static UnsafeSpan<T> AsUnsafeSpanNoChecks<T>(this NativeArray<T> arr) where T : unmanaged {
            return default;
        }

        /// <summary>
        /// Converts a NativeArray to a span, assuming it will only be read.
        ///
        /// The span is a view of the array memory, so do not dispose the array while the span is in use.
        ///
        /// Warning: No checks are done to ensure that you only read from the array. You are responsible for ensuring that you do not write to the span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static UnsafeSpan<T> AsUnsafeReadOnlySpan<T>(this NativeArray<T> arr) where T : unmanaged {
            return default;
        }

        /// <summary>
        /// Converts an UnsafeList to a span.
        ///
        /// The span is a view of the list memory, so do not dispose the list while the span is in use.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static UnsafeSpan<T> AsUnsafeSpan<T>(this UnsafeList<T> arr) where T : unmanaged {
            return default;
        }

        /// <summary>
        /// Converts an ARUnsafeList to a span.
        /// The buffer must be a multiple of the element size.
        ///
        /// The span is a view of the buffer memory, so do not dispose the buffer while the span is in use.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static UnsafeSpan<T> AsUnsafeSpan<T>(this ARUnsafeList<T> list) where T : unmanaged {
            return default;
        }

        /// <summary>
        /// Converts a NativeSlice to a span.
        ///
        /// The span is a view of the slice memory, so do not dispose the underlaying memory allocation while the span is in use.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeSpan<T> AsUnsafeSpan<T>(this NativeSlice<T> slice) where T : unmanaged
        {
            return default;
        }

        /// <summary>Returns true if the value exists in the span</summary>
        public static bool Contains<T>(this UnsafeSpan<T> span, T value) where T : unmanaged, System.IEquatable<T>
        {
            return default;
        }

        /// <summary>
        /// Returns the index of the first occurrence of a value in the span.
        /// If the value is not found, -1 is returned.
        /// </summary>
        public static int IndexOf<T>(this UnsafeSpan<T> span, T value) where T : unmanaged, System.IEquatable<T>
        {
            return default;
        }

        /// <summary>Sorts the span in ascending order</summary>
        public static void Sort<T>(this UnsafeSpan<T> span) where T : unmanaged, System.IComparable<T>
        {
        }

        /// <summary>Sorts the span in ascending order</summary>
        public static void Sort<T, U>(this UnsafeSpan<T> span, U comp) where T : unmanaged where U : System.Collections.Generic.IComparer<T>
        {
        }

#if !MODULE_COLLECTIONS_2_4_0_OR_NEWER
        /// <summary>Shifts elements toward the end of this list, increasing its length</summary>
        public static void InsertRange<T>(this NativeList<T> list, int index, int count) where T : unmanaged
        {
        }
#endif

#if !MODULE_COLLECTIONS_2_1_0_OR_NEWER
		/// <summary>Appends value count times to the end of this list</summary>
		public static void AddReplicate<T>(this NativeList<T> list, T value, int count) where T : unmanaged {
			var origLength = list.Length;
			list.ResizeUninitialized(origLength + count);
			list.AsUnsafeSpan().Slice(origLength).Fill(value);
		}
#endif
    }
}
