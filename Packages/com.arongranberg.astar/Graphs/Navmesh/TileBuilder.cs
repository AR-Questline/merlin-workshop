using System.Collections.Generic;
using Pathfinding.Graphs.Navmesh.Jobs;
using Pathfinding.Jobs;
using Pathfinding.Pooling;
using Pathfinding.Sync;
using Pathfinding.Graphs.Navmesh.Voxelization.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Assertions;

namespace Pathfinding.Graphs.Navmesh {
	/// <summary>
	/// Settings for building tile meshes in a recast graph.
	///
	/// See: <see cref="RecastGraph"/> for more documentation on the individual fields.
	/// See: <see cref="RecastBuilder"/>
	/// </summary>
	public struct TileBuilder {
		public float walkableClimb;
		public RecastGraph.CollectionSettings collectionSettings;
		public RecastGraph.RelevantGraphSurfaceMode relevantGraphSurfaceMode;
		public RecastGraph.DimensionMode dimensionMode;
		public RecastGraph.BackgroundTraversability backgroundTraversability;

		// TODO: Don't store in struct
		public int tileBorderSizeInVoxels;
		public float walkableHeight;
		public float maxSlope;
		// TODO: Specify in world units
		public int characterRadiusInVoxels;
		public int minRegionSize;
		public float maxEdgeLength;
		public float contourMaxError;
		public UnityEngine.SceneManagement.Scene scene;
		public TileLayout tileLayout;
		public IntRect tileRect;
		public List<RecastGraph.PerLayerModification> perLayerModifications;

		public class TileBuilderOutput : IProgress, System.IDisposable {
			public NativeReference<int> currentTileCounter;
			public TileMeshesUnsafe tileMeshes;
#if UNITY_EDITOR
			public List<(UnityEngine.Object, Mesh)> meshesUnreadableAtRuntime;
#endif

			public float Progress {
				get {
					var tileCount = tileMeshes.tileRect.Area;
					var currentTile = Mathf.Min(tileCount, currentTileCounter.Value);
					return tileCount > 0 ? currentTile / (float)tileCount : 0; // "Scanning tiles: " + currentTile + " of " + (tileCount) + " tiles...");
				}
			}

			public void Dispose () {
            }
        }

		public TileBuilder (RecastGraph graph, TileLayout tileLayout, IntRect tileRect) : this()
        {
        }

        /// <summary>
        /// Number of extra voxels on each side of a tile to ensure accurate navmeshes near the tile border.
        /// The width of a tile is expanded by 2 times this value (1x to the left and 1x to the right)
        /// </summary>
        int TileBorderSizeInVoxels
        {
            get
            {
                return characterRadiusInVoxels + 3;
            }
        }

        float TileBorderSizeInWorldUnits
        {
            get
            {
                return TileBorderSizeInVoxels * tileLayout.cellSize;
            }
        }

        /// <summary>Get the world space bounds for all tiles, including an optional (graph space) padding around the tiles in the x and z axis</summary>
        public Bounds GetWorldSpaceBounds(float xzPadding = 0)
        {
            return default;
        }

        public RecastMeshGatherer.MeshCollection CollectMeshes(Bounds bounds)
        {
            return default;
        }

        /// <summary>A mapping from tiles to the meshes that each tile touches</summary>
        public struct BucketMapping
        {
            /// <summary>All meshes that should be voxelized</summary>
            public NativeArray<RasterizationMesh> meshes;
            /// <summary>Indices into the <see cref="meshes"/> array</summary>
            public NativeArray<int> pointers;
            /// <summary>
            /// For each tile, the range of pointers in <see cref="pointers"/> that correspond to that tile.
            /// This is a cumulative sum of the number of pointers in each bucket.
            ///
            /// Bucket i will contain pointers in the range [i > 0 ? bucketRanges[i-1] : 0, bucketRanges[i]).
            ///
            /// The length is the same as the number of tiles.
            /// </summary>
            public NativeArray<int> bucketRanges;
        }

        /// <summary>Creates a list for every tile and adds every mesh that touches a tile to the corresponding list</summary>
        BucketMapping PutMeshesIntoTileBuckets(RecastMeshGatherer.MeshCollection meshCollection, IntRect tileBuckets)
        {
            return default;
        }

        public Promise<TileBuilderOutput> Schedule(DisposeArena arena)
        {
            return default;
        }
    }
}
