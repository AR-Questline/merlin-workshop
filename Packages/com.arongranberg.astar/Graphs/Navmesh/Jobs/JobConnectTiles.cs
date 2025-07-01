using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Pathfinding.Graphs.Navmesh.Jobs {
	/// <summary>
	/// Connects adjacent tiles together.
	///
	/// This only creates connections between tiles. Connections internal to a tile should be handled by <see cref="JobCalculateTriangleConnections"/>.
	///
	/// Use the <see cref="ScheduleBatch"/> method to connect a bunch of tiles efficiently using maximum parallelism.
	/// </summary>
	public struct JobConnectTiles : IJob {
		/// <summary>GCHandle referring to a NavmeshTile[] array of size tileRect.Width*tileRect.Height</summary>
		public System.Runtime.InteropServices.GCHandle tiles;
		public int coordinateSum;
		public int direction;
		public int zOffset;
		public int zStride;
		Vector2 tileWorldSize;
		IntRect tileRect;
		/// <summary>Maximum vertical distance between two tiles to create a connection between them</summary>
		public float maxTileConnectionEdgeDistance;

		static readonly Unity.Profiling.ProfilerMarker ConnectTilesMarker = new Unity.Profiling.ProfilerMarker("ConnectTiles");

		/// <summary>
		/// Schedule jobs to connect all the given tiles with each other while exploiting as much parallelism as possible.
		/// tilesHandle should be a GCHandle referring to a NavmeshTile[] array of size tileRect.Width*tileRect.Height.
		/// </summary>
		public static JobHandle ScheduleBatch (System.Runtime.InteropServices.GCHandle tilesHandle, JobHandle dependency, IntRect tileRect, Vector2 tileWorldSize, float maxTileConnectionEdgeDistance)
        {
            return default;
        }

        /// <summary>
        /// Schedule jobs to connect all the given tiles inside innerRect with tiles that are outside it, while exploiting as much parallelism as possible.
        /// tilesHandle should be a GCHandle referring to a NavmeshTile[] array of size tileRect.Width*tileRect.Height.
        /// </summary>
        public static JobHandle ScheduleRecalculateBorders(System.Runtime.InteropServices.GCHandle tilesHandle, JobHandle dependency, IntRect tileRect, IntRect innerRect, Vector2 tileWorldSize, float maxTileConnectionEdgeDistance)
        {
            return default;
        }

        public void Execute()
        {
        }
    }

    /// <summary>
    /// Connects two adjacent tiles together.
    ///
    /// This only creates connections between tiles. Connections internal to a tile should be handled by <see cref="JobCalculateTriangleConnections"/>.
    /// </summary>
    struct JobConnectTilesSingle : IJob
    {
        /// <summary>GCHandle referring to a NavmeshTile[] array of size tileRect.Width*tileRect.Height</summary>
        public System.Runtime.InteropServices.GCHandle tiles;
		/// <summary>Index of the first tile in the <see cref="tiles"/> array</summary>
		public int tileIndex1;
		/// <summary>Index of the second tile in the <see cref="tiles"/> array</summary>
		public int tileIndex2;
		/// <summary>Size of a tile in world units</summary>
		public Vector2 tileWorldSize;
		/// <summary>Maximum vertical distance between two tiles to create a connection between them</summary>
		public float maxTileConnectionEdgeDistance;

		public void Execute () {
        }
    }
}
