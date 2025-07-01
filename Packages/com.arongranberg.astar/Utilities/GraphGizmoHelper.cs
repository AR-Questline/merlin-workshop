using UnityEngine;

namespace Pathfinding.Util {
	using Pathfinding.Drawing;
	using Pathfinding.Collections;
	using Pathfinding.Pooling;

	/// <summary>Combines hashes into a single hash value</summary>
	public struct NodeHasher {
		readonly bool includePathSearchInfo;
		readonly bool includeAreaInfo;
		readonly bool includeHierarchicalNodeInfo;
		readonly PathHandler debugData;
		public DrawingData.Hasher hasher;

		public NodeHasher(AstarPath active) : this()
        {
        }

        public void HashNode(GraphNode node)
        {
        }

        public void Add<T>(T v)
        {
        }

        public static implicit operator DrawingData.Hasher(NodeHasher hasher)
        {
            return default;
        }
    }

    public class GraphGizmoHelper : IAstarPooledObject, System.IDisposable
    {
        public DrawingData.Hasher hasher { get; private set; }
        PathHandler debugData;
        ushort debugPathID;
        GraphDebugMode debugMode;
        public bool showSearchTree;
        float debugFloor;
        float debugRoof;
        public CommandBuilder builder;
        Vector3 drawConnectionStart;
        Color drawConnectionColor;
        readonly System.Action<GraphNode> drawConnection;
#if UNITY_EDITOR
        UnsafeSpan<GlobalNodeStorage.DebugPathNode> debugPathNodes;
#endif
        GlobalNodeStorage nodeStorage;

        public GraphGizmoHelper()
        {
        }

        public static GraphGizmoHelper GetSingleFrameGizmoHelper(DrawingData gizmos, AstarPath active, RedrawScope redrawScope)
        {
            return default;
        }

        public static GraphGizmoHelper GetGizmoHelper(DrawingData gizmos, AstarPath active, DrawingData.Hasher hasher, RedrawScope redrawScope)
        {
            return default;
        }

        public void Init (AstarPath active, DrawingData.Hasher hasher, DrawingData gizmos, RedrawScope redrawScope) {
        }

        public void OnEnterPool()
        {
        }

        public void DrawConnections(GraphNode node)
        {
        }

        void DrawConnection(GraphNode other) {
        }

        /// <summary>
        /// Color to use for gizmos.
        /// Returns a color to be used for the specified node with the current debug settings (editor only).
        ///
        /// Version: Since 3.6.1 this method will not handle null nodes
        /// </summary>
        public Color NodeColor(GraphNode node)
        {
            return default;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Returns if the node is in the search tree of the path.
        /// Only guaranteed to be correct if path is the latest path calculated.
        /// Use for gizmo drawing only.
        /// </summary>
        internal static bool InSearchTree(GraphNode node, UnsafeSpan<GlobalNodeStorage.DebugPathNode> debugPathNodes, ushort pathID)
        {
            return default;
        }
#endif

        public void DrawWireTriangle(Vector3 a, Vector3 b, Vector3 c, Color color)
        {
        }

        public void DrawTriangles(Vector3[] vertices, Color[] colors, int numTriangles)
        {
        }

        public void DrawWireTriangles(Vector3[] vertices, Color[] colors, int numTriangles)
        {
        }

        void System.IDisposable.Dispose()
        {
        }
    }
}
