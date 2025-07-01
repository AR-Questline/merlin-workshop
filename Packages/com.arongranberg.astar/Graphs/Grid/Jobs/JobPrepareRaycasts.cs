using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Pathfinding.Collections;

namespace Pathfinding.Graphs.Grid.Jobs {
	/// <summary>
	/// Prepares a set of raycast commands for a grid graph.
	///
	/// This is very similar to <see cref="JobPrepareGridRaycast"/> but it uses an array of origin points instead of a grid pattern.
	///
	/// See: <see cref="GraphCollision"/>
	/// </summary>
	[BurstCompile]
	public struct JobPrepareRaycasts : IJob {
		public Vector3 direction;
		public Vector3 originOffset;
		public float distance;
		public LayerMask mask;
		public PhysicsScene physicsScene;

		[ReadOnly]
		public NativeArray<Vector3> origins;

		[WriteOnly]
		public NativeArray<RaycastCommand> raycastCommands;

		public void Execute () {
        }
    }
}
