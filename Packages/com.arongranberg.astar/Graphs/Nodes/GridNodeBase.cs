#define PREALLOCATE_NODES
using UnityEngine;
using Pathfinding.Serialization;

namespace Pathfinding {
	/// <summary>Base class for GridNode and LevelGridNode</summary>
	public abstract class GridNodeBase : GraphNode {
		const int GridFlagsWalkableErosionOffset = 8;
		const int GridFlagsWalkableErosionMask = 1 << GridFlagsWalkableErosionOffset;

		const int GridFlagsWalkableTmpOffset = 9;
		const int GridFlagsWalkableTmpMask = 1 << GridFlagsWalkableTmpOffset;

		public const int NodeInGridIndexLayerOffset = 24;
		protected const int NodeInGridIndexMask = 0xFFFFFF;

		/// <summary>
		/// Bitfield containing the x and z coordinates of the node as well as the layer (for layered grid graphs).
		/// See: NodeInGridIndex
		/// </summary>
		protected int nodeInGridIndex;
		protected ushort gridFlags;

#if !ASTAR_GRID_NO_CUSTOM_CONNECTIONS
		/// <summary>
		/// Custon non-grid connections from this node.
		/// See: <see cref="Connect"/>
		/// See: <see cref="Disconnect"/>
		///
		/// This field is removed if the ASTAR_GRID_NO_CUSTOM_CONNECTIONS compiler directive is used.
		/// Removing it can save a tiny bit of memory. You can enable the define in the Optimizations tab in the A* inspector.
		/// See: compiler-directives (view in online documentation for working links)
		///
		/// Note: If you modify this array or the contents of it you must call <see cref="SetConnectivityDirty"/>.
		/// </summary>
		public Connection[] connections;
#endif

		/// <summary>
		/// The index of the node in the grid.
		/// This is x + z*graph.width
		/// So you can get the X and Z indices using
		/// <code>
		/// int index = node.NodeInGridIndex;
		/// int x = index % graph.width;
		/// int z = index / graph.width;
		/// // where graph is GridNode.GetGridGraph (node.graphIndex), i.e the graph the nodes are contained in.
		/// </code>
		///
		/// See: <see cref="CoordinatesInGrid"/>
		/// </summary>
		public int NodeInGridIndex { get { return nodeInGridIndex & NodeInGridIndexMask; } set { nodeInGridIndex = (nodeInGridIndex & ~NodeInGridIndexMask) | value; } }

		/// <summary>
		/// X coordinate of the node in the grid.
		/// The node in the bottom left corner has (x,z) = (0,0) and the one in the opposite
		/// corner has (x,z) = (width-1, depth-1)
		///
		/// See: <see cref="ZCoordinateInGrid"/>
		/// See: <see cref="NodeInGridIndex"/>
		/// </summary>
		public int XCoordinateInGrid => NodeInGridIndex % GridNode.GetGridGraph(GraphIndex).width;

		/// <summary>
		/// Z coordinate of the node in the grid.
		/// The node in the bottom left corner has (x,z) = (0,0) and the one in the opposite
		/// corner has (x,z) = (width-1, depth-1)
		///
		/// See: <see cref="XCoordinateInGrid"/>
		/// See: <see cref="NodeInGridIndex"/>
		/// </summary>
		public int ZCoordinateInGrid => NodeInGridIndex / GridNode.GetGridGraph(GraphIndex).width;

		/// <summary>
		/// The X and Z coordinates of the node in the grid.
		///
		/// The node in the bottom left corner has (x,z) = (0,0) and the one in the opposite
		/// corner has (x,z) = (width-1, depth-1)
		///
		/// See: <see cref="XCoordinateInGrid"/>
		/// See: <see cref="ZCoordinateInGrid"/>
		/// See: <see cref="NodeInGridIndex"/>
		/// </summary>
		public Vector2Int CoordinatesInGrid {
			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			get {
				var width = GridNode.GetGridGraph(GraphIndex).width;
				var index = NodeInGridIndex;
				var z = index / width;
				var x = index - z * width;
				return new Vector2Int(x, z);
			}
		}

		/// <summary>
		/// Stores walkability before erosion is applied.
		/// Used internally when updating the graph.
		/// </summary>
		public bool WalkableErosion {
			get {
				return (gridFlags & GridFlagsWalkableErosionMask) != 0;
			}
			set {
				unchecked { gridFlags = (ushort)(gridFlags & ~GridFlagsWalkableErosionMask | (value ? (ushort)GridFlagsWalkableErosionMask : (ushort)0)); }
			}
		}

