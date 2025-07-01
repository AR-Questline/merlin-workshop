using Pathfinding.Util;
using Pathfinding.Pooling;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;

namespace Pathfinding {
	/// <summary>
	/// Contains useful functions for working with paths and nodes.
	/// This class works a lot with the <see cref="Pathfinding.GraphNode"/> class, a useful function to get nodes is AstarPath.GetNearest.
	/// See: <see cref="AstarPath.GetNearest"/>
	/// See: <see cref="Pathfinding.GraphUpdateUtilities"/>
	/// See: <see cref="Pathfinding.GraphUtilities"/>
	/// </summary>
	public static class PathUtilities {
		/// <summary>
		/// Returns if there is a walkable path from node1 to node2.
		/// This method is extremely fast because it only uses precalculated information.
		///
		/// <code>
		/// GraphNode node1 = AstarPath.active.GetNearest(point1, NNConstraint.Walkable).node;
		/// GraphNode node2 = AstarPath.active.GetNearest(point2, NNConstraint.Walkable).node;
		///
		/// if (PathUtilities.IsPathPossible(node1, node2)) {
		///     // Yay, there is a path between those two nodes
		/// }
		/// </code>
		///
		/// Equivalent to calling <see cref="IsPathPossible(List<GraphNode>)"/> with a list containing node1 and node2.
		///
		/// See: graph-updates (view in online documentation for working links)
		/// See: <see cref="AstarPath.GetNearest"/>
		/// See: <see cref="Pathfinding.HierarchicalGraph"/>
		/// </summary>
		public static bool IsPathPossible (GraphNode node1, GraphNode node2) {
            return default;
        }

        /// <summary>
        /// Returns if there are walkable paths between all nodes in the list.
        ///
        /// Returns true for empty lists.
        ///
        /// See: graph-updates (view in online documentation for working links)
        /// See: <see cref="AstarPath.GetNearest"/>
        /// </summary>
        public static bool IsPathPossible(List<GraphNode> nodes)
        {
            return default;
        }

        /// <summary>
        /// Returns if there are walkable paths between all nodes in the list.
        ///
        /// This method will actually only check if the first node can reach all other nodes. However this is
        /// equivalent in 99% of the cases since almost always the graph connections are bidirectional.
        /// If you are not aware of any cases where you explicitly create unidirectional connections
        /// this method can be used without worries.
        ///
        /// Returns true for empty lists
        ///
        /// Warning: This method is significantly slower than the IsPathPossible method which does not take a tagMask
        ///
        /// See: graph-updates (view in online documentation for working links)
        /// See: <see cref="AstarPath.GetNearest"/>
        /// </summary>
        public static bool IsPathPossible(List<GraphNode> nodes, int tagMask)
        {
            return default;
        }

        /// <summary>
        /// Returns all nodes reachable from the seed node.
        /// This function performs a DFS (depth-first-search) or flood fill of the graph and returns all nodes which can be reached from
        /// the seed node. In almost all cases this will be identical to returning all nodes which have the same area as the seed node.
        /// In the editor areas are displayed as different colors of the nodes.
        /// The only case where it will not be so is when there is a one way path from some part of the area to the seed node
        /// but no path from the seed node to that part of the graph.
        ///
        /// The returned list is not sorted in any particular way.
        ///
        /// Depending on the number of reachable nodes, this function can take quite some time to calculate
        /// so don't use it too often or it might affect the framerate of your game.
        ///
        /// See: bitmasks (view in online documentation for working links).
        ///
        /// Returns: A List<Node> containing all nodes reachable from the seed node.
        /// For better memory management the returned list should be pooled, see Pathfinding.Pooling.ListPool.
        /// </summary>
        /// <param name="seed">The node to start the search from.</param>
        /// <param name="tagMask">Optional mask for tags. This is a bitmask.</param>
        /// <param name="filter">Optional filter for which nodes to search. You can combine this with tagMask = -1 to make the filter determine everything.
        ///      Only walkable nodes are searched regardless of the filter. If the filter function returns false the node will be treated as unwalkable.</param>
        public static List<GraphNode> GetReachableNodes(GraphNode seed, int tagMask = -1, System.Func<GraphNode, bool> filter = null)
        {
            return default;
        }

