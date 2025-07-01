#if !ASTAR_NO_GRID_GRAPH
#if !ASTAR_LEVELGRIDNODE_MORE_LAYERS
#define ASTAR_LEVELGRIDNODE_FEW_LAYERS
#endif
using UnityEngine;
using System.Collections.Generic;
using Pathfinding.Serialization;

namespace Pathfinding {
	/// <summary>
	/// Describes a single node for the LayerGridGraph.
	/// Works almost the same as a grid node, except that it also stores to which layer the connections go to
	/// </summary>
	public class LevelGridNode : GridNodeBase {
		public LevelGridNode() {
        }

        public LevelGridNode (AstarPath astar) {
        }

        private static LayerGridGraph[] _gridGraphs = new LayerGridGraph[0];
		public static LayerGridGraph GetGridGraph (uint graphIndex) {
            return default;
        }

        public static void SetGridGraph (int graphIndex, LayerGridGraph graph) {
        }

        public static void ClearGridGraph (int graphIndex, LayerGridGraph graph) {
        }

#if ASTAR_LEVELGRIDNODE_FEW_LAYERS
        public uint gridConnections;
#else
		public ulong gridConnections;
#endif

		protected static LayerGridGraph[] gridGraphs;

		const int MaxNeighbours = 8;
#if ASTAR_LEVELGRIDNODE_FEW_LAYERS
		public const int ConnectionMask = 0xF;
		public const int ConnectionStride = 4;
		public const int AxisAlignedConnectionsMask = 0xFFFF;
		public const uint AllConnectionsMask = 0xFFFFFFFF;
#else
		public const int ConnectionMask = 0xFF;
		public const int ConnectionStride = 8;
		public const ulong AxisAlignedConnectionsMask = 0xFFFFFFFF;
		public const ulong AllConnectionsMask = 0xFFFFFFFFFFFFFFFF;
#endif
		public const int NoConnection = ConnectionMask;

		internal const ulong DiagonalConnectionsMask = ((ulong)NoConnection << 4*ConnectionStride) | ((ulong)NoConnection << 5*ConnectionStride) | ((ulong)NoConnection << 6*ConnectionStride) | ((ulong)NoConnection << 7*ConnectionStride);

		/// <summary>
		/// Maximum number of layers the layered grid graph supports.
		///
		/// This can be changed in the A* Inspector -> Optimizations tab by enabling or disabling the ASTAR_LEVELGRIDNODE_MORE_LAYERS option.
		/// </summary>
		public const int MaxLayerCount = ConnectionMask;

		/// <summary>
		/// Removes all grid connections from this node.
		///
		/// Warning: Using this method can make the graph data inconsistent. It's recommended to use other ways to update the graph, instead.
		/// </summary>
		public override void ResetConnectionsInternal () {
        }

#if ASTAR_LEVELGRIDNODE_FEW_LAYERS
        public override bool HasAnyGridConnections => gridConnections != unchecked ((uint)-1);
#else
		public override bool HasAnyGridConnections => gridConnections != unchecked ((ulong)-1);
#endif

		public override bool HasConnectionsToAllEightNeighbours {
			get {
				for (int i = 0; i < 8; i++) {
					if (!HasConnectionInDirection(i)) return false;
				}
				return true;
			}
		}

		public override bool HasConnectionsToAllAxisAlignedNeighbours {
			get {
				return (gridConnections & AxisAlignedConnectionsMask) == AxisAlignedConnectionsMask;
			}
		}

		/// <summary>
		/// Layer coordinate of the node in the grid.
		/// If there are multiple nodes in the same (x,z) cell, then they will be stored in different layers.
		/// Together with NodeInGridIndex, you can look up the node in the nodes array
		/// <code>
		/// int index = node.NodeInGridIndex + node.LayerCoordinateInGrid * graph.width * graph.depth;
		/// Assert(node == graph.nodes[index]);
		/// </code>
		///
		/// See: XCoordInGrid
		/// See: ZCoordInGrid
		/// See: NodeInGridIndex
		/// </summary>
		public int LayerCoordinateInGrid { get { return nodeInGridIndex >> NodeInGridIndexLayerOffset; } set { nodeInGridIndex = (nodeInGridIndex & NodeInGridIndexMask) | (value << NodeInGridIndexLayerOffset); } }

		public void SetPosition (Int3 position) {
        }

        public override int GetGizmoHashCode () {
            return default;
        }

        public override GridNodeBase GetNeighbourAlongDirection (int direction) {
            return default;
        }

        public override void ClearConnections (bool alsoReverse) {
        }

        public override void GetConnections<T>(GetConnectionsWithData<T> action, ref T data, int connectionFilter) {
        }

        public override bool HasConnectionInDirection (int direction) {
            return default;
        }

        /// <summary>
        /// Set which layer a grid connection goes to.
        ///
        /// Warning: Using this method can make the graph data inconsistent. It's recommended to use other ways to update the graph, instead.
        /// </summary>
        /// <param name="dir">Direction for the connection.</param>
        /// <param name="value">The layer of the connected node or #NoConnection if there should be no connection in that direction.</param>
        public void SetConnectionValue (int dir, int value) {
        }

#if ASTAR_LEVELGRIDNODE_FEW_LAYERS
        public void SetAllConnectionInternal (ulong value) {
        }
#else
		public void SetAllConnectionInternal (ulong value) {
			gridConnections = value;
		}
#endif


        /// <summary>
        /// Which layer a grid connection goes to.
        /// Returns: The layer of the connected node or <see cref="NoConnection"/> if there is no connection in that direction.
        /// </summary>
        /// <param name="dir">Direction for the connection.</param>
        public int GetConnectionValue (int dir) {
            return default;
        }

        public override void AddPartialConnection (GraphNode node, uint cost, bool isOutgoing, bool isIncoming) {
        }

        public override void RemovePartialConnection (GraphNode node) {
        }

        /// <summary>
        /// Removes a connection from the internal grid connections, not the list of custom connections.
        /// See: SetConnectionValue
        /// </summary>
        protected void RemoveGridConnection (LevelGridNode node) {
        }

        public override bool GetPortal (GraphNode other, out Vector3 left, out Vector3 right) {
            left = default(Vector3);
            right = default(Vector3);
            return default;
        }

        public override void Open (Path path, uint pathNodeIndex, uint gScore) {
        }

        public override void SerializeNode(GraphSerializationContext ctx)
        {
        }

        public override void DeserializeNode (GraphSerializationContext ctx) {
        }
    }
}
#endif
