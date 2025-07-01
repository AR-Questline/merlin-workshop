using UnityEngine;
using Pathfinding.Serialization;

namespace Pathfinding {
	/// <summary>
	/// Node used for the PointGraph.
	/// This is just a simple point with a list of connections (and associated costs) to other nodes.
	/// It does not have any concept of a surface like many other node types.
	///
	/// See: PointGraph
	/// </summary>
	public class PointNode : GraphNode {
		/// <summary>
		/// All connections from this node.
		/// See: <see cref="Connect"/>
		/// See: <see cref="Disconnect"/>
		/// See: <see cref="GetConnections"/>
		///
		/// Note: If you modify this array or the contents of it you must call <see cref="SetConnectivityDirty"/>.
		///
		/// Note: If you modify this array or the contents of it you must call <see cref="PointGraph.RegisterConnectionLength"/> with the length of the new connections.
		///
		/// This may be null if the node has no connections to other nodes.
		/// </summary>
		public Connection[] connections;

		/// <summary>
		/// GameObject this node was created from (if any).
		/// Warning: When loading a graph from a saved file or from cache, this field will be null.
		///
		/// <code>
		/// var node = AstarPath.active.GetNearest(transform.position).node;
		/// var pointNode = node as PointNode;
		///
		/// if (pointNode != null) {
		///     Debug.Log("That node was created from the GameObject named " + pointNode.gameObject.name);
		/// } else {
		///     Debug.Log("That node is not a PointNode");
		/// }
		/// </code>
		/// </summary>
		public GameObject gameObject;

		[System.Obsolete("Set node.position instead")]
		public void SetPosition (Int3 value) {
        }

        public PointNode() {
        }

        public PointNode (AstarPath astar) {
        }

        /// <summary>
        /// Closest point on the surface of this node to the point p.
        ///
        /// For a point node this is always the node's <see cref="position"/> sicne it has no surface.
        /// </summary>
        public override Vector3 ClosestPointOnNode (Vector3 p) {
            return default;
        }

        /// <summary>
        /// Checks if point is inside the node when seen from above.
        ///
        /// Since point nodes have no surface area, this method always returns false.
        /// </summary>
        public override bool ContainsPoint (Vector3 point) {
            return default;
        }

        /// <summary>
        /// Checks if point is inside the node in graph space.
        ///
        /// Since point nodes have no surface area, this method always returns false.
        /// </summary>
        public override bool ContainsPointInGraphSpace (Int3 point) {
            return default;
        }

        public override void GetConnections<T>(GetConnectionsWithData<T> action, ref T data, int connectionFilter) {
        }

        public override void ClearConnections (bool alsoReverse) {
        }

        public override bool ContainsOutgoingConnection (GraphNode node) {
            return default;
        }

        public override void AddPartialConnection (GraphNode node, uint cost, bool isOutgoing, bool isIncoming) {
        }

        public override void RemovePartialConnection(GraphNode node)
        {
        }

        public override void Open(Path path, uint pathNodeIndex, uint gScore)
        {
        }

        public override void OpenAtPoint(Path path, uint pathNodeIndex, Int3 pos, uint gScore)
        {
        }

        public override int GetGizmoHashCode()
        {
            return default;
        }

        public override void SerializeNode(GraphSerializationContext ctx)
        {
        }

        public override void DeserializeNode(GraphSerializationContext ctx)
        {
        }

        public override void SerializeReferences(GraphSerializationContext ctx)
        {
        }

        public override void DeserializeReferences(GraphSerializationContext ctx)
        {
        }
    }
}