		/// <summary>Temporary variable used internally when updating the graph.</summary>
		public bool TmpWalkable {
			get {
				return (gridFlags & GridFlagsWalkableTmpMask) != 0;
			}
			set {
				unchecked { gridFlags = (ushort)(gridFlags & ~GridFlagsWalkableTmpMask | (value ? (ushort)GridFlagsWalkableTmpMask : (ushort)0)); }
			}
		}

		/// <summary>
		/// True if the node has grid connections to all its 8 neighbours.
		/// Note: This will always return false if GridGraph.neighbours is set to anything other than Eight.
		/// See: GetNeighbourAlongDirection
		/// See: <see cref="HasConnectionsToAllAxisAlignedNeighbours"/>
		/// </summary>
		public abstract bool HasConnectionsToAllEightNeighbours { get; }

		/// <summary>
		/// True if the node has grid connections to all its 4 axis-aligned neighbours.
		/// See: GetNeighbourAlongDirection
		/// See: <see cref="HasConnectionsToAllEightNeighbours"/>
		/// </summary>
		public abstract bool HasConnectionsToAllAxisAlignedNeighbours { get; }

		/// <summary>
		/// The connection opposite the given one.
		///
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
		/// For example, dir=1 outputs 3, dir=6 outputs 4 and so on.
		///
		/// See: <see cref="HasConnectionInDirection"/>
		/// </summary>
		public static int OppositeConnectionDirection (int dir) {
            return default;
        }

        /// <summary>
        /// Converts from dx + 3*dz to a neighbour direction.
        ///
        /// Used by <see cref="OffsetToConnectionDirection"/>.
        ///
        /// Assumes that dx and dz are both in the range [0,2].
        /// See: <see cref="GridGraph.neighbourOffsets"/>
        /// </summary>
        internal static readonly int[] offsetToDirection = { 7, 0, 4, 3, -1, 1, 6, 2, 5 };

		/// <summary>
		/// Converts from a delta (dx, dz) to a neighbour direction.
		///
		/// For example, if dx=1 and dz=0, the return value will be 1, which is the direction to the right of a grid coordinate.
		///
		/// If dx=0 and dz=0, the return value will be -1.
		///
		/// See: <see cref="GridGraph.neighbourOffsets"/>
		///
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
		/// See: <see cref="HasConnectionInDirection"/>
		/// </summary>
		/// <param name="dx">X coordinate delta. Should be in the range [-1, 1]. Values outside this range will cause -1 to be returned.</param>
		/// <param name="dz">Z coordinate delta. Should be in the range [-1, 1]. Values outside this range will cause -1 to be returned.</param>
		public static int OffsetToConnectionDirection (int dx, int dz) {
            return default;
        }

        /// <summary>
        /// Projects the given point onto the plane of this node's surface.
        ///
        /// The point will be projected down to a plane that contains the surface of the node.
        /// If the point is not contained inside the node, it is projected down onto this plane anyway.
        /// </summary>
        public Vector3 ProjectOnSurface (Vector3 point) {
            return default;
        }

        public override Vector3 ClosestPointOnNode (Vector3 p) {
            return default;
        }

        /// <summary>
        /// Checks if point is inside the node when seen from above.
        ///
        /// The borders of a node are considered to be inside the node.
        ///
        /// Note that <see cref="ContainsPointInGraphSpace"/> is faster than this method as it avoids
        /// some coordinate transformations. If you are repeatedly calling this method
        /// on many different nodes but with the same point then you should consider
        /// transforming the point first and then calling ContainsPointInGraphSpace.
        /// <code>
        /// Int3 p = (Int3)graph.transform.InverseTransform(point);
        ///
        /// node.ContainsPointInGraphSpace(p);
        /// </code>
        /// </summary>
        public override bool ContainsPoint (Vector3 point) {
            return default;
        }

        /// <summary>
        /// Checks if point is inside the node in graph space.
        ///
        /// The borders of a node are considered to be inside the node.
        ///
        /// The y coordinate of the point is ignored.
        /// </summary>
        public override bool ContainsPointInGraphSpace (Int3 point) {
            return default;
        }

        public override float SurfaceArea()
        {
            return default;
        }

        public override Vector3 RandomPointOnSurface()
        {
            return default;
        }

