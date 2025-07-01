using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Pathfinding {
	using Pathfinding.Collections;
	using Unity.Profiling;

	/// <summary>
	/// NNConstraint which also takes an <see cref="ITraversalProvider"/> into account.
	///
	/// Paths will automatically use this if an ITraversalProvider is set on the path.
	/// </summary>
	public class NNConstraintWithTraversalProvider : NNConstraint {
		public ITraversalProvider traversalProvider;
		public NNConstraint baseConstraint;
		public Path path;

		public void Reset () {
        }

        public bool isSet => traversalProvider != null;

        public void Set(Path path, NNConstraint constraint, ITraversalProvider traversalProvider)
        {
        }

        public override bool SuitableGraph(int graphIndex, NavGraph graph)
        {
            return default;
        }

        public override bool Suitable(GraphNode node)
        {
            return default;
        }
    }

	/// <summary>
	/// Stores temporary node data for a single pathfinding request.
	/// Every node has one PathNode per thread used.
	/// It stores e.g G score, H score and other temporary variables needed
	/// for path calculation, but which are not part of the graph structure.
	///
	/// See: Pathfinding.PathHandler
	/// See: https://en.wikipedia.org/wiki/A*_search_algorithm
	/// </summary>
	public struct PathNode {
		/// <summary>The path request (in this thread, if multithreading is used) which last used this node</summary>
		public ushort pathID;

		/// <summary>
		/// Index of the node in the binary heap.
		/// The open list in the A* algorithm is backed by a binary heap.
		/// To support fast 'decrease key' operations, the index of the node
		/// is saved here.
		/// </summary>
		public ushort heapIndex;

		/// <summary>Bitpacked variable which stores several fields</summary>
		private uint flags;

		public static readonly PathNode Default = new PathNode { pathID = 0, heapIndex = BinaryHeap.NotInHeap, flags = 0 };

		/// <summary>Parent index uses the first 26 bits</summary>
		private const uint ParentIndexMask = (1U << 26) - 1U;

		private const int FractionAlongEdgeOffset = 26;
		private const uint FractionAlongEdgeMask = ((1U << 30) - 1U) & ~ParentIndexMask;
		public const int FractionAlongEdgeQuantization = 1 << (30 - 26);

		public static uint ReverseFractionAlongEdge(uint v) => (FractionAlongEdgeQuantization - 1) - v;

		public static uint QuantizeFractionAlongEdge (float v) {
            return default;
        }

        public static float UnQuantizeFractionAlongEdge(uint v)
        {
            return default;
        }

        /// <summary>Flag 1 is at bit 30</summary>
        private const int Flag1Offset = 30;
		private const uint Flag1Mask = 1U << Flag1Offset;

		/// <summary>Flag 2 is at bit 31</summary>
		private const int Flag2Offset = 31;
		private const uint Flag2Mask = 1U << Flag2Offset;

		public uint fractionAlongEdge {
			get => (flags & FractionAlongEdgeMask) >> FractionAlongEdgeOffset;
			set => flags = (flags & ~FractionAlongEdgeMask) | ((value << FractionAlongEdgeOffset) & FractionAlongEdgeMask);
		}

		public uint parentIndex {
			get => flags & ParentIndexMask;
			set => flags = (flags & ~ParentIndexMask) | value;
		}

		/// <summary>
		/// Use as temporary flag during pathfinding.
		/// Path types can use this during pathfinding to mark
		/// nodes. When done, this flag should be reverted to its default state (false) to
		/// avoid messing up other pathfinding requests.
		/// </summary>
		public bool flag1 {
			get => (flags & Flag1Mask) != 0;
			set => flags = (flags & ~Flag1Mask) | (value ? Flag1Mask : 0U);
		}

		/// <summary>
		/// Use as temporary flag during pathfinding.
		/// Path types can use this during pathfinding to mark
		/// nodes. When done, this flag should be reverted to its default state (false) to
		/// avoid messing up other pathfinding requests.
		/// </summary>
		public bool flag2 {
			get => (flags & Flag2Mask) != 0;
			set => flags = (flags & ~Flag2Mask) | (value ? Flag2Mask : 0U);
		}
	}

	public enum TemporaryNodeType {
		Start,
		End,
		Ignore,
	}

	public struct TemporaryNode {
		public uint associatedNode;
		public Int3 position;
		public int targetIndex;
		public TemporaryNodeType type;
	}

	/// <summary>Handles thread specific path data.</summary>
	public class PathHandler {
		/// <summary>
		/// Current PathID.
		/// See: <see cref="PathID"/>
		/// </summary>
		private ushort pathID;

		public readonly int threadID;
		public readonly int totalThreadCount;
		public readonly NNConstraintWithTraversalProvider constraintWrapper = new NNConstraintWithTraversalProvider();
		internal readonly GlobalNodeStorage nodeStorage;
		public int numTemporaryNodes { [IgnoredByDeepProfiler] get; private set; }

		/// <summary>
		/// All path nodes with an index greater or equal to this are temporary nodes that only exist for the duration of a single path.
		///
		/// This is a copy of NodeStorage.nextNodeIndex. This is used to avoid having to access the NodeStorage while pathfinding as it's an extra indirection.
		/// </summary>
		public uint temporaryNodeStartIndex { [IgnoredByDeepProfiler] get; private set; }
		UnsafeSpan<TemporaryNode> temporaryNodes;

		/// <summary>
		/// Reference to the per-node data for this thread.
		///
		/// Note: Only guaranteed to point to a valid allocation while the path is being calculated.
		///
		/// Be careful when storing copies of this array, as it may be re-allocated by the AddTemporaryNode method.
		/// </summary>
		public UnsafeSpan<PathNode> pathNodes;
#if UNITY_EDITOR
		UnsafeSpan<GlobalNodeStorage.DebugPathNode> debugPathNodes;
#endif

		/// <summary>
		/// Binary heap to keep track of nodes on the "Open list".
		/// See: https://en.wikipedia.org/wiki/A*_search_algorithm
		/// </summary>
		public BinaryHeap heap = new BinaryHeap(128);

		/// <summary>ID for the path currently being calculated or last path that was calculated</summary>
		public ushort PathID { get { return pathID; } }

		/// <summary>
		/// StringBuilder that paths can use to build debug strings.
		/// Better for performance and memory usage to use a single StringBuilder instead of each path creating its own
		/// </summary>
		public readonly System.Text.StringBuilder DebugStringBuilder = new System.Text.StringBuilder();

		internal PathHandler (GlobalNodeStorage nodeStorage, int threadID, int totalThreadCount) {
        }

        public void InitializeForPath (Path p) {
        }

        /// <summary>
        /// Returns the PathNode corresponding to the specified node.
        /// The PathNode is specific to this PathHandler since multiple PathHandlers
        /// are used at the same time if multithreading is enabled.
        /// </summary>
        public PathNode GetPathNode(GraphNode node, uint variant = 0)
        {
            return default;
        }

        public bool IsTemporaryNode(uint pathNodeIndex) => pathNodeIndex >= temporaryNodeStartIndex;

        /// <summary>
        /// Add a new temporary node for this path request.
        ///
        /// Warning: This may invalidate all memory references to path nodes in this path.
        /// </summary>
        public uint AddTemporaryNode(TemporaryNode node)
        {
            return default;
        }

        public GraphNode GetNode(uint nodeIndex) => nodeStorage.GetNode(nodeIndex);

        public ref TemporaryNode GetTemporaryNode(uint nodeIndex)
        {
	        throw new NotImplementedException();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void LogVisitedNode(uint pathNodeIndex, uint h, uint g)
        {
        }

        /// <summary>
        /// Set all nodes' pathIDs to 0.
        /// See: Pathfinding.PathNode.pathID
        /// </summary>
        public void ClearPathIDs()
        {
        }

        public void Dispose()
        {
        }
    }
}
