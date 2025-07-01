using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Pathfinding.Jobs;
using UnityEngine.Assertions;

namespace Pathfinding.Graphs.Grid.Jobs {
	/// <summary>
	/// Writes node data from unmanaged arrays into managed <see cref="GridNodeBase"/> objects.
	///
	/// This is done after burst jobs have been working on graph data, as they cannot access the managed objects directly.
	///
	/// Earlier, data will have been either calculated from scratch, or read from the managed objects using the <see cref="JobReadNodeData"/> job.
	/// </summary>
	public struct JobWriteNodeData : IJobParallelForBatched {
		public System.Runtime.InteropServices.GCHandle nodesHandle;
		public uint graphIndex;

		/// <summary>(width, depth) of the array that the <see cref="nodesHandle"/> refers to</summary>
		public int3 nodeArrayBounds;
		public IntBounds dataBounds;
		public IntBounds writeMask;

		[ReadOnly]
		public NativeArray<Vector3> nodePositions;

		[ReadOnly]
		public NativeArray<uint> nodePenalties;

		[ReadOnly]
		public NativeArray<int> nodeTags;

		[ReadOnly]
		public NativeArray<ulong> nodeConnections;

		[ReadOnly]
		public NativeArray<bool> nodeWalkableWithErosion;

		[ReadOnly]
		public NativeArray<bool> nodeWalkable;

		public bool allowBoundsChecks => false;

		public void Execute (int startIndex, int count) {
        }
    }
}