        static Queue<GraphNode> BFSQueue;
		static Dictionary<GraphNode, int> BFSMap;

		/// <summary>
		/// Returns all nodes up to a given node-distance from the seed node.
		/// This function performs a BFS (<a href="https://en.wikipedia.org/wiki/Breadth-first_search">breadth-first search</a>) or flood fill of the graph and returns all nodes within a specified node distance which can be reached from
		/// the seed node. In almost all cases when depth is large enough this will be identical to returning all nodes which have the same area as the seed node.
		/// In the editor areas are displayed as different colors of the nodes.
		/// The only case where it will not be so is when there is a one way path from some part of the area to the seed node
		/// but no path from the seed node to that part of the graph.
		///
		/// The returned list is sorted by node distance from the seed node
		/// i.e distance is measured in the number of nodes the shortest path from seed to that node would pass through.
		/// Note that the distance measurement does not take heuristics, penalties or tag penalties.
		///
		/// Depending on the number of nodes, this function can take quite some time to calculate
		/// so don't use it too often or it might affect the framerate of your game.
		///
		/// Returns: A List<GraphNode> containing all nodes reachable up to a specified node distance from the seed node.
		/// For better memory management the returned list should be pooled, see Pathfinding.Pooling.ListPool
		///
		/// Warning: This method is not thread safe. Only use it from the Unity thread (i.e normal game code).
		///
		/// The video below shows the BFS result with varying values of depth. Points are sampled on the nodes using <see cref="GetPointsOnNodes"/>.
		/// [Open online documentation to see videos]
		///
		/// <code>
		/// var seed = AstarPath.active.GetNearest(transform.position, NNConstraint.Walkable).node;
		/// var nodes = PathUtilities.BFS(seed, 10);
		/// foreach (var node in nodes) {
		///     Debug.DrawRay((Vector3)node.position, Vector3.up, Color.red, 10);
		/// }
		/// </code>
		/// </summary>
		/// <param name="seed">The node to start the search from.</param>
		/// <param name="depth">The maximum node-distance from the seed node.</param>
		/// <param name="tagMask">Optional mask for tags. This is a bitmask.</param>
		/// <param name="filter">Optional filter for which nodes to search. You can combine this with depth = int.MaxValue and tagMask = -1 to make the filter determine everything.
		///      Only walkable nodes are searched regardless of the filter. If the filter function returns false the node will be treated as unwalkable.</param>
		public static List<GraphNode> BFS (GraphNode seed, int depth, int tagMask = -1, System.Func<GraphNode, bool> filter = null)
        {
            return default;
        }

        /// <summary>
        /// Returns points in a spiral centered around the origin with a minimum clearance from other points.
        /// The points are laid out on the involute of a circle
        /// See: http://en.wikipedia.org/wiki/Involute
        /// Which has some nice properties.
        /// All points are separated by clearance world units.
        /// This method is O(n), yes if you read the code you will see a binary search, but that binary search
        /// has an upper bound on the number of steps, so it does not yield a log factor.
        ///
        /// Note: Consider recycling the list after usage to reduce allocations.
        /// See: Pathfinding.Pooling.ListPool
        /// </summary>
        public static List<Vector3> GetSpiralPoints(int count, float clearance)
        {
            return default;
        }

        /// <summary>
        /// Returns the XZ coordinate of the involute of circle.
        /// See: http://en.wikipedia.org/wiki/Involute
        /// </summary>
        private static Vector3 InvoluteOfCircle(float a, float t)
        {
            return default;
        }

        /// <summary>
        /// Will calculate a number of points around p which are on the graph and are separated by clearance from each other.
        /// This is like GetPointsAroundPoint except that previousPoints are treated as being in world space.
        /// The average of the points will be found and then that will be treated as the group center.
        /// </summary>
        /// <param name="p">The point to generate points around</param>
        /// <param name="g">The graph to use for linecasting. If you are only using one graph, you can get this by AstarPath.active.graphs[0] as IRaycastableGraph.
        /// Note that not all graphs are raycastable, recast, navmesh and grid graphs are raycastable. On recast and navmesh it works the best.</param>
        /// <param name="previousPoints">The points to use for reference. Note that these are in world space.
        ///      The new points will overwrite the existing points in the list. The result will be in world space.</param>
        /// <param name="radius">The final points will be at most this distance from p.</param>
        /// <param name="clearanceRadius">The points will if possible be at least this distance from each other.</param>
        public static void GetPointsAroundPointWorld(Vector3 p, IRaycastableGraph g, List<Vector3> previousPoints, float radius, float clearanceRadius)
        {
        }

