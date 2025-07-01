using Pathfinding.Jobs;
using Pathfinding.Util;
using Pathfinding.Graphs.Navmesh.Voxelization.Burst;
using Pathfinding.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using Unity.Profiling;

namespace Pathfinding.Graphs.Navmesh.Jobs {
	/// <summary>
	/// Scratch space for building navmesh tiles using voxelization.
	///
	/// This uses quite a lot of memory, so it is used by a single worker thread for multiple tiles in order to minimize allocations.
	/// </summary>
	public struct TileBuilderBurst : IArenaDisposable {
		public LinkedVoxelField linkedVoxelField;
		public CompactVoxelField compactVoxelField;
		public NativeList<ushort> distanceField;
		public NativeQueue<Int3> tmpQueue1;
		public NativeQueue<Int3> tmpQueue2;
		public NativeList<VoxelContour> contours;
		public NativeList<int> contourVertices;
		public VoxelMesh voxelMesh;

		public TileBuilderBurst (int width, int depth, int voxelWalkableHeight, int maximumVoxelYCoord) : this()
        {
        }

        void IArenaDisposable.DisposeWith(DisposeArena arena)
        {
        }
    }

    /// <summary>
    /// Builds tiles from a polygon soup using voxelization.
    ///
    /// This job takes the following steps:
    /// - Voxelize the input meshes
    /// - Filter and process the resulting voxelization in various ways to remove unwanted artifacts and make it better suited for pathfinding.
    /// - Extract a walkable surface from the voxelization.
    /// - Triangulate this surface and create navmesh tiles from it.
    ///
    /// This job uses work stealing to distribute the work between threads. The communication happens using a shared queue and the <see cref="currentTileCounter"/> atomic variable.
    /// </summary>
    [BurstCompile(CompileSynchronously = true)]
    // TODO: [BurstCompile(FloatMode = FloatMode.Fast)]
    public struct JobBuildTileMeshFromVoxels : IJob
    {
        public TileBuilderBurst tileBuilder;
        [ReadOnly]
        public TileBuilder.BucketMapping inputMeshes;
        [ReadOnly]
        public NativeArray<Bounds> tileGraphSpaceBounds;
        public Matrix4x4 voxelToTileSpace;

        /// <summary>
        /// Limits of the graph space bounds for the whole graph on the XZ plane.
        ///
        /// Used to crop the border tiles to exactly the limits of the graph's bounding box.
        /// </summary>
        public Vector2 graphSpaceLimits;

        [NativeDisableUnsafePtrRestriction]
        public unsafe TileMesh.TileMeshUnsafe* outputMeshes;

        /// <summary>Max number of tiles to process in this job</summary>
        public int maxTiles;

        public int voxelWalkableClimb;
        public uint voxelWalkableHeight;
        public float cellSize;
        public float cellHeight;
        public float maxSlope;
        public RecastGraph.DimensionMode dimensionMode;
        public RecastGraph.BackgroundTraversability backgroundTraversability;
        public Matrix4x4 graphToWorldSpace;
        public int characterRadiusInVoxels;
        public int tileBorderSizeInVoxels;
        public int minRegionSize;
        public float maxEdgeLength;
        public float contourMaxError;
        [ReadOnly]
        public NativeArray<JobBuildRegions.RelevantGraphSurfaceInfo> relevantGraphSurfaces;
        public RecastGraph.RelevantGraphSurfaceMode relevantGraphSurfaceMode;

        [NativeDisableUnsafePtrRestriction]
        public unsafe int* currentTileCounter;

        public void SetOutputMeshes(NativeArray<TileMesh.TileMeshUnsafe> arr)
        {
        }

        public void SetCounter(NativeReference<int> counter)
        {
        }

        private static readonly ProfilerMarker MarkerVoxelize = new ProfilerMarker("Voxelize");
        private static readonly ProfilerMarker MarkerFilterLedges = new ProfilerMarker("FilterLedges");
        private static readonly ProfilerMarker MarkerFilterLowHeightSpans = new ProfilerMarker("FilterLowHeightSpans");
		private static readonly ProfilerMarker MarkerBuildCompactField = new ProfilerMarker("BuildCompactField");
        private static readonly ProfilerMarker MarkerBuildConnections = new ProfilerMarker("BuildConnections");
        private static readonly ProfilerMarker MarkerErodeWalkableArea = new ProfilerMarker("ErodeWalkableArea");
        private static readonly ProfilerMarker MarkerBuildDistanceField = new ProfilerMarker("BuildDistanceField");
        private static readonly ProfilerMarker MarkerBuildRegions = new ProfilerMarker("BuildRegions");
		private static readonly ProfilerMarker MarkerBuildContours = new ProfilerMarker("BuildContours");
        private static readonly ProfilerMarker MarkerBuildMesh = new ProfilerMarker("BuildMesh");
        private static readonly ProfilerMarker MarkerConvertAreasToTags = new ProfilerMarker("ConvertAreasToTags");
        private static readonly ProfilerMarker MarkerRemoveDuplicateVertices = new ProfilerMarker("RemoveDuplicateVertices");
        private static readonly ProfilerMarker MarkerTransformTileCoordinates = new ProfilerMarker("TransformTileCoordinates");

        public void Execute()
        {
        }
    }
}
