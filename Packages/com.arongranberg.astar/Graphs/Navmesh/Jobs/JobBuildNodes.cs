using Pathfinding.Jobs;
using Pathfinding.Sync;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;

namespace Pathfinding.Graphs.Navmesh.Jobs {
	/// <summary>
	/// Builds nodes and tiles and prepares them for pathfinding.
	///
	/// Takes input from a <see cref="TileBuilder"/> job and outputs a <see cref="BuildNodeTilesOutput"/>.
	///
	/// This job takes the following steps:
	/// - Calculate connections between nodes inside each tile
	/// - Create node and tile objects
	/// - Connect adjacent tiles together
	/// </summary>
	public struct JobBuildNodes {
		uint graphIndex;
		public uint initialPenalty;
		public bool recalculateNormals;
		public float maxTileConnectionEdgeDistance;
		Matrix4x4 graphToWorldSpace;
		TileLayout tileLayout;

		public class BuildNodeTilesOutput : IProgress, System.IDisposable {
			public TileBuilder.TileBuilderOutput progressSource;
			public NavmeshTile[] tiles;

			public float Progress => progressSource.Progress;

			public void Dispose () {
            }
        }

		internal JobBuildNodes(RecastGraph graph, TileLayout tileLayout) : this()
        {
        }

        public Promise<BuildNodeTilesOutput> Schedule(DisposeArena arena, Promise<TileBuilder.TileBuilderOutput> preCutDependency, Promise<TileCutter.TileCutterOutput> postCutDependency)
        {
            return default;
        }
    }
}
