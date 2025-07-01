using Pathfinding.Sync;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;

namespace Pathfinding {
	using Pathfinding.Util;
	using Pathfinding.RVO;
	using Pathfinding.Jobs;
	using Pathfinding.Drawing;
	using Pathfinding.Collections;

	[BurstCompile]
	public class NavmeshEdges {
		public RVO.SimulatorBurst.ObstacleData obstacleData;
		SpinLock allocationLock = new SpinLock();
		const int JobRecalculateObstaclesBatchCount = 32;
		RWLock rwLock = new RWLock();
		public HierarchicalGraph hierarchicalGraph;
		int gizmoVersion = 0;

		public void Dispose ()
        {
        }

        void Init()
        {
        }

        public JobHandle RecalculateObstacles(NativeList<int> dirtyHierarchicalNodes, NativeReference<int> numHierarchicalNodes, JobHandle dependency)
        {
            return default;
        }

        public void OnDrawGizmos(DrawingData gizmos, RedrawScope redrawScope)
        {
        }

        /// <summary>
        /// Obstacle data for navmesh edges.
        ///
        /// Can be queried in burst jobs.
        /// </summary>
        public NavmeshBorderData GetNavmeshEdgeData(out RWLock.CombinedReadLockAsync readLock)
        {
            readLock = default(RWLock.CombinedReadLockAsync);
            return default;
        }

        [BurstCompile]
        struct JobResizeObstacles : IJob
        {
            public NativeList<UnmanagedObstacle> obstacles;
			public NativeReference<int> numHierarchicalNodes;

			public void Execute ()
            {
            }
        }

        struct JobCalculateObstacles : IJobParallelForBatch
        {
            public System.Runtime.InteropServices.GCHandle hGraphGC;
			public SlabAllocator<float3> obstacleVertices;
			public SlabAllocator<ObstacleVertexGroup> obstacleVertexGroups;
			[NativeDisableParallelForRestriction]
			public NativeArray<UnmanagedObstacle> obstacles;
			[NativeDisableParallelForRestriction]
			public NativeArray<Bounds> bounds;
			[ReadOnly]
			public NativeList<int> dirtyHierarchicalNodes;
			[NativeDisableUnsafePtrRestriction]
			public unsafe SpinLock* allocationLock;

			public void Execute (int startIndex, int count) {
            }

            private static readonly ProfilerMarker MarkerBBox = new ProfilerMarker("HierarchicalBBox");
            private static readonly ProfilerMarker MarkerObstacles = new ProfilerMarker("CalculateObstacles");
            private static readonly ProfilerMarker MarkerCollect = new ProfilerMarker("Collect");
            private static readonly ProfilerMarker MarkerTrace = new ProfilerMarker("Trace");

            void CalculateBoundingBox(HierarchicalGraph hGraph, int hierarchicalNode)
            {
            }

            void CalculateObstacles(HierarchicalGraph hGraph, int hierarchicalNode, SlabAllocator<ObstacleVertexGroup> obstacleVertexGroups, SlabAllocator<float3> obstacleVertices, NativeArray<UnmanagedObstacle> obstacles, NativeList<RVO.RVOObstacleCache.ObstacleSegment> edgesScratch)
            {
            }
        }

        /// <summary>
        /// Burst-accessible data about borders in the navmesh.
        ///
        /// Can be queried from burst, and from multiple threads in parallel.
        /// </summary>
        // TODO: Change to a quadtree/kdtree/aabb tree that stored edges as { index: uint10, prev: uint10, next: uint10 }, with a natural max of 1024 vertices per obstacle (hierarchical node). This is fine because hnodes have at most 256 nodes, which cannot create more than 1024 edges.
        public struct NavmeshBorderData
        {
            public HierarchicalGraph.HierarhicalNodeData hierarhicalNodeData;
            public RVO.SimulatorBurst.ObstacleData obstacleData;

            /// <summary>
            /// An empty set of edges.
            ///
            /// Must be disposed using <see cref="DisposeEmpty"/>.
            /// </summary>
            public static NavmeshBorderData CreateEmpty(Allocator allocator)
            {
                return default;
            }

            public void DisposeEmpty(JobHandle dependsOn)
            {
            }

            static void GetHierarchicalNodesInRangeRec(int hierarchicalNode, Bounds bounds, SlabAllocator<int> connectionAllocator, [NoAlias] NativeList<int> connectionAllocations, NativeList<Bounds> nodeBounds, [NoAlias] NativeList<int> indices)
            {
            }

            static unsafe void ConvertObstaclesToEdges(ref RVO.SimulatorBurst.ObstacleData obstacleData, NativeList<int> obstacleIndices, Bounds localBounds, NativeList<float2> edgeBuffer, NativeMovementPlane movementPlane)
            {
            }

            public void GetObstaclesInRange(int hierarchicalNode, Bounds bounds, NativeList<int> obstacleIndexBuffer)
            {
            }

            public void GetEdgesInRange(int hierarchicalNode, Bounds localBounds, NativeList<float2> edgeBuffer, NativeList<int> scratchBuffer, NativeMovementPlane movementPlane)
            {
            }
        }
	}
}
