using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEngine.Profiling;
using Pathfinding.Collections;

namespace Pathfinding {
	using Pathfinding.Util;
	using Pathfinding.Jobs;
	using UnityEngine.Assertions;

	internal class GlobalNodeStorage {
		readonly AstarPath astar;
		Unity.Jobs.JobHandle lastAllocationJob;

		/// <summary>
		/// Holds the next node index which has not been used by any previous node.
		/// See: <see cref="nodeIndexPools"/>
		/// </summary>
		public uint nextNodeIndex = 1;

		/// <summary>
		/// The number of nodes for which path node data has been reserved.
		/// Will be at least as high as <see cref="nextNodeIndex"/>
		/// </summary>
		public uint reservedPathNodeData = 0;

		/// <summary>Number of nodes that have been destroyed in total</summary>
		public uint destroyedNodesVersion { get; private set; }

		const int InitialTemporaryNodes = 256;

		int temporaryNodeCount = InitialTemporaryNodes;

		/// <summary>
		/// Holds indices for nodes that have been destroyed.
		/// To avoid trashing a lot of memory structures when nodes are
		/// frequently deleted and created, node indices are reused.
		///
		/// There's one pool for each possible number of node variants (1, 2 and 3).
		/// </summary>
		internal readonly IndexedStack<uint>[] nodeIndexPools = new [] {
			new IndexedStack<uint>(),
			new IndexedStack<uint>(),
			new IndexedStack<uint>(),
		};

		public PathfindingThreadData[] pathfindingThreadData = new PathfindingThreadData[0];

		/// <summary>Maps from NodeIndex to node</summary>
		GraphNode[] nodes = new GraphNode[0];

		public GlobalNodeStorage (AstarPath astar) {
        }

        public GraphNode GetNode(uint nodeIndex) => nodes[nodeIndex];

#if UNITY_EDITOR
		public struct DebugPathNode {
			public uint g;
			public uint h;
			public uint parentIndex;
			public ushort pathID;
			public byte fractionAlongEdge;
		}
#endif

		public struct PathfindingThreadData {
			public UnsafeSpan<PathNode> pathNodes;
#if UNITY_EDITOR
			public UnsafeSpan<DebugPathNode> debugPathNodes;
#endif
		}

		internal class IndexedStack<T> {
			T[] buffer = new T[4];

			public int Count { get; private set; }

			public void Push (T v) {
            }

            public void Clear () {
            }

            public T Pop () {
                return default;
            }

            /// <summary>Pop the last N elements and store them in the buffer. The items will be in insertion order.</summary>
            public void PopMany(T[] resultBuffer, int popCount)
            {
            }
        }

		void DisposeThreadData () {
        }

        public void SetThreadCount (int threadCount)
        {
        }

        /// <summary>
        /// Grows temporary node storage for the given thread.
        ///
        /// This can happen if a path traverses a lot of off-mesh links, or if it is a multi-target path with a lot of targets.
        ///
        /// If enough nodes are created that we have a to grow the regular node storage, then the number of temporary nodes will grow to the same value on all threads.
        /// </summary>
        public void GrowTemporaryNodeStorage(int threadID)
        {
        }

        /// <summary>
        /// Initializes temporary path data for a node.
        /// Warning: This method should not be called directly.
        ///
        /// See: <see cref="AstarPath.InitializeNode"/>
        /// </summary>
        public void InitializeNode(GraphNode node)
        {
        }

        /// <summary>
        /// Reserves space for global node data.
        ///
        /// Warning: Must be called only when a lock is held on this object.
        /// </summary>
        void ReserveNodeIndices(uint nextNodeIndex)
        {
        }

        /// <summary>
        /// Destroyes the given node.
        /// This is to be called after the node has been disconnected from the graph so that it cannot be reached from any other nodes.
        /// It should only be called during graph updates, that is when the pathfinding threads are either not running or paused.
        ///
        /// Warning: This method should not be called by user code. It is used internally by the system.
        /// </summary>
        public void DestroyNode(GraphNode node)
        {
        }

        public void OnDisable()
        {
        }

        struct JobAllocateNodes<T> : IJob where T : GraphNode
        {
            public T[] result;
			public int count;
			public GlobalNodeStorage nodeStorage;
			public uint variantsPerNode;
			public System.Func<T> createNode;

			public bool allowBoundsChecks => false;

			public void Execute () {
            }
        }

        public Unity.Jobs.JobHandle AllocateNodesJob<T>(T[] result, int count, System.Func<T> createNode, uint variantsPerNode) where T : GraphNode
        {
            return default;
        }
    }
}
