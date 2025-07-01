using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Pathfinding.Util;
using UnityEngine.Profiling;
using System.Collections.Generic;
using Pathfinding.Jobs;
using Pathfinding.Graphs.Grid.Jobs;
using Pathfinding.Collections;
using Unity.Jobs.LowLevel.Unsafe;

namespace Pathfinding.Graphs.Grid {
	public struct GridGraphNodeData {
		public Allocator allocationMethod;
		public int numNodes;
		/// <summary>
		/// Bounds for the part of the graph that this data represents.
		/// For example if the first layer of a layered grid graph is being updated between x=10 and x=20, z=5 and z=15
		/// then this will be IntBounds(xmin=10, ymin=0, zmin=5, xmax=20, ymax=0, zmax=15)
		/// </summary>
		public IntBounds bounds;
		/// <summary>
		/// Number of layers that the data contains.
		/// For a non-layered grid graph this will always be 1.
		/// </summary>
		public int layers => bounds.size.y;

		/// <summary>
		/// Positions of all nodes.
		///
		/// Data is valid in these passes:
		/// - BeforeCollision: Valid
		/// - BeforeConnections: Valid
		/// - AfterConnections: Valid
		/// - AfterErosion: Valid
		/// - PostProcess: Valid
		/// </summary>
		public NativeArray<Vector3> positions;

		/// <summary>
		/// Bitpacked connections of all nodes.
		///
		/// Connections are stored in different formats depending on <see cref="layeredDataLayout"/>.
		/// You can use <see cref="LayeredGridAdjacencyMapper"/> and <see cref="FlatGridAdjacencyMapper"/> to access connections for the different data layouts.
		///
		/// Data is valid in these passes:
		/// - BeforeCollision: Invalid
		/// - BeforeConnections: Invalid
		/// - AfterConnections: Valid
		/// - AfterErosion: Valid (but will be overwritten)
		/// - PostProcess: Valid
		/// </summary>
		public NativeArray<ulong> connections;

		/// <summary>
		/// Bitpacked connections of all nodes.
		///
		/// Data is valid in these passes:
		/// - BeforeCollision: Valid
		/// - BeforeConnections: Valid
		/// - AfterConnections: Valid
		/// - AfterErosion: Valid
		/// - PostProcess: Valid
		/// </summary>
		public NativeArray<uint> penalties;

		/// <summary>
		/// Tags of all nodes
		///
		/// Data is valid in these passes:
		/// - BeforeCollision: Valid (but if erosion uses tags then it will be overwritten later)
		/// - BeforeConnections: Valid (but if erosion uses tags then it will be overwritten later)
		/// - AfterConnections: Valid (but if erosion uses tags then it will be overwritten later)
		/// - AfterErosion: Valid
		/// - PostProcess: Valid
		/// </summary>
		public NativeArray<int> tags;

		/// <summary>
		/// Normals of all nodes.
		/// If height testing is disabled the normal will be (0,1,0) for all nodes.
		/// If a node doesn't exist (only happens in layered grid graphs) or if the height raycast didn't hit anything then the normal will be (0,0,0).
		///
		/// Data is valid in these passes:
		/// - BeforeCollision: Valid
		/// - BeforeConnections: Valid
		/// - AfterConnections: Valid
		/// - AfterErosion: Valid
		/// - PostProcess: Valid
		/// </summary>
		public NativeArray<float4> normals;

		/// <summary>
		/// Walkability of all nodes before erosion happens.
		///
		/// Data is valid in these passes:
		/// - BeforeCollision: Valid (it will be combined with collision testing later)
		/// - BeforeConnections: Valid
		/// - AfterConnections: Valid
		/// - AfterErosion: Valid
		/// - PostProcess: Valid
		/// </summary>
		public NativeArray<bool> walkable;

		/// <summary>
		/// Walkability of all nodes after erosion happens. This is the final walkability of the nodes.
		/// If no erosion is used then the data will just be copied from the <see cref="walkable"/> array.
		///
		/// Data is valid in these passes:
		/// - BeforeCollision: Invalid
		/// - BeforeConnections: Invalid
		/// - AfterConnections: Invalid
		/// - AfterErosion: Valid
		/// - PostProcess: Valid
		/// </summary>
		public NativeArray<bool> walkableWithErosion;


