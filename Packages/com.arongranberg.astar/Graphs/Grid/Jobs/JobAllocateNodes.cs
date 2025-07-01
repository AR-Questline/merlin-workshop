using Pathfinding.Collections;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Pathfinding.Graphs.Grid.Jobs {
	/// <summary>
	/// Allocates and deallocates nodes in a grid graph.
	///
	/// This will inspect every cell in the dataBounds and allocate or deallocate the node depending on if that slot should have a node or not according to the nodeNormals array (pure zeroes means no node).
	///
	/// This is only used for incremental updates of grid graphs.
	/// The initial layer of the grid graph (which is always filled with nodes) is allocated in the <see cref="GridGraph.AllocateNodesJob"/> method.
	/// </summary>
	public struct JobAllocateNodes : IJob {
		public AstarPath active;
		[ReadOnly]
		public NativeArray<float4> nodeNormals;
		public IntBounds dataBounds;
		public int3 nodeArrayBounds;
		public GridNodeBase[] nodes;
		public System.Func<GridNodeBase> newGridNodeDelegate;

		public void Execute () {
        }
    }
}
