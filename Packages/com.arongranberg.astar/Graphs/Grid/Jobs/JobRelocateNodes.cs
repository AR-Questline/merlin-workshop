using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Pathfinding.Graphs.Grid.Jobs {
	/// <summary>Moves all nodes to new positions</summary>
	[BurstCompile]
	public struct JobRelocateNodes : IJob, GridIterationUtilities.ICellAction {
		public Matrix4x4 previousWorldToGraph;
		public Matrix4x4 graphToWorld;
		public NativeArray<Vector3> positions;
		public IntBounds bounds;

		public void Execute () {
        }

        public void Execute (uint innerIndex, int x, int y, int z) {
        }
    }
}
