using UnityEngine;
using System.Collections.Generic;
using Pathfinding.Serialization;
using Pathfinding.Pooling;

namespace Pathfinding {
	using Pathfinding.Drawing;
	using Unity.Jobs;

	/// <summary>
	/// Graph consisting of a set of points.
	///
	/// [Open online documentation to see images]
	///
	/// The point graph is the most basic graph structure, it consists of a number of interconnected points in space called nodes or waypoints.
	/// The point graph takes a Transform object as "root", this Transform will be searched for child objects, every child object will be treated as a node.
	/// If <see cref="recursive"/> is enabled, it will also search the child objects of the children recursively.
	/// It will then check if any connections between the nodes can be made, first it will check if the distance between the nodes isn't too large (<see cref="maxDistance)"/>
	/// and then it will check if the axis aligned distance isn't too high. The axis aligned distance, named <see cref="limits"/>,
	/// is useful because usually an AI cannot climb very high, but linking nodes far away from each other,
	/// but on the same Y level should still be possible. <see cref="limits"/> and <see cref="maxDistance"/> are treated as being set to infinity if they are set to 0 (zero).
	/// Lastly it will check if there are any obstructions between the nodes using
	/// <a href="http://unity3d.com/support/documentation/ScriptReference/Physics.Raycast.html">raycasting</a> which can optionally be thick.
	/// One thing to think about when using raycasting is to either place the nodes a small
	/// distance above the ground in your scene or to make sure that the ground is not in the raycast mask to avoid the raycast from hitting the ground.
	///
	/// Alternatively, a tag can be used to search for nodes.
	/// See: http://docs.unity3d.com/Manual/Tags.html
	///
	/// For larger graphs, it can take quite some time to scan the graph with the default settings.
	/// You can enable <see cref="optimizeForSparseGraph"/> which will in most cases reduce the calculation times drastically.
	///
	/// Note: Does not support linecast because the nodes do not have a surface.
	///
	/// See: get-started-point (view in online documentation for working links)
	/// See: graphTypes (view in online documentation for working links)
	///
	/// \section pointgraph-inspector Inspector
	/// [Open online documentation to see images]
	///
	/// \inspectorField{Root, root}
	/// \inspectorField{Recursive, recursive}
	/// \inspectorField{Tag, searchTag}
	/// \inspectorField{Max Distance, maxDistance}
	/// \inspectorField{Max Distance (axis aligned), limits}
	/// \inspectorField{Raycast, raycast}
	/// \inspectorField{Raycast → Use 2D Physics, use2DPhysics}
	/// \inspectorField{Raycast → Thick Raycast, thickRaycast}
	/// \inspectorField{Raycast → Thick Raycast → Radius, thickRaycastRadius}
	/// \inspectorField{Raycast → Mask, mask}
	/// \inspectorField{Optimize For Sparse Graph, optimizeForSparseGraph}
	/// \inspectorField{Nearest Node Queries Find Closest, nearestNodeDistanceMode}
	/// \inspectorField{Initial Penalty, initialPenalty}
	/// </summary>
	[JsonOptIn]
	[Pathfinding.Util.Preserve]
	public class PointGraph : NavGraph
		, IUpdatableGraph {
		/// <summary>
		/// Children of this transform are treated as nodes.
		///
		/// If null, the <see cref="searchTag"/> will be used instead.
		/// </summary>
		[JsonMember]
		public Transform root;

		/// <summary>If no <see cref="root"/> is set, all nodes with the tag is used as nodes</summary>
		[JsonMember]
		public string searchTag;

		/// <summary>
		/// Max distance for a connection to be valid.
		/// The value 0 (zero) will be read as infinity and thus all nodes not restricted by
		/// other constraints will be added as connections.
		///
		/// A negative value will disable any neighbours to be added.
		/// It will completely stop the connection processing to be done, so it can save you processing
		/// power if you don't these connections.
		/// </summary>
		[JsonMember]
		public float maxDistance;

		/// <summary>Max distance along the axis for a connection to be valid. 0 = infinity</summary>
		[JsonMember]
		public Vector3 limits;

		/// <summary>
		/// Use raycasts to filter connections.
		///
		/// If a hit is detected between two nodes, the connection will not be created.
		/// </summary>
		[JsonMember]
		public bool raycast = true;

		/// <summary>Use the 2D Physics API</summary>
		[JsonMember]
		public bool use2DPhysics;

		/// <summary>
		/// Use thick raycast.
		///
		/// If enabled, the collision check shape will not be a line segment, but a capsule with a radius of <see cref="thickRaycastRadius"/>.
		/// </summary>
		[JsonMember]
		public bool thickRaycast;

		/// <summary>
		/// Thick raycast radius.
		///
		/// See: <see cref="thickRaycast"/>
		/// </summary>
		[JsonMember]
		public float thickRaycastRadius = 1;

		/// <summary>
		/// Recursively search for child nodes to the <see cref="root"/>.
		///
		/// If false, all direct children of <see cref="root"/> will be used as nodes.
		/// If true, all children of <see cref="root"/> and their children (recursively) will be used as nodes.
		/// </summary>
		[JsonMember]
		public bool recursive = true;

		/// <summary>
		/// Layer mask to use for raycasting.
		///
		/// All objects included in this layer mask will be treated as obstacles.
		///
		/// See: <see cref="raycast"/>
		/// </summary>
		[JsonMember]
		public LayerMask mask;

		/// <summary>
		/// Optimizes the graph for sparse graphs.
		///
		/// This can reduce calculation times for both scanning and for normal path requests by huge amounts.
		/// It reduces the number of node-node checks that need to be done during scan, and can also optimize getting the nearest node from the graph (such as when querying for a path).
		///
		/// Try enabling and disabling this option, check the scan times logged when you scan the graph to see if your graph is suited for this optimization
		/// or if it makes it slower.
		///
		/// The gain of using this optimization increases with larger graphs, the default scan algorithm is brute force and requires O(n^2) checks, this optimization
		/// along with a graph suited for it, requires only O(n) checks during scan (assuming the connection distance limits are reasonable).
		///
		/// Warning:
		/// When you have this enabled, you will not be able to move nodes around using scripting unless you recalculate the lookup structure at the same time.
		/// See: <see cref="RebuildNodeLookup"/>
		///
		/// If you enable this during runtime, you need to call <see cref="RebuildNodeLookup"/> to make this take effect.
		/// If you are going to scan the graph afterwards then you do not need to do this.
		/// </summary>
		[JsonMember]
		public bool optimizeForSparseGraph;

		PointKDTree lookupTree = new PointKDTree();

		/// <summary>
		/// Longest known connection.
		/// In squared Int3 units.
		///
		/// See: <see cref="RegisterConnectionLength"/>
		/// </summary>
		long maximumConnectionLength = 0;

		/// <summary>
		/// All nodes in this graph.
		/// Note that only the first <see cref="nodeCount"/> will be non-null.
		///
		/// You can also use the GetNodes method to get all nodes.
		///
		/// The order of the nodes is unspecified, and may change when nodes are added or removed.
		/// </summary>
		public PointNode[] nodes;

		/// <summary>
		/// \copydoc Pathfinding::PointGraph::NodeDistanceMode
		///
		/// See: <see cref="NodeDistanceMode"/>
		///
		/// If you enable this during runtime, you will need to call <see cref="RebuildConnectionDistanceLookup"/> to make sure some cache data is properly recalculated.
		/// If the graph doesn't have any nodes yet or if you are going to scan the graph afterwards then you do not need to do this.
		/// </summary>
		[JsonMember]
		public NodeDistanceMode nearestNodeDistanceMode;

		/// <summary>Number of nodes in this graph</summary>
		public int nodeCount { get; protected set; }

		public override bool isScanned => nodes != null;

		/// <summary>
		/// Distance query mode.
		/// [Open online documentation to see images]
		///
		/// In the image above there are a few red nodes. Assume the agent is the orange circle. Using the Node mode the closest point on the graph that would be found would be the node at the bottom center which
		/// may not be what you want. Using the Connection mode it will find the closest point on the connection between the two nodes in the top half of the image.
		///
		/// When using the Connection option you may also want to use the Connection option for the Seeker's Start End Modifier snapping options.
		/// This is not strictly necessary, but it most cases it is what you want.
		///
		/// See: <see cref="Pathfinding.StartEndModifier.exactEndPoint"/>
		/// </summary>
		public enum NodeDistanceMode {
			/// <summary>
			/// All nearest node queries find the closest node center.
			/// This is the fastest option but it may not be what you want if you have long connections.
			/// </summary>
			Node,
			/// <summary>
			/// All nearest node queries find the closest point on edges between nodes.
			/// This is useful if you have long connections where the agent might be closer to some unrelated node if it is standing on a long connection between two nodes.
			/// This mode is however slower than the Node mode.
			/// </summary>
			Connection,
		}

		public override int CountNodes () {
            return default;
        }

        public override void GetNodes(System.Action<GraphNode> action)
        {
        }

        public override NNInfo GetNearest (Vector3 position, NNConstraint constraint, float maxDistanceSqr) {
            return default;
        }

        NNInfo FindClosestConnectionPoint(PointNode node, Vector3 position, float maxDistanceSqr)
        {
            return default;
        }

        public override NNInfo RandomPointOnSurface(NNConstraint nnConstraint = null, bool highQuality = true)
        {
            return default;
        }

        /// <summary>
        /// Add a node to the graph at the specified position.
        /// Note: Vector3 can be casted to Int3 using (Int3)myVector.
        ///
        /// Note: This needs to be called when it is safe to update nodes, which is
        /// - when scanning
        /// - during a graph update
        /// - inside a callback registered using AstarPath.AddWorkItem
        ///
        /// <code>
        /// AstarPath.active.AddWorkItem(() => {
        ///     var graph = AstarPath.active.data.pointGraph;
        ///     // Add 2 nodes and connect them
        ///     var node1 = graph.AddNode((Int3)transform.position);
        ///     var node2 = graph.AddNode((Int3)(transform.position + Vector3.right));
        ///     var cost = (uint)(node2.position - node1.position).costMagnitude;
        ///     GraphNode.Connect(node1, node2, cost);
        /// });
        /// </code>
        ///
        /// See: runtime-graphs (view in online documentation for working links)
        /// See: creating-point-nodes (view in online documentation for working links)
        /// </summary>
        public PointNode AddNode(Int3 position)
        {
            return default;
        }

        /// <summary>
        /// Add a node with the specified type to the graph at the specified position.
        ///
        /// Note: Vector3 can be casted to Int3 using (Int3)myVector.
        ///
        /// Note: This needs to be called when it is safe to update nodes, which is
        /// - when scanning
        /// - during a graph update
        /// - inside a callback registered using AstarPath.AddWorkItem
        ///
        /// See: <see cref="AstarPath.AddWorkItem"/>
        /// See: runtime-graphs (view in online documentation for working links)
        /// See: creating-point-nodes (view in online documentation for working links)
        /// </summary>
        /// <param name="node">This must be a node created using T(AstarPath.active) right before the call to this method.
        /// The node parameter is only there because there is no new(AstarPath) constraint on
        /// generic type parameters.</param>
        /// <param name="position">The node will be set to this position.</param>
        public T AddNode<T>(T node, Int3 position) where T : PointNode
        {
            return default;
        }

        /// <summary>
        /// Removes a node from the graph.
        ///
        /// <code>
        /// // Make sure we only modify the graph when all pathfinding threads are paused
        /// AstarPath.active.AddWorkItem(() => {
        ///     // Find the node closest to some point
        ///     var nearest = AstarPath.active.GetNearest(new Vector3(1, 2, 3));
        ///
        ///     // Check if it is a PointNode
        ///     if (nearest.node is PointNode pnode) {
        ///         // Remove the node. Assuming it belongs to the first point graph in the scene
        ///         AstarPath.active.data.pointGraph.RemoveNode(pnode);
        ///     }
        /// });
        /// </code>
        ///
        /// Note: For larger graphs, this operation can be slow, as it is linear in the number of nodes in the graph.
        ///
        /// See: <see cref="AddNode"/>
        /// See: creating-point-nodes (view in online documentation for working links)
        /// </summary>
        public void RemoveNode(PointNode node)
        {
        }

        /// <summary>Recursively counds children of a transform</summary>
        protected static int CountChildren(Transform tr)
        {
            return default;
        }

        /// <summary>Recursively adds childrens of a transform as nodes</summary>
        protected static void AddChildren (PointNode[] nodes, ref int c, Transform tr) {
        }

        /// <summary>
        /// Rebuilds the lookup structure for nodes.
        ///
        /// This is used when <see cref="optimizeForSparseGraph"/> is enabled.
        ///
        /// You should call this method every time you move a node in the graph manually and
        /// you are using <see cref="optimizeForSparseGraph"/>, otherwise pathfinding might not work correctly.
        ///
        /// You may also call this after you have added many nodes using the
        /// <see cref="AddNode"/> method. When adding nodes using the <see cref="AddNode"/> method they
        /// will be added to the lookup structure. The lookup structure will
        /// rebalance itself when it gets too unbalanced. But if you are
        /// sure you won't be adding any more nodes in the short term, you can
        /// make sure it is perfectly balanced and thus squeeze out the last
        /// bit of performance by calling this method. This can improve the
        /// performance of the <see cref="GetNearest"/> method slightly. The improvements
        /// are on the order of 10-20%.
        /// </summary>
        public void RebuildNodeLookup ()
        {
        }

        static PointKDTree BuildNodeLookup(PointNode[] nodes, int nodeCount, bool optimizeForSparseGraph)
        {
            return default;
        }

        /// <summary>Rebuilds a cache used when <see cref="nearestNodeDistanceMode"/> = <see cref="NodeDistanceMode"/>.Connection</summary>
        public void RebuildConnectionDistanceLookup()
        {
        }

        static long LongestConnectionLength(PointNode[] nodes, int nodeCount)
        {
            return default;
        }

        /// <summary>
        /// Ensures the graph knows that there is a connection with this length.
        /// This is used when the nearest node distance mode is set to ToConnection.
        /// If you are modifying node connections yourself (i.e. manipulating the PointNode.connections array) then you must call this function
        /// when you add any connections.
        ///
        /// When using GraphNode.Connect this is done automatically.
        /// It is also done for all nodes when <see cref="RebuildNodeLookup"/> is called.
        /// </summary>
        /// <param name="sqrLength">The length of the connection in squared Int3 units. This can be calculated using (node1.position - node2.position).sqrMagnitudeLong.</param>
        public void RegisterConnectionLength(long sqrLength)
        {
        }

        protected virtual PointNode[] CreateNodes(int count)
        {
            return default;
        }

        class PointGraphScanPromise : IGraphUpdatePromise
        {
            public PointGraph graph;
			PointKDTree lookupTree;
			PointNode[] nodes;

			public IEnumerator<JobHandle> Prepare () {
                return default;
            }

            public void Apply(IGraphUpdateContext ctx)
            {
            }
        }

        protected override void DestroyAllNodes()
        {
        }

        protected override IGraphUpdatePromise ScanInternal() => new PointGraphScanPromise { graph = this };

        /// <summary>
        /// Recalculates connections for all nodes in the graph.
        /// This is useful if you have created nodes manually using <see cref="AddNode"/> and then want to connect them in the same way as the point graph normally connects nodes.
        /// </summary>
        public void ConnectNodes()
        {
        }

        /// <summary>
        /// Calculates connections for all nodes in the graph.
        /// This is an IEnumerable, you can iterate through it using e.g foreach to get progress information.
        /// </summary>
        static IEnumerable<float> ConnectNodesAsync(PointNode[] nodes, int nodeCount, PointKDTree lookupTree, float maxDistance, Vector3 limits, PointGraph graph)
        {
            return default;
        }

        /// <summary>
        /// Returns if the connection between a and b is valid.
        /// Checks for obstructions using raycasts (if enabled) and checks for height differences.
        /// As a bonus, it outputs the distance between the nodes too if the connection is valid.
        ///
        /// Note: This is not the same as checking if node a is connected to node b.
        /// That should be done using a.ContainsOutgoingConnection(b)
        /// </summary>
        public virtual bool IsValidConnection(GraphNode a, GraphNode b, out float dist)
        {
            dist = default(float);
            return default;
        }

        class PointGraphUpdatePromise : IGraphUpdatePromise {
            public PointGraph graph;
			public List<GraphUpdateObject> graphUpdates;

			public void Apply (IGraphUpdateContext ctx) {
            }
        }

		/// <summary>
		/// Updates an area in the list graph.
		/// Recalculates possibly affected connections, i.e all connectionlines passing trough the bounds of the guo will be recalculated
		/// </summary>
		IGraphUpdatePromise IUpdatableGraph.ScheduleGraphUpdates (List<GraphUpdateObject> graphUpdates) {
            return default;
        }

#if UNITY_EDITOR
        static readonly Color NodeColor = new Color(0.161f, 0.341f, 1f, 0.5f);

		public override void OnDrawGizmos (DrawingData gizmos, bool drawNodes, RedrawScope redrawScope) {
        }

        static void DrawChildren (CommandBuilder draw, PointGraph graph, Transform tr) {
        }
#endif

        protected override void PostDeserialization(GraphSerializationContext ctx)
        {
        }

        public override void RelocateNodes(Matrix4x4 deltaMatrix)
        {
        }

        protected override void SerializeExtraInfo(GraphSerializationContext ctx)
        {
        }

        protected override void DeserializeExtraInfo (GraphSerializationContext ctx) {
        }
    }
}
