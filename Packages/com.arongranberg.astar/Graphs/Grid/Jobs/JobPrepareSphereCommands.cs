#if UNITY_2022_2_OR_NEWER
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Pathfinding.Collections;

namespace Pathfinding.Graphs.Grid.Jobs {
	/// <summary>
	/// Prepares a set of sphere commands for collision checking in a grid graph.
	///
	/// See: <see cref="GraphCollision"/>
	/// </summary>
	[BurstCompile]
	public struct JobPrepareSphereCommands : IJob {
		public Vector3 originOffset;
		public float radius;
		public LayerMask mask;
		public PhysicsScene physicsScene;

		[ReadOnly]
		public NativeArray<Vector3> origins;

		[WriteOnly]
		public NativeArray<OverlapSphereCommand> commands;

		public void Execute () {
        }
    }
}
#endif
