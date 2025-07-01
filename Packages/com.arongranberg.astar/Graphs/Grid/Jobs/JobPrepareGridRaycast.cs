using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Pathfinding.Collections;
using UnityEngine.Assertions;

namespace Pathfinding.Graphs.Grid.Jobs {
	/// <summary>
	/// Prepares a set of raycast commands for a grid graph.
	///
	/// Each ray will start at <see cref="raycastOffset"/> from the node's position. The end point of the raycast will be the start point + <see cref="raycastDirection"/>.
	///
	/// See: <see cref="GraphCollision"/>
	/// </summary>
	[BurstCompile]
	public struct JobPrepareGridRaycast : IJob {
		public Matrix4x4 graphToWorld;
		public IntBounds bounds;
		public Vector3 raycastOffset;
		public Vector3 raycastDirection;
		public LayerMask raycastMask;
		public PhysicsScene physicsScene;

		[WriteOnly]
		public NativeArray<RaycastCommand> raycastCommands;

		public void Execute () {
        }
    }
}
