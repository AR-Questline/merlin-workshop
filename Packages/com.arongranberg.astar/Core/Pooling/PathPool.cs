//#define ASTAR_NO_POOLING // Disable pooling for some reason. Maybe for debugging or just for measuring the difference.
using System;
using System.Collections.Generic;

namespace Pathfinding.Pooling {
	/// <summary>Pools path objects to reduce load on the garbage collector</summary>
	public static class PathPool {
		static readonly Dictionary<Type, Stack<Path> > pool = new Dictionary<Type, Stack<Path> >();
		static readonly Dictionary<Type, int> totalCreated = new Dictionary<Type, int>();

		/// <summary>
		/// Adds a path to the pool.
		/// This function should not be used directly. Instead use the Path.Claim and Path.Release functions.
		/// </summary>
		public static void Pool (Path path) {
        }

        /// <summary>Total created instances of paths of the specified type</summary>
        public static int GetTotalCreated (Type type) {
            return default;
        }

        /// <summary>Number of pooled instances of a path of the specified type</summary>
        public static int GetSize (Type type) {
            return default;
        }

        /// <summary>Get a path from the pool or create a new one if the pool is empty</summary>
        public static T GetPath<T>() where T : Path, new() {
            return default;
        }
    }
}
