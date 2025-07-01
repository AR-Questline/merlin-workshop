using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Pathfinding.Jobs;
using Pathfinding.Util;
using Pathfinding.Collections;

namespace Pathfinding.Graphs.Grid.Jobs {
	/// <summary>
	/// Calculates the grid connections for all nodes.
	///
	/// This is a IJobParallelForBatch job. Calculating the connections in multiple threads is faster,
	/// but due to hyperthreading (used on most intel processors) the individual threads will become slower.
	/// It is still worth it though.
	/// </summary>
	[BurstCompile(FloatMode = FloatMode.Fast, CompileSynchronously = true)]
	public struct JobCalculateGridConnections : IJobParallelForBatched {
		public float maxStepHeight;
		public float4x4 graphToWorld;
		public IntBounds bounds;
		public int3 arrayBounds;
		public NumNeighbours neighbours;
		public float characterHeight;
		public bool use2D;
		public bool cutCorners;
		public bool maxStepUsesSlope;
		public bool layeredDataLayout;

		[ReadOnly]
		public UnsafeSpan<bool> nodeWalkable;

		[ReadOnly]
		public UnsafeSpan<float4> nodeNormals;

		[ReadOnly]
		public UnsafeSpan<Vector3> nodePositions;

		/// <summary>All bitpacked node connections</summary>
		[WriteOnly]
		public UnsafeSpan<ulong> nodeConnections;

		public bool allowBoundsChecks => false;

		public static bool IsValidConnection (float y, float y2, float maxStepHeight) {
            return default;
        }

        public static bool IsValidConnection (float2 yRange, float2 yRange2, float maxStepHeight, float characterHeight) {
            return default;
        }

        static float ConnectionY (UnsafeSpan<float3> nodePositions, UnsafeSpan<float4> nodeNormals, NativeArray<float4> normalToHeightOffset, int nodeIndex, int dir, float4 up, bool reverse) {
            return default;
        }

        static float2 ConnectionYRange(UnsafeSpan<float3> nodePositions, UnsafeSpan<float4> nodeNormals, NativeArray<float4> normalToHeightOffset, int nodeIndex, int layerStride, int y, int maxY, int dir, float4 up, bool reverse)
        {
            return default;
        }

        static NativeArray<float4> HeightOffsetProjections(float4x4 graphToWorldTranform, bool maxStepUsesSlope)
        {
            return default;
        }

        public void Execute(int start, int count)
        {
        }

        public void ExecuteFlat(int start, int count)
        {
        }

        public void ExecuteLayered(int start, int count)
        {
        }
    }
}