        /// <summary>
        /// Transforms a world space point to a normalized point on this node's surface.
        /// (0.5,0.5) represents the node's center. (0,0), (1,0), (1,1) and (0,1) each represent the corners of the node.
        ///
        /// See: <see cref="UnNormalizePoint"/>
        /// </summary>
        public Vector2 NormalizePoint(Vector3 worldPoint)
        {
            return default;
        }

        /// <summary>
        /// Transforms a normalized point on this node's surface to a world space point.
        /// (0.5,0.5) represents the node's center. (0,0), (1,0), (1,1) and (0,1) each represent the corners of the node.
        ///
        /// See: <see cref="NormalizePoint"/>
        /// </summary>
        public Vector3 UnNormalizePoint(Vector2 normalizedPointOnSurface)
        {
            return default;
        }

        public override int GetGizmoHashCode()
        {
            return default;
        }

        /// <summary>
        /// Adjacent grid node in the specified direction.
        /// This will return null if the node does not have a connection to a node
        /// in that direction.
        ///
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
        /// See: <see cref="GetConnections"/>
        /// See: <see cref="GetNeighbourAlongDirection"/>
        ///
        /// Note: This method only takes grid connections into account, not custom connections (i.e. those added using <see cref="Connect"/> or using node links).
        /// </summary>
        public abstract GridNodeBase GetNeighbourAlongDirection(int direction);

		/// <summary>
		/// True if the node has a connection to an adjecent node in the specified direction.
		///
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
		/// See: <see cref="GetConnections"/>
		/// See: <see cref="GetNeighbourAlongDirection"/>
		/// See: <see cref="OffsetToConnectionDirection"/>
		///
		/// Note: This method only takes grid connections into account, not custom connections (i.e. those added using <see cref="Connect"/> or using node links).
		/// </summary>
		public virtual bool HasConnectionInDirection (int direction) {
            return default;
        }

        /// <summary>True if this node has any grid connections</summary>
        public abstract bool HasAnyGridConnections { get; }

        public override bool ContainsOutgoingConnection(GraphNode node)
        {
            return default;
        }

        /// <summary>
        /// Disables all grid connections from this node.
        /// Note: Other nodes might still be able to get to this node.
        /// Therefore it is recommended to also disable the relevant connections on adjacent nodes.
        /// </summary>
        public abstract void ResetConnectionsInternal();

		public override void OpenAtPoint (Path path, uint pathNodeIndex, Int3 pos, uint gScore) {
        }

        public override void Open (Path path, uint pathNodeIndex, uint gScore) {
        }

#if ASTAR_GRID_NO_CUSTOM_CONNECTIONS
		public override void AddPartialConnection (GraphNode node, uint cost, bool isOutgoing, bool isIncoming) {
			throw new System.NotImplementedException("GridNodes do not have support for adding manual connections with your current settings."+
				"\nPlease disable ASTAR_GRID_NO_CUSTOM_CONNECTIONS in the Optimizations tab in the A* Inspector");
		}

		public override void RemovePartialConnection (GraphNode node) {
			// Nothing to do because ASTAR_GRID_NO_CUSTOM_CONNECTIONS is enabled
		}

		public void ClearCustomConnections (bool alsoReverse) {
		}
#else
        /// <summary>Same as <see cref="ClearConnections"/>, but does not clear grid connections, only custom ones (e.g added by <see cref="AddConnection"/> or a NodeLink component)</summary>
        public void ClearCustomConnections (bool alsoReverse) {
        }

        public override void ClearConnections (bool alsoReverse) {
        }

        public override void GetConnections<T>(GetConnectionsWithData<T> action, ref T data, int connectionFilter) {
        }

        public override void AddPartialConnection (GraphNode node, uint cost, bool isOutgoing, bool isIncoming) {
        }

        /// <summary>
        /// Removes any connection from this node to the specified node.
        /// If no such connection exists, nothing will be done.
        ///
        /// Note: This only removes the connection from this node to the other node.
        /// You may want to call the same function on the other node to remove its eventual connection
        /// to this node.
        ///
        /// Version: Before 4.3.48 This method only handled custom connections (those added using link components or the AddConnection method).
        /// Regular grid connections had to be added or removed using <see cref="Pathfinding.GridNode.SetConnectionInternal"/>. Starting with 4.3.48 this method
        /// can remove all types of connections.
        /// </summary>
        public override void RemovePartialConnection(GraphNode node)
        {
        }

        public override void SerializeReferences(GraphSerializationContext ctx)
        {
        }

        public override void DeserializeReferences (GraphSerializationContext ctx) {
        }
#endif
    }
}
