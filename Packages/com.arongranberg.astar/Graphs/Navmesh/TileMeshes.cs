using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Pathfinding.Graphs.Navmesh {
	/// <summary>
	/// Represents a rectangular group of tiles of a recast graph.
	///
	/// This is a portable representation in that it can be serialized to and from a byte array.
	///
	/// <code>
	/// // Scans the first 6x6 chunk of tiles of the recast graph (the IntRect uses inclusive coordinates)
	/// var graph = AstarPath.active.data.recastGraph;
	/// var buildSettings = RecastBuilder.BuildTileMeshes(graph, new TileLayout(graph), new IntRect(0, 0, 5, 5));
	/// var disposeArena = new Pathfinding.Jobs.DisposeArena();
	/// var promise = buildSettings.Schedule(disposeArena);
	///
	/// AstarPath.active.AddWorkItem(() => {
	///     // Block until the asynchronous job completes
	///     var result = promise.Complete();
	///     TileMeshes tiles = result.tileMeshes.ToManaged();
	///     // Take the scanned tiles and place them in the graph,
	///     // but not at their original location, but 2 tiles away, rotated 90 degrees.
	///     tiles.tileRect = tiles.tileRect.Offset(new Vector2Int(2, 0));
	///     tiles.Rotate(1);
	///     graph.ReplaceTiles(tiles);
	///
	///     // Dispose unmanaged data
	///     disposeArena.DisposeAll();
	///     result.Dispose();
	/// });
	/// </code>
	///
	/// See: <see cref="NavmeshPrefab"/> uses this representation internally for storage.
	/// See: <see cref="RecastGraph.ReplaceTiles"/>
	/// See: <see cref="RecastBuilder.BuildTileMeshes"/>
	/// </summary>
	public struct TileMeshes {
		/// <summary>Tiles laid out row by row</summary>
		public TileMesh[] tileMeshes;
		/// <summary>Which tiles in the graph this group of tiles represents</summary>
		public IntRect tileRect;
		/// <summary>World-space size of each tile</summary>
		public Vector2 tileWorldSize;

		/// <summary>Rotate this group of tiles by 90*N degrees clockwise about the group's center</summary>
		public void Rotate (int rotation)
        {
        }

        /// <summary>
        /// Serialize this struct to a portable byte array.
        /// The data is compressed using the deflate algorithm to reduce size.
        /// See: <see cref="Deserialize"/>
        /// </summary>
        public byte[] Serialize()
        {
            return default;
        }

        /// <summary>
        /// Deserialize an instance from a byte array.
        /// See: <see cref="Serialize"/>
        /// </summary>
        public static TileMeshes Deserialize(byte[] bytes)
        {
            return default;
        }
    }

    /// <summary>Unsafe representation of a <see cref="TileMeshes"/> struct</summary>
    public struct TileMeshesUnsafe
    {
        public NativeArray<TileMesh.TileMeshUnsafe> tileMeshes;
        public IntRect tileRect;
        public Vector2 tileWorldSize;

        public TileMeshesUnsafe(NativeArray<TileMesh.TileMeshUnsafe> tileMeshes, IntRect tileRect, Vector2 tileWorldSize) : this()
        {
        }

        /// <summary>Copies the native data to managed data arrays which are easier to work with</summary>
        public TileMeshes ToManaged()
        {
            return default;
        }

        public void Dispose(Allocator allocator)
        {
        }
    }
}
