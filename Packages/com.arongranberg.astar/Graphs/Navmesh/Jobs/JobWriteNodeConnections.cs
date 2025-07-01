using Pathfinding.Pooling;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Assertions;
using UnityEngine.Profiling;

namespace Pathfinding.Graphs.Navmesh.Jobs {
	/// <summary>
	/// Writes connections to each node in each tile.
	///
	/// It also calculates the connection costs between nodes.
	///
	/// This job is run after all tiles have been built and the connections have been calculated.
	///
	/// See: <see cref="JobCalculateTriangleConnections"/>
	/// </summary>
	public struct JobWriteNodeConnections : IJob {
		/// <summary>Connections for each tile</summary>
		[ReadOnly]
		public NativeArray<JobCalculateTriangleConnections.TileNodeConnectionsUnsafe> nodeConnections;
		/// <summary>Array of <see cref="NavmeshTile"/></summary>
		public System.Runtime.InteropServices.GCHandle tiles;

		public void Execute () {
        }

        void Apply (TriangleMeshNode[] nodes, JobCalculateTriangleConnections.TileNodeConnectionsUnsafe connections) {
        }
    }
}