        /// <summary>
        /// Will calculate a number of points around center which are on the graph and are separated by clearance from each other.
        /// The maximum distance from center to any point will be radius.
        /// Points will first be tried to be laid out as previousPoints and if that fails, random points will be selected.
        /// This is great if you want to pick a number of target points for group movement. If you pass all current agent points from e.g the group's average position
        /// this method will return target points so that the units move very little within the group, this is often aesthetically pleasing and reduces jitter if using
        /// some kind of local avoidance.
        ///
        /// TODO: Write unit tests
        /// </summary>
        /// <param name="center">The point to generate points around</param>
        /// <param name="g">The graph to use for linecasting. If you are only using one graph, you can get this by AstarPath.active.graphs[0] as IRaycastableGraph.
        /// Note that not all graphs are raycastable, recast, navmesh and grid graphs are raycastable. On recast and navmesh it works the best.</param>
        /// <param name="previousPoints">The points to use for reference. Note that these should not be in world space. They are treated as relative to center.
        ///      The new points will overwrite the existing points in the list. The result will be in world space, not relative to center.</param>
        /// <param name="radius">The final points will be at most this distance from center.</param>
        /// <param name="clearanceRadius">The points will if possible be at least this distance from each other.</param>
        public static void GetPointsAroundPoint(Vector3 center, IRaycastableGraph g, List<Vector3> previousPoints, float radius, float clearanceRadius)
        {
        }

        [BurstCompile(FloatMode = FloatMode.Fast)]
        struct JobFormationPacked : IJob
        {
            public NativeArray<float3> positions;
            public float3 destination;
            public float agentRadius;
            public NativeMovementPlane movementPlane;

            public float CollisionTime(float2 pos1, float2 pos2, float2 v1, float2 v2, float r1, float r2)
            {
                return default;
            }

            struct DistanceComparer : IComparer<int>
            {
                public NativeArray<float2> positions;

                public int Compare(int x, int y)
                {
                    return default;
                }
            }

            public void Execute()
            {
            }
        }

        public static void FormationPacked(List<Vector3> currentPositions, Vector3 destination, float clearanceRadius, NativeMovementPlane movementPlane)
        {
        }

        public enum FormationMode
        {
            SinglePoint,
            Packed,
        }

        public static List<Vector3> FormationDestinations(List<IAstarAI> group, Vector3 destination, FormationMode formationMode, float marginFactor = 0.1f)
        {
            return default;
        }

        class ConstrainToSet : NNConstraint
        {
            public HashSet<GraphNode> nodes;

            public override bool Suitable(GraphNode node)
            {
                return default;
            }
        }

        public static void GetPointsAroundPointWorldFlexible(Vector3 center, Quaternion rotation, List<Vector3> positions)
        {
        }

        /// <summary>
        /// Returns randomly selected points on the specified nodes with each point being separated by clearanceRadius from each other.
        /// Selecting points ON the nodes only works for TriangleMeshNode (used by Recast Graph and Navmesh Graph) and GridNode (used by GridGraph).
        /// For other node types, only the positions of the nodes will be used.
        ///
        /// clearanceRadius will be reduced if no valid points can be found.
        ///
        /// Note: This method assumes that the nodes in the list have the same type for some special cases.
        /// More specifically if the first node is not a TriangleMeshNode or a GridNode, it will use a fast path
        /// which assumes that all nodes in the list have the same surface area (which usually is a surface area of zero and the
        /// nodes are all PointNodes).
        /// </summary>
        public static List<Vector3> GetPointsOnNodes (List<GraphNode> nodes, int count, float clearanceRadius = 0) {
            return default;
        }
    }
}
