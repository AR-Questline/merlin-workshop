using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Pathfinding.Jobs;

namespace Pathfinding.Graphs.Grid.Jobs {
	/// <summary>
	/// Reads node data from managed <see cref="GridNodeBase"/> objects into unmanaged arrays.
	///
	/// This is done so that burst jobs can later access this data directly.
	///
	/// Later, data will be written back to the managed objects using the <see cref="JobWriteNodeData"/> job.
	/// </summary>
	public struct JobReadNodeData : IJobParallelForBatched {
		public System.Runtime.InteropServices.GCHandle nodesHandle;
		public uint graphIndex;

		public Slice3D slice;

		[WriteOnly]
		public NativeArray<Vector3> nodePositions;

		[WriteOnly]
		public NativeArray<uint> nodePenalties;

		[WriteOnly]
		public NativeArray<int> nodeTags;

		[WriteOnly]
		public NativeArray<ulong> nodeConnections;

		[WriteOnly]
		public NativeArray<bool> nodeWalkableWithErosion;

		[WriteOnly]
		public NativeArray<bool> nodeWalkable;

		public bool allowBoundsChecks => false;

		struct Reader : GridIterationUtilities.ISliceAction {
			public GridNodeBase[] nodes;
			public NativeArray<Vector3> nodePositions;
			public NativeArray<uint> nodePenalties;
			public NativeArray<int> nodeTags;
			public NativeArray<ulong> nodeConnections;
			public NativeArray<bool> nodeWalkableWithErosion;
			public NativeArray<bool> nodeWalkable;

			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			public void Execute (uint outerIdx, uint innerIdx) {
            }
        }

        public void Execute(int startIndex, int count)
        {
        }
    }
}