		/// <summary>
		/// True if the data may have multiple layers.
		/// For layered data the nodes are laid out as `data[y*width*depth + z*width + x]`.
		/// For non-layered data the nodes are laid out as `data[z*width + x]` (which is equivalent to the above layout assuming y=0).
		///
		/// This also affects how node connections are stored. You can use <see cref="LayeredGridAdjacencyMapper"/> and <see cref="FlatGridAdjacencyMapper"/> to access
		/// connections for the different data layouts.
		/// </summary>
		public bool layeredDataLayout;

		public void AllocateBuffers (JobDependencyTracker dependencyTracker)
        {
        }

        public void TrackBuffers(JobDependencyTracker dependencyTracker)
        {
        }

        public void PersistBuffers(JobDependencyTracker dependencyTracker)
        {
        }

        public void Dispose()
        {
        }

        public JobHandle Rotate2D(int dx, int dz, JobHandle dependency)
        {
            return default;
        }

        public void ResizeLayerCount(int layerCount, JobDependencyTracker dependencyTracker)
        {
        }

        struct LightReader : GridIterationUtilities.ISliceAction
        {
            public GridNodeBase[] nodes;
			public UnsafeSpan<Vector3> nodePositions;
			public UnsafeSpan<bool> nodeWalkable;

			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			public void Execute (uint outerIdx, uint innerIdx) {
            }
        }

		public void ReadFromNodesForConnectionCalculations (GridNodeBase[] nodes, Slice3D slice, JobHandle nodesDependsOn, NativeArray<float4> graphNodeNormals, JobDependencyTracker dependencyTracker) {
        }

        void ReadNodeNormals(Slice3D slice, NativeArray<float4> graphNodeNormals, JobDependencyTracker dependencyTracker)
        {
        }

        public static GridGraphNodeData ReadFromNodes(GridNodeBase[] nodes, Slice3D slice, JobHandle nodesDependsOn, NativeArray<float4> graphNodeNormals, Allocator allocator, bool layeredDataLayout, JobDependencyTracker dependencyTracker)
        {
            return default;
        }

        public GridGraphNodeData ReadFromNodesAndCopy(GridNodeBase[] nodes, Slice3D slice, JobHandle nodesDependsOn, NativeArray<float4> graphNodeNormals, bool copyPenaltyAndTags, JobDependencyTracker dependencyTracker)
        {
            return default;
        }

        public void CopyFrom(GridGraphNodeData other, bool copyPenaltyAndTags, JobDependencyTracker dependencyTracker) => CopyFrom(other, IntBounds.Intersection(bounds, other.bounds), copyPenaltyAndTags, dependencyTracker);

        public void CopyFrom(GridGraphNodeData other, IntBounds bounds, bool copyPenaltyAndTags, JobDependencyTracker dependencyTracker)
        {
        }

        public JobHandle AssignToNodes(GridNodeBase[] nodes, int3 nodeArrayBounds, IntBounds writeMask, uint graphIndex, JobHandle nodesDependsOn, JobDependencyTracker dependencyTracker)
        {
            return default;
        }
    }

    public struct GridGraphScanData
    {
        /// <summary>
        /// Tracks dependencies between jobs to allow parallelism without tediously specifying dependencies manually.
        /// Always use when scheduling jobs.
        /// </summary>
        public JobDependencyTracker dependencyTracker;

        /// <summary>The up direction of the graph, in world space</summary>
        public Vector3 up;

        /// <summary>Transforms graph-space to world space</summary>
        public GraphTransform transform;

        /// <summary>Data for all nodes in the graph update that is being calculated</summary>
        public GridGraphNodeData nodes;

        /// <summary>
        /// Bounds of the data arrays.
        /// Deprecated: Use nodes.bounds or heightHitsBounds depending on if you are using the heightHits array or not
        /// </summary>
        [System.Obsolete("Use nodes.bounds or heightHitsBounds depending on if you are using the heightHits array or not")]
        public IntBounds bounds => nodes.bounds;

        /// <summary>
        /// True if the data may have multiple layers.
        /// For layered data the nodes are laid out as `data[y*width*depth + z*width + x]`.
        /// For non-layered data the nodes are laid out as `data[z*width + x]` (which is equivalent to the above layout assuming y=0).
        ///
        /// Deprecated: Use nodes.layeredDataLayout instead
        /// </summary>
        [System.Obsolete("Use nodes.layeredDataLayout instead")]
        public bool layeredDataLayout => nodes.layeredDataLayout;

