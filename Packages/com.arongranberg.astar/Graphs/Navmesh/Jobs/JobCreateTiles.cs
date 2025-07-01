using Pathfinding.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;

namespace Pathfinding.Graphs.Navmesh.Jobs {
	/// <summary>
	/// Builds tiles optimized for pathfinding, from a list of <see cref="TileMesh.TileMeshUnsafe"/>.
	///
	/// This job takes the following steps:
	/// - Transform all vertices using the <see cref="graphToWorldSpace"/> matrix.
	/// - Remove duplicate vertices
	/// - If <see cref="recalculateNormals"/> is enabled: ensure all triangles are laid out in the clockwise direction.
	/// </summary>
	public struct JobCreateTiles : IJob {
		/// <summary>An array of <see cref="TileMesh.TileMeshUnsafe"/> of length tileRect.Width*tileRect.Height, or an uninitialized array</summary>
		[ReadOnly]
		[NativeDisableContainerSafetyRestriction]
		public NativeArray<TileMesh.TileMeshUnsafe> preCutTileMeshes;

		/// <summary>An array of <see cref="TileMesh.TileMeshUnsafe"/> of length tileRect.Width*tileRect.Height</summary>
		[ReadOnly]
		public NativeArray<TileMesh.TileMeshUnsafe> tileMeshes;

		/// <summary>
		/// An array of <see cref="NavmeshTile"/> of length tileRect.Width*tileRect.Height.
		/// This array will be filled with the created tiles.
		/// </summary>
		public System.Runtime.InteropServices.GCHandle tiles;

		/// <summary>Graph index of the graph that these nodes will be added to</summary>
		public uint graphIndex;

		/// <summary>
		/// Number of tiles in the graph.
		///
		/// This may be much bigger than the <see cref="tileRect"/> that we are actually processing.
		/// For example if a graph update is performed, the <see cref="tileRect"/> will just cover the tiles that are recalculated,
		/// while <see cref="graphTileCount"/> will contain all tiles in the graph.
		/// </summary>
		public Vector2Int graphTileCount;

		/// <summary>
		/// Rectangle of tiles that we are processing.
		///
		/// (xmax, ymax) must be smaller than graphTileCount.
		/// If for examples <see cref="graphTileCount"/> is (10, 10) and <see cref="tileRect"/> is {2, 3, 5, 6} then we are processing tiles (2, 3) to (5, 6) inclusive.
		/// </summary>
		public IntRect tileRect;

		/// <summary>Initial penalty for all nodes in the tile</summary>
		public uint initialPenalty;

		/// <summary>
		/// If true, all triangles will be guaranteed to be laid out in clockwise order.
		/// If false, their original order will be preserved.
		/// </summary>
		public bool recalculateNormals;

		/// <summary>Size of a tile in world units along the graph's X and Z axes</summary>
		public Vector2 tileWorldSize;

		/// <summary>Matrix to convert from graph space to world space</summary>
		public Matrix4x4 graphToWorldSpace;

		public void Execute () {
        }
    }
}
