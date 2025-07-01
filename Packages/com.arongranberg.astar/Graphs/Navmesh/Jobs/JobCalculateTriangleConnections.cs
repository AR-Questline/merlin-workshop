using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Pathfinding.Graphs.Navmesh.Jobs {
	/// <summary>
	/// Calculates node connections between triangles within each tile.
	/// Connections between tiles are handled at a later stage in <see cref="JobConnectTiles"/>.
	/// </summary>
	[BurstCompile]
	public struct JobCalculateTriangleConnections : IJob {
		[ReadOnly]
		public NativeArray<TileMesh.TileMeshUnsafe> tileMeshes;
		[WriteOnly]
		public NativeArray<TileNodeConnectionsUnsafe> nodeConnections;

		public struct TileNodeConnectionsUnsafe {
			/// <summary>Stream of packed connection edge infos (from <see cref="Connection.PackShapeEdgeInfo"/>)</summary>
			public Unity.Collections.LowLevel.Unsafe.UnsafeAppendBuffer neighbours;
			/// <summary>Number of neighbours for each triangle</summary>
			public Unity.Collections.LowLevel.Unsafe.UnsafeAppendBuffer neighbourCounts;
		}

		public void Execute () {
        }
    }
}