        /// <summary>
        /// Raycasts hits used for height testing.
        /// This data is only valid if height testing is enabled, otherwise the array is uninitialized (heightHits.IsCreated will be false).
        ///
        /// Data is valid in these passes:
        /// - BeforeCollision: Valid (if height testing is enabled)
        /// - BeforeConnections: Valid (if height testing is enabled)
        /// - AfterConnections: Valid (if height testing is enabled)
        /// - AfterErosion: Valid (if height testing is enabled)
        /// - PostProcess: Valid (if height testing is enabled)
        ///
        /// Warning: This array does not have the same size as the arrays in <see cref="nodes"/>. It will usually be slightly smaller. See <see cref="heightHitsBounds"/>.
        /// </summary>
        public NativeArray<RaycastHit> heightHits;

        /// <summary>
        /// Bounds for the <see cref="heightHits"/> array.
        ///
        /// During an update, the scan data may contain more nodes than we are doing height testing for.
        /// For a few nodes around the update, the data will be read from the existing graph, instead. This is done for performance.
        /// This means that there may not be any height testing information these nodes.
        /// However, all nodes that will be written to will always have height testing information.
        /// </summary>
        public IntBounds heightHitsBounds;

        /// <summary>
        /// Node positions.
        /// Deprecated: Use <see cref="nodes.positions"/> instead
        /// </summary>
        [System.Obsolete("Use nodes.positions instead")]
        public NativeArray<Vector3> nodePositions => nodes.positions;

        /// <summary>
        /// Node connections.
        /// Deprecated: Use <see cref="nodes.connections"/> instead
        /// </summary>
        [System.Obsolete("Use nodes.connections instead")]
        public NativeArray<ulong> nodeConnections => nodes.connections;

        /// <summary>
        /// Node penalties.
        /// Deprecated: Use <see cref="nodes.penalties"/> instead
        /// </summary>
        [System.Obsolete("Use nodes.penalties instead")]
        public NativeArray<uint> nodePenalties => nodes.penalties;

        /// <summary>
        /// Node tags.
        /// Deprecated: Use <see cref="nodes.tags"/> instead
        /// </summary>
        [System.Obsolete("Use nodes.tags instead")]
        public NativeArray<int> nodeTags => nodes.tags;

        /// <summary>
        /// Node normals.
        /// Deprecated: Use <see cref="nodes.normals"/> instead
        /// </summary>
        [System.Obsolete("Use nodes.normals instead")]
        public NativeArray<float4> nodeNormals => nodes.normals;

        /// <summary>
        /// Node walkability.
        /// Deprecated: Use <see cref="nodes.walkable"/> instead
        /// </summary>
        [System.Obsolete("Use nodes.walkable instead")]
        public NativeArray<bool> nodeWalkable => nodes.walkable;

        /// <summary>
        /// Node walkability with erosion.
        /// Deprecated: Use <see cref="nodes.walkableWithErosion"/> instead
        /// </summary>
        [System.Obsolete("Use nodes.walkableWithErosion instead")]
        public NativeArray<bool> nodeWalkableWithErosion => nodes.walkableWithErosion;

        public void SetDefaultPenalties(uint initialPenalty)
        {
        }

        public void SetDefaultNodePositions(GraphTransform transform)
        {
        }

        public JobHandle HeightCheck(GraphCollision collision, int maxHits, IntBounds recalculationBounds, NativeArray<int> outLayerCount, float characterHeight, Allocator allocator)
        {
            return default;
        }

        public void CopyHits(IntBounds recalculationBounds)
        {
        }

        public void CalculateWalkabilityFromHeightData(bool useRaycastNormal, bool unwalkableWhenNoGround, float maxSlope, float characterHeight)
        {
        }

        public IEnumerator<JobHandle> CollisionCheck(GraphCollision collision, IntBounds calculationBounds)
        {
            return default;
        }

        public void Connections(float maxStepHeight, bool maxStepUsesSlope, IntBounds calculationBounds, NumNeighbours neighbours, bool cutCorners, bool use2D, bool useErodedWalkability, float characterHeight)
        {
        }

        public void Erosion(NumNeighbours neighbours, int erodeIterations, IntBounds erosionWriteMask, bool erosionUsesTags, int erosionStartTag, int erosionTagsPrecedenceMask)
        {
        }

        public void AssignNodeConnections(GridNodeBase[] nodes, int3 nodeArrayBounds, IntBounds writeBounds)
        {
        }
    }
}
