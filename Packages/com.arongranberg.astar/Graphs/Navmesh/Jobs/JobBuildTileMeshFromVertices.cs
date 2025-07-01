using Pathfinding.Util;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using Pathfinding.Collections;
using Pathfinding.Sync;

namespace Pathfinding.Graphs.Navmesh.Jobs {
	/// <summary>
	/// Builds tiles from raw mesh vertices and indices.
	///
	/// This job takes the following steps:
	/// - Transform all vertices using the <see cref="meshToGraph"/> matrix.
	/// - Remove duplicate vertices
	/// - If <see cref="recalculateNormals"/> is enabled: ensure all triangles are laid out in the clockwise direction.
	/// </summary>
	[BurstCompile(FloatMode = FloatMode.Default)]
	public struct JobBuildTileMeshFromVertices : IJob {
		public NativeArray<Vector3> vertices;
		public NativeArray<int> indices;
		public Matrix4x4 meshToGraph;
		public NativeArray<TileMesh.TileMeshUnsafe> outputBuffers;
		public bool recalculateNormals;


		[BurstCompile(FloatMode = FloatMode.Fast)]
		public struct JobTransformTileCoordinates : IJob {
			public NativeArray<Vector3> vertices;
			public NativeArray<Int3> outputVertices;
			public Matrix4x4 matrix;

			public void Execute () {
            }
        }

		public static Promise<TileBuilder.TileBuilderOutput> Schedule (NativeArray<Vector3> vertices, NativeArray<int> indices, Matrix4x4 meshToGraph, bool recalculateNormals) {
            return default;
        }

        public void Execute()
        {
        }
    }
}
