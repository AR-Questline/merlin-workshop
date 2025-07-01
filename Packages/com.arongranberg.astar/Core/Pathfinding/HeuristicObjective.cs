using Unity.Mathematics;
using Unity.Burst;
using Pathfinding.Util;
using Pathfinding.Graphs.Util;
using Pathfinding.Collections;

namespace Pathfinding {
	/// <summary>
	/// Calculates an estimated cost from the specified point to the target.
	///
	/// See: https://en.wikipedia.org/wiki/A*_search_algorithm
	/// </summary>
	[BurstCompile]
	public readonly struct HeuristicObjective {
		readonly int3 mn;
		readonly int3 mx;
		readonly Heuristic heuristic;
		readonly float heuristicScale;
		readonly UnsafeSpan<uint> euclideanEmbeddingCosts;
		readonly uint euclideanEmbeddingPivots;
		readonly uint targetNodeIndex;

		public bool hasHeuristic => heuristic != Heuristic.None;

		public HeuristicObjective (int3 point, Heuristic heuristic, float heuristicScale) : this()
        {
        }

        public HeuristicObjective(int3 point, Heuristic heuristic, float heuristicScale, uint targetNodeIndex, EuclideanEmbedding euclideanEmbedding) : this()
        {
        }

        public HeuristicObjective(int3 mn, int3 mx, Heuristic heuristic, float heuristicScale, uint targetNodeIndex, EuclideanEmbedding euclideanEmbedding) : this()
        {
        }

        public int Calculate(int3 point, uint nodeIndex)
        {
            return default;
        }

        [BurstCompile]
        public static int Calculate(in HeuristicObjective objective, ref int3 point, uint nodeIndex)
        {
            return default;
        }
    }
}
