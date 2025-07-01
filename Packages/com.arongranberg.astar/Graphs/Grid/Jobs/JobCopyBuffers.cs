using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Pathfinding.Jobs;
using UnityEngine.Assertions;

namespace Pathfinding.Graphs.Grid.Jobs {
	/// <summary>
	/// Copies 3D arrays with grid data from one shape to another.
	///
	/// Only the data for the nodes that exist in both buffers will be copied.
	///
	/// This essentially is several <see cref="JobCopyRectangle"/> jobs in one (to avoid scheduling overhead).
	/// See that job for more documentation.
	/// </summary>
	[BurstCompile]
	public struct JobCopyBuffers : IJob {
		[ReadOnly]
		[DisableUninitializedReadCheck]
		public GridGraphNodeData input;

		[WriteOnly]
		public GridGraphNodeData output;
		public IntBounds bounds;

		public bool copyPenaltyAndTags;

		public void Execute () {
        }
    }
}
