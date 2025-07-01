using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Pathfinding.Jobs;
using Pathfinding.Util;
using Pathfinding.Collections;

namespace Pathfinding.Graphs.Grid.Jobs {
	/// <summary>
	/// Filters out diagonal connections that are not allowed in layered grid graphs.
	///
	/// This is a IJobParallelForBatched job which is parallelelized over the z coordinate of the <see cref="slice"/>.
	///
	/// The <see cref="JobCalculateGridConnections"/> job will run first, and calculate the connections for all nodes.
	/// However, for layered grid graphs, the connections for diagonal nodes may be incorrect, and this
	/// post-processing pass is needed to validate the diagonal connections.
	/// </summary>
	[BurstCompile]
	public struct JobFilterDiagonalConnections : IJobParallelForBatched {
		public Slice3D slice;
		public NumNeighbours neighbours;
		public bool cutCorners;

		/// <summary>All bitpacked node connections</summary>
		public UnsafeSpan<ulong> nodeConnections;

		public bool allowBoundsChecks => false;

		public void Execute (int start, int count) {
        }
    }
}
