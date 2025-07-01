using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Pathfinding.Graphs.Grid.Jobs {
	/// <summary>Calculates for each grid node if it should be walkable or not</summary>
	[BurstCompile(FloatMode = FloatMode.Fast)]
	public struct JobNodeWalkability : IJob {
		/// <summary>
		/// If true, use the normal of the raycast hit to check if the ground is flat enough to stand on.
		///
		/// Any nodes with a steeper slope than <see cref="maxSlope"/> will be made unwalkable.
		/// </summary>
		public bool useRaycastNormal;
		/// <summary>Max slope in degrees</summary>
		public float maxSlope;
		/// <summary>Normalized up direction of the graph</summary>
		public Vector3 up;
		/// <summary>If true, nodes will be made unwalkable if no ground was found under them</summary>
		public bool unwalkableWhenNoGround;
		/// <summary>For layered grid graphs, if there's a node above another node closer than this distance, the lower node will be made unwalkable</summary>
		public float characterHeight;
		/// <summary>Number of nodes in each layer</summary>
		public int layerStride;

		[ReadOnly]
		public NativeArray<float3> nodePositions;

		public NativeArray<float4> nodeNormals;

		[WriteOnly]
		public NativeArray<bool> nodeWalkable;

		public void Execute () {
        }
    }
}
