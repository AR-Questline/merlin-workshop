#if !UNITY_EDITOR
// Extra optimizations when not running in the editor, but less error checking
#define ASTAR_OPTIMIZE_POOLING
#endif

using System;
using System.Collections.Generic;

namespace Pathfinding.Pooling {
	/// <summary>
	/// Lightweight List Pool.
	/// Handy class for pooling lists of type T.
	///
	/// Usage:
	/// - Claim a new list using <code> List<SomeClass> foo = ListPool<SomeClass>.Claim (); </code>
	/// - Use it and do stuff with it
	/// - Release it with <code> ListPool<SomeClass>.Release (foo); </code>
	///
	/// You do not need to clear the list before releasing it.
	/// After you have released a list, you should never use it again, if you do use it, you will
	/// mess things up quite badly in the worst case.
	///
	/// Since: Version 3.2
	/// See: Pathfinding.Util.StackPool
	/// </summary>
	public static class ListPool<T> {
		/// <summary>Internal pool</summary>
		static readonly List<List<T> > pool = new List<List<T> >();

#if !ASTAR_NO_POOLING
		static readonly List<List<T> > largePool = new List<List<T> >();
		static readonly HashSet<List<T> > inPool = new HashSet<List<T> >();
#endif

		/// <summary>
		/// When requesting a list with a specified capacity, search max this many lists in the pool before giving up.
		/// Must be greater or equal to one.
		/// </summary>
		const int MaxCapacitySearchLength = 8;
		const int LargeThreshold = 5000;
		const int MaxLargePoolSize = 8;

		/// <summary>
		/// Claim a list.
		/// Returns a pooled list if any are in the pool.
		/// Otherwise it creates a new one.
		/// After usage, this list should be released using the Release function (though not strictly necessary).
		/// </summary>
		public static List<T> Claim () {
            return default;
        }

        static int FindCandidate (List<List<T> > pool, int capacity) {
            return default;
        }

        /// <summary>
        /// Claim a list with minimum capacity
        /// Returns a pooled list if any are in the pool.
        /// Otherwise it creates a new one.
        /// After usage, this list should be released using the Release function (though not strictly necessary).
        /// A subset of the pool will be searched for a list with a high enough capacity and one will be returned
        /// if possible, otherwise the list with the largest capacity found will be returned.
        /// </summary>
        public static List<T> Claim(int capacity)
        {
            return default;
        }

        /// <summary>
        /// Makes sure the pool contains at least count pooled items with capacity size.
        /// This is good if you want to do all allocations at start.
        /// </summary>
        public static void Warmup(int count, int size)
        {
        }


        /// <summary>
        /// Releases a list and sets the variable to null.
        /// After the list has been released it should not be used anymore.
        ///
        /// Throws: System.InvalidOperationException
        /// Releasing a list when it has already been released will cause an exception to be thrown.
        ///
        /// See: <see cref="Claim"/>
        /// </summary>
        public static void Release(ref List<T> list)
        {
        }

        /// <summary>
        /// Releases a list.
        /// After the list has been released it should not be used anymore.
        ///
        /// Throws: System.InvalidOperationException
        /// Releasing a list when it has already been released will cause an exception to be thrown.
        ///
        /// See: <see cref="Claim"/>
        /// </summary>
        public static void Release(List<T> list)
        {
        }

        /// <summary>
        /// Clears the pool for lists of this type.
        /// This is an O(n) operation, where n is the number of pooled lists.
        /// </summary>
        public static void Clear()
        {
        }

        /// <summary>Number of lists of this type in the pool</summary>
        public static int GetSize()
        {
            return default;
        }
    }
}
