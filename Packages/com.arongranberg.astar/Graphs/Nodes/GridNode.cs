#define PREALLOCATE_NODES
using System.Collections.Generic;
using Pathfinding.Serialization;
using Pathfinding.Util;
using UnityEngine;

namespace Pathfinding {
	/// <summary>Node used for the GridGraph</summary>
	public class GridNode : GridNodeBase {
		public GridNode() {
        }

        public GridNode (AstarPath astar) {
        }

#if !ASTAR_NO_GRID_GRAPH
        private static GridGraph[] _gridGraphs = new GridGraph[0];
		public static GridGraph GetGridGraph (uint graphIndex) {
            return default;
        }

        public static void SetGridGraph (int graphIndex, GridGraph graph) {
        }

        public static void ClearGridGraph (int graphIndex, GridGraph graph) {
        }

        /// <summary>Internal use only</summary>
        internal ushort InternalGridFlags {
			get { return gridFlags; }
			set { gridFlags = value; }
		}

		const int GridFlagsConnectionOffset = 0;
		const int GridFlagsConnectionBit0 = 1 << GridFlagsConnectionOffset;
		const int GridFlagsConnectionMask = 0xFF << GridFlagsConnectionOffset;
		const int GridFlagsAxisAlignedConnectionMask = 0xF << GridFlagsConnectionOffset;

		const int GridFlagsEdgeNodeOffset = 10;
		const int GridFlagsEdgeNodeMask = 1 << GridFlagsEdgeNodeOffset;

		public override bool HasConnectionsToAllEightNeighbours {
			get {
				return (InternalGridFlags & GridFlagsConnectionMask) == GridFlagsConnectionMask;
			}
		}

		public override bool HasConnectionsToAllAxisAlignedNeighbours {
			get {
				return (InternalGridFlags & GridFlagsAxisAlignedConnectionMask) == GridFlagsAxisAlignedConnectionMask;
			}
		}

		/// <summary>
		/// True if the node has a connection in the specified direction.
		/// The dir parameter corresponds to directions in the grid as:
		/// <code>
		///         Z
		///         |
		///         |
		///
		///      6  2  5
		///       \ | /
		/// --  3 - X - 1  ----- X
		///       / | \
		///      7  0  4
		///
		///         |
		///         |
		/// </code>
		///
		/// See: <see cref="SetConnectionInternal"/>
		/// See: <see cref="GridGraph.neighbourXOffsets"/>
		/// See: <see cref="GridGraph.neighbourZOffsets"/>
		/// See: <see cref="GridGraph.neighbourOffsets"/>
		/// See: <see cref="GridGraph.GetNeighbourDirections"/>
		/// </summary>
		public override bool HasConnectionInDirection (int dir) {
            return default;
        }

        /// <summary>
        /// Enables or disables a connection in a specified direction on the graph.
        ///
        /// Note: This only changes the connection from this node to the other node. You may also want to call the same method on the other node with the opposite direction.
        ///
        /// See: <see cref="HasConnectionInDirection"/>
        /// See: <see cref="OppositeConnectionDirection"/>
        /// </summary>
        public void SetConnection(int dir, bool value)
        {
        }

        /// <summary>
        /// Enables or disables a connection in a specified direction on the graph.
        /// See: <see cref="HasConnectionInDirection"/>
        ///
        /// Warning: Using this method can make the graph data inconsistent. It's recommended to use other ways to update the graph, instead, for example <see cref="SetConnection"/>.
        /// </summary>
        public void SetConnectionInternal(int dir, bool value)
        {
        }

        /// <summary>
        /// Sets the state of all grid connections.
        ///
        /// See: SetConnectionInternal
        ///
        /// Warning: Using this method can make the graph data inconsistent. It's recommended to use other ways to update the graph, instead.
        /// </summary>
        /// <param name="connections">a bitmask of the connections (bit 0 is the first connection, bit 1 the second connection, etc.).</param>
        public void SetAllConnectionInternal(int connections)
        {
        }

        /// <summary>Bitpacked int containing all 8 grid connections</summary>
        public int GetAllConnectionInternal()
        {
            return default;
        }

        public override bool HasAnyGridConnections => GetAllConnectionInternal() != 0;

		/// <summary>
		/// Disables all grid connections from this node.
		/// Note: Other nodes might still be able to get to this node.
		/// Therefore it is recommended to also disable the relevant connections on adjacent nodes.
		///
		/// Warning: Using this method can make the graph data inconsistent. It's recommended to use other ways to update the graph, instead.
		/// </summary>
		public override void ResetConnectionsInternal () {
        }

        /// <summary>
        /// Work in progress for a feature that required info about which nodes were at the border of the graph.
        /// Note: This property is not functional at the moment.
        /// </summary>
        public bool EdgeNode {
			get {
				return (gridFlags & GridFlagsEdgeNodeMask) != 0;
			}
			set {
				unchecked { gridFlags = (ushort)(gridFlags & ~GridFlagsEdgeNodeMask | (value ? GridFlagsEdgeNodeMask : 0)); }
			}
		}

		public override GridNodeBase GetNeighbourAlongDirection (int direction) {
            return default;
        }

        public override void ClearConnections (bool alsoReverse) {
        }

        public override void GetConnections<T>(GetConnectionsWithData<T> action, ref T data, int connectionFilter) {
        }

        public override bool GetPortal (GraphNode other, out Vector3 left, out Vector3 right) {
            left = default(Vector3);
            right = default(Vector3);
            return default;
        }

        /// <summary>
        /// Filters diagonal connections based on the non-diagonal ones to prevent corner cutting and similar things.
        ///
        /// This involves some complicated bitshifting to calculate which diagonal connections
        /// should be active based on the non-diagonal ones.
        /// For example a path should not be able to pass from A to B if the \<see cref="s"/> represent nodes
        /// that we cannot traverse.
        ///
        /// <code>
        ///    # B
        ///    A #
        /// </code>
        ///
        /// Additionally if corner cutting is disabled we will also prevent a connection from A to B in this case:
        ///
        /// <code>
        ///      B
        ///    A #
        /// </code>
        ///
        /// If neighbours = 4 then only the 4 axis aligned connections will be enabled.
        ///
        /// If neighbours = 6 then only the connections which are valid for hexagonal graphs will be enabled.
        /// </summary>
        public static int FilterDiagonalConnections(int conns, NumNeighbours neighbours, bool cutCorners)
        {
            return default;
        }

        public override void Open(Path path, uint pathNodeIndex, uint gScore)
        {
        }

        public override void SerializeNode(GraphSerializationContext ctx)
        {
        }

        public override void DeserializeNode(GraphSerializationContext ctx)
        {
        }

        public override void AddPartialConnection(GraphNode node, uint cost, bool isOutgoing, bool isIncoming)
        {
        }

        public override void RemovePartialConnection(GraphNode node)
        {
        }

        /// <summary>
        /// Removes a connection from the internal grid connections.
        /// See: SetConnectionInternal
        /// </summary>
        protected void RemoveGridConnection(GridNode node)
        {
        }
#else
		public override void AddPartialConnection (GraphNode node, uint cost, bool isOutgoing, bool isIncoming) {
			throw new System.NotImplementedException();
		}

		public override void ClearConnections (bool alsoReverse) {
			throw new System.NotImplementedException();
		}

		public override void GetConnections (GraphNodeDelegate del) {
			throw new System.NotImplementedException();
		}

		public override void Open (Path path, PathNode pathNode, PathHandler handler) {
			throw new System.NotImplementedException();
		}

		public override void AddPartialConnection (GraphNode node) {
			throw new System.NotImplementedException();
		}
#endif
    }
}
