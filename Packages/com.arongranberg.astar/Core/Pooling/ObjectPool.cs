#if !UNITY_EDITOR
// Extra optimizations when not running in the editor, but less error checking
#define ASTAR_OPTIMIZE_POOLING
#endif

using System;
using System.Collections.Generic;

namespace Pathfinding.Pooling {
	public interface IAstarPooledObject {
		void OnEnterPool();
	}

	/// <summary>
	/// Lightweight object Pool for IAstarPooledObject.
	/// Handy class for pooling objects of type T which implements the IAstarPooledObject interface.
	///
	/// Usage:
	/// - Claim a new object using <code> SomeClass foo = ObjectPool<SomeClass>.Claim (); </code>
	/// - Use it and do stuff with it
	/// - Release it with <code> ObjectPool<SomeClass>.Release (foo); </code>
	///
	/// After you have released a object, you should never use it again.
	///
	/// Since: Version 3.2
	/// Version: Since 3.7.6 this class is thread safe
	/// See: Pathfinding.Pooling.ListPool
	/// See: ObjectPoolSimple
	/// </summary>
	public static class ObjectPool<T> where T : class, IAstarPooledObject, new(){
		public static T Claim () {
            return default;
        }

        public static void Release(ref T obj)
        {
        }
    }

	/// <summary>
	/// Lightweight object Pool.
	/// Handy class for pooling objects of type T.
	///
	/// Usage:
	/// - Claim a new object using <code> SomeClass foo = ObjectPool<SomeClass>.Claim (); </code>
	/// - Use it and do stuff with it
	/// - Release it with <code> ObjectPool<SomeClass>.Release (foo); </code>
	///
	/// After you have released a object, you should never use it again.
	///
	/// Since: Version 3.2
	/// Version: Since 3.7.6 this class is thread safe
	/// See: Pathfinding.Pooling.ListPool
	/// See: ObjectPool
	/// </summary>
	public static class ObjectPoolSimple<T> where T : class, new(){
		/// <summary>Internal pool</summary>
		static List<T> pool = new List<T>();

#if !ASTAR_NO_POOLING
		static readonly HashSet<T> inPool = new HashSet<T>();
#endif

		/// <summary>
		/// Claim a object.
		/// Returns a pooled object if any are in the pool.
		/// Otherwise it creates a new one.
		/// After usage, this object should be released using the Release function (though not strictly necessary).
		/// </summary>
		public static T Claim () {
            return default;
        }

        /// <summary>
        /// Releases an object.
        /// After the object has been released it should not be used anymore.
        /// The variable will be set to null to prevent silly mistakes.
        ///
        /// Throws: System.InvalidOperationException
        /// Releasing an object when it has already been released will cause an exception to be thrown.
        /// However enabling ASTAR_OPTIMIZE_POOLING will prevent this check.
        ///
        /// See: Claim
        /// </summary>
        public static void Release(ref T obj)
        {
        }

        /// <summary>
        /// Clears the pool for objects of this type.
        /// This is an O(n) operation, where n is the number of pooled objects.
        /// </summary>
        public static void Clear()
        {
        }

        /// <summary>Number of objects of this type in the pool</summary>
        public static int GetSize()
        {
            return default;
        }
    }
}
