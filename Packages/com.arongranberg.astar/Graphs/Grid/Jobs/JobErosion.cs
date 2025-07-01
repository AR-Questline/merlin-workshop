using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Pathfinding.Jobs;
using UnityEngine.Assertions;

namespace Pathfinding.Graphs.Grid.Jobs {
	/// <summary>
	/// Calculates erosion.
	/// Note that to ensure that connections are completely up to date after updating a node you
	/// have to calculate the connections for both the changed node and its neighbours.
	///
	/// In a layered grid graph, this will recalculate the connections for all nodes
	/// in the (x,z) cell (it may have multiple layers of nodes).
	///
	/// See: CalculateConnections(GridNodeBase)
	/// </summary>
	[BurstCompile]
	public struct JobErosion<AdjacencyMapper> : IJob where AdjacencyMapper : GridAdjacencyMapper, new() {
		public IntBounds bounds;
		public IntBounds writeMask;
		public NumNeighbours neighbours;
		public int erosion;
		public bool erosionUsesTags;
		public int erosionStartTag;

		[ReadOnly]
		public NativeArray<ulong> nodeConnections;

		[ReadOnly]
		public NativeArray<bool> nodeWalkable;

		[WriteOnly]
		public NativeArray<bool> outNodeWalkable;

		public NativeArray<int> nodeTags;
		public int erosionTagsPrecedenceMask;

		// Note: the first 3 connections are to nodes with a higher x or z coordinate
		// The last 3 connections are to nodes with a lower x or z coordinate
		// This is required for the grassfire transform to work properly
		// This is a permutation of GridGraph.hexagonNeighbourIndices
		static readonly int[] hexagonNeighbourIndices = { 1, 2, 5, 0, 3, 7 };

		public void Execute () {
        }
    }
}
