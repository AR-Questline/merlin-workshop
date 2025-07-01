#pragma warning disable 414
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Pathfinding.Pooling;
using Pathfinding.Collections;

namespace Pathfinding.Graphs.Util {
	using Pathfinding.Drawing;

	public enum HeuristicOptimizationMode {
		None,
		Random,
		RandomSpreadOut,
		Custom
	}

	/// <summary>
	/// Implements heuristic optimizations.
	///
	/// See: heuristic-opt
	/// See: Game AI Pro - Pathfinding Architecture Optimizations by Steve Rabin and Nathan R. Sturtevant
	/// </summary>
	[System.Serializable]
	public class EuclideanEmbedding {
		/// <summary>
		/// If heuristic optimization should be used and how to place the pivot points.
		/// See: heuristic-opt
		/// See: Game AI Pro - Pathfinding Architecture Optimizations by Steve Rabin and Nathan R. Sturtevant
		/// </summary>
		public HeuristicOptimizationMode mode;

		public int seed;

		/// <summary>All children of this transform will be used as pivot points</summary>
		public Transform pivotPointRoot;

		public int spreadOutCount = 1;

		[System.NonSerialized]
		public bool dirty;

		/// <summary>
		/// Costs laid out as n*[int],n*[int],n*[int] where n is the number of pivot points.
		/// Each node has n integers which is the cost from that node to the pivot node.
		/// They are at around the same place in the array for simplicity and for cache locality.
		///
		/// cost(nodeIndex, pivotIndex) = costs[nodeIndex*pivotCount+pivotIndex]
		/// </summary>
		public NativeArray<uint> costs { get; private set; }
		public int pivotCount { get; private set; }

		GraphNode[] pivots;

		/*
		 * Seed for random number generator.
		 * Must not be zero
		 */
		const uint ra = 12820163;

		/*
		 * Seed for random number generator.
		 * Must not be zero
		 */
		const uint rc = 1140671485;

		/*
		 * Parameter for random number generator.
		 */
		uint rval;

		/// <summary>
		/// Simple linear congruential generator.
		/// See: http://en.wikipedia.org/wiki/Linear_congruential_generator
		/// </summary>
		uint GetRandom () {
            return default;
        }

        public void OnDisable()
        {
        }

        public static uint GetHeuristic(UnsafeSpan<uint> costs, uint pivotCount, uint nodeIndex1, uint nodeIndex2)
        {
            return default;
        }

        void GetClosestWalkableNodesToChildrenRecursively(Transform tr, List<GraphNode> nodes)
        {
        }

        /// <summary>
        /// Pick N random walkable nodes from all nodes in all graphs and add them to the buffer.
        ///
        /// Here we select N random nodes from a stream of nodes.
        /// Probability of choosing the first N nodes is 1
        /// Probability of choosing node i is min(N/i,1)
        /// A selected node will replace a random node of the previously
        /// selected ones.
        ///
        /// See: https://en.wikipedia.org/wiki/Reservoir_sampling
        /// </summary>
        void PickNRandomNodes(int count, List<GraphNode> buffer)
        {
        }

        GraphNode PickAnyWalkableNode () {
            return default;
        }

        public void RecalculatePivots()
        {
        }

        class EuclideanEmbeddingSearchPath : Path {
			public UnsafeSpan<uint> costs;
			public uint costIndexStride;
			public uint pivotIndex;
			public GraphNode startNode;
			public uint furthestNodeScore;
			public GraphNode furthestNode;

			public static EuclideanEmbeddingSearchPath Construct (UnsafeSpan<uint> costs, uint costIndexStride, uint pivotIndex, GraphNode startNode) {
                return default;
            }

            protected override void OnFoundEndNode(uint pathNode, uint hScore, uint gScore)
            {
            }

            protected override void OnHeapExhausted()
            {
            }

            public override void OnVisitNode(uint pathNode, uint hScore, uint gScore)
            {
            }

            protected override void Prepare()
            {
            }
        }

		public void RecalculateCosts () {
        }

        void RecalculateCostsInner () {
        }

        /// <summary>
        /// Special case necessary for paths to unwalkable nodes right next to walkable nodes to be able to use good heuristics.
        ///
        /// This will find all unwalkable nodes in all grid graphs with walkable nodes as neighbours
        /// and set the cost to reach them from each of the pivots as the minimum of the cost to
        /// reach the neighbours of each node.
        ///
        /// See: ABPath.EndPointGridGraphSpecialCase
        /// </summary>
        void ApplyGridGraphEndpointSpecialCase()
        {
        }

        public void OnDrawGizmos()
        {
        }
    }
}
