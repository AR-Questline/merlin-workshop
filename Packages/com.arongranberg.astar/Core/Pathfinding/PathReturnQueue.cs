using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace Pathfinding {
	class PathReturnQueue {
		/// <summary>
		/// Holds all paths which are waiting to be flagged as completed.
		/// See: <see cref="ReturnPaths"/>
		/// </summary>
		readonly Queue<Path> pathReturnQueue = new Queue<Path>();

		/// <summary>
		/// Paths are claimed silently by some object to prevent them from being recycled while still in use.
		/// This will be set to the AstarPath object.
		/// </summary>
		readonly System.Object pathsClaimedSilentlyBy;

		readonly System.Action OnReturnedPaths;

		public PathReturnQueue (System.Object pathsClaimedSilentlyBy, System.Action OnReturnedPaths) {
        }

        public void Enqueue (Path path) {
        }

        /// <summary>
        /// Returns all paths in the return stack.
        /// Paths which have been processed are put in the return stack.
        /// This function will pop all items from the stack and return them to e.g the Seeker requesting them.
        /// </summary>
        /// <param name="timeSlice">Do not return all paths at once if it takes a long time, instead return some and wait until the next call.</param>
        public void ReturnPaths (bool timeSlice) {
        }
    }
}
