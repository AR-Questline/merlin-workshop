using UnityEngine;
using Pathfinding.Util;
using UnityEngine.Tilemaps;

namespace Pathfinding.Graphs.Navmesh {
	/// <summary>
	/// Represents the position and size of a tile grid for a recast/navmesh graph.
	///
	/// This separates out the physical layout of tiles from all the other recast graph settings.
	/// </summary>
	public struct TileLayout {
		/// <summary>How many tiles there are in the grid</summary>
		public Vector2Int tileCount;

		/// <summary>Transforms coordinates from graph space to world space</summary>
		public GraphTransform transform;

		/// <summary>Size of a tile in voxels along the X and Z axes</summary>
		public Vector2Int tileSizeInVoxels;

		/// <summary>
		/// Size in graph space of the whole grid.
		///
		/// If the original bounding box was not an exact multiple of the tile size, this will be less than the total width of all tiles.
		/// </summary>
		public Vector3 graphSpaceSize;

		/// <summary>\copydocref{RecastGraph.cellSize}</summary>
		public float cellSize;

		/// <summary>
		/// Voxel y coordinates will be stored as ushorts which have 65536 values.
		/// Leave a margin to make sure things do not overflow
		/// </summary>
		public float CellHeight => Mathf.Max(graphSpaceSize.y / 64000, 0.001f);

		public Vector2 TileWorldSize => new Vector2(TileWorldSizeX, TileWorldSizeZ);

		/// <summary>Size of a tile in world units, along the graph's X axis</summary>
		public float TileWorldSizeX => tileSizeInVoxels.x * cellSize;

		/// <summary>Size of a tile in world units, along the graph's Z axis</summary>
		public float TileWorldSizeZ => tileSizeInVoxels.y * cellSize;

		/// <summary>Returns an XZ bounds object with the bounds of a group of tiles in graph space</summary>
		public Bounds GetTileBoundsInGraphSpace (int x, int z, int width = 1, int depth = 1) {
            return default;
        }

        /// <summary>
        /// Returns a rect containing the indices of all tiles touching the specified bounds.
        /// If a margin is passed, the bounding box in graph space is expanded by that amount in every direction.
        /// </summary>
        public IntRect GetTouchingTiles (Bounds bounds, float margin = 0) {
            return default;
        }

        /// <summary>Returns a rect containing the indices of all tiles touching the specified bounds.</summary>
        /// <param name="rect">Graph space rectangle (in graph space all tiles are on the XZ plane regardless of graph rotation and other transformations, the first tile has a corner at the origin)</param>
        public IntRect GetTouchingTilesInGraphSpace(Rect rect)
        {
            return default;
        }

        public TileLayout(RecastGraph graph) : this(new Bounds(graph.forcedBoundsCenter, graph.forcedBoundsSize), Quaternion.Euler(graph.rotation), graph.cellSize, graph.editorTileSize, graph.useTiles)
        {
        }

        public TileLayout(NavMeshGraph graph) : this(new Bounds(graph.transform.Transform(graph.forcedBoundsSize * 0.5f), graph.forcedBoundsSize), Quaternion.Euler(graph.rotation), 0.001f, 0, false)
        {
        }

        public TileLayout(Bounds bounds, Quaternion rotation, float cellSize, int tileSizeInVoxels, bool useTiles) : this()
        {
        }
    }
}
