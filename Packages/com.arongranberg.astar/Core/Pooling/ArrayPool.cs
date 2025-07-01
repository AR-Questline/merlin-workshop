#if !UNITY_EDITOR
// Extra optimizations when not running in the editor, but less error checking
#define ASTAR_OPTIMIZE_POOLING
#endif

using System;
using System.Collections.Generic;

namespace Pathfinding.Pooling {
	/// <summary>
	/// Lightweight Array Pool.
	/// Handy class for pooling arrays of type T.
	///
	/// Usage:
	/// - Claim a new array using <code> SomeClass[] foo = ArrayPool<SomeClass>.Claim (capacity); </code>
	/// - Use it and do stuff with it
	/// - Release it with <code> ArrayPool<SomeClass>.Release (ref foo); </code>
	///
	/// Warning: Arrays returned from the Claim method may contain arbitrary data.
	///  You cannot rely on it being zeroed out.
	///
	/// After you have released a array, you should never use it again, if you do use it
	/// your code may modify it at the same time as some other code is using it which
	/// will likely lead to bad results.
	///
	/// Since: Version 3.8.6
	/// See: Pathfinding.Pooling.ListPool
	/// </summary>
	public static class ArrayPool<T> {
#if !ASTAR_NO_POOLING
		/// <summary>
		/// Maximum length of an array pooled using ClaimWithExactLength.
		/// Arrays with lengths longer than this will silently not be pooled.
		/// </summary>
		const int MaximumExactArrayLength = 256;

		/// <summary>
		/// Internal pool.
		/// The arrays in each bucket have lengths of 2^i
		/// </summary>
		static readonly Stack<T[]>[] pool = new Stack<T[]>[31];
		static readonly Stack<T[]>[] exactPool = new Stack<T[]>[MaximumExactArrayLength+1];
#if !ASTAR_OPTIMIZE_POOLING
		static readonly HashSet<T[]> inPool = new HashSet<T[]>();
#endif
#endif

		/// <summary>
		/// Returns an array with at least the specified length.
		/// Warning: Returned arrays may contain arbitrary data.
		/// You cannot rely on it being zeroed out.
		///
		/// The returned array will always be a power of two, or zero.
		/// </summary>
		public static T[] Claim (int minimumLength)
        {
            return default;
        }

        /// <summary>
        /// Returns an array with the specified length.
        /// Use with caution as pooling too many arrays with different lengths that
        /// are rarely being reused will lead to an effective memory leak.
        ///
        /// Use <see cref="Claim"/> if you just need an array that is at least as large as some value.
        ///
        /// Warning: Returned arrays may contain arbitrary data.
        /// You cannot rely on it being zeroed out.
        /// </summary>
        public static T[] ClaimWithExactLength(int length)
        {
            return default;
        }

        /// <summary>
        /// Pool an array.
        /// If the array was got using the <see cref="ClaimWithExactLength"/> method then the allowNonPowerOfTwo parameter must be set to true.
        /// The parameter exists to make sure that non power of two arrays are not pooled unintentionally which could lead to memory leaks.
        /// </summary>
        public static void Release(ref T[] array, bool allowNonPowerOfTwo = false)
        {
        }
    }

    /// <summary>Extension methods for List<T></summary>
    public static class ListExtensions
    {
        /// <summary>
        /// Identical to ToArray but it uses ArrayPool<T> to avoid allocations if possible.
        ///
        /// Use with caution as pooling too many arrays with different lengths that
        /// are rarely being reused will lead to an effective memory leak.
        /// </summary>
        public static T[] ToArrayFromPool<T>(this List<T> list) {
            return default;
        }

        /// <summary>
        /// Clear a list faster than List<T>.Clear.
        /// It turns out that the List<T>.Clear method will clear all elements in the underlaying array
        /// not just the ones up to Count. If the list only has a few elements, but the capacity
        /// is huge, this can cause performance problems. Using the RemoveRange method to remove
        /// all elements in the list does not have this problem, however it is implemented in a
        /// stupid way, so it will clear the elements twice (completely unnecessarily) so it will
        /// only be faster than using the Clear method if the number of elements in the list is
        /// less than half of the capacity of the list.
        ///
        /// Hopefully this method can be removed when Unity upgrades to a newer version of Mono.
        /// </summary>
        public static void ClearFast<T>(this List<T> list)
        {
        }
    }
}
