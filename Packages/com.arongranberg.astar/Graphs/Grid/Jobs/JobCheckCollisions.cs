using UnityEngine;
using Unity.Collections;
using Pathfinding.Jobs;

namespace Pathfinding.Graphs.Grid.Jobs {
	/// <summary>
	/// Checks if nodes are obstructed by obstacles or not.
	///
	/// See: <see cref="GraphCollision"/>
	/// </summary>
	struct JobCheckCollisions : IJobTimeSliced {
		[ReadOnly]
		public NativeArray<Vector3> nodePositions;
		public NativeArray<bool> collisionResult;
		public GraphCollision collision;
		int startIndex;

		public void Execute () {
        }

        public bool Execute (TimeSlice timeSlice) {
            return default;
        }
    }
}
