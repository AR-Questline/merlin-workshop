using UnityEngine;
using System.Collections.Generic;

namespace Pathfinding {
	using Pathfinding.Pooling;

	/// <summary>
	/// Contains utility methods for getting useful information out of graph.
	/// This class works a lot with the <see cref="Pathfinding.GraphNode"/> class, a useful function to get nodes is <see cref="AstarPath.GetNearest"/>.
	///
	/// See: <see cref="AstarPath.GetNearest"/>
	/// See: <see cref="Pathfinding.GraphUpdateUtilities"/>
	/// See: <see cref="Pathfinding.PathUtilities"/>
	/// </summary>
	public static class GraphUtilities {
		/// <summary>
		/// Convenience method to get a list of all segments of the contours of a graph.
		/// Returns: A list of segments. Every 2 elements form a line segment. The first segment is (result[0], result[1]), the second one is (result[2], result[3]) etc.
		/// The line segments are oriented so that the navmesh is on the right side of the segments when seen from above.
		///
		/// This method works for navmesh, recast, grid graphs and layered grid graphs. For other graph types it will return an empty list.
		///
		/// If you need more information about how the contours are connected you can take a look at the other variants of this method.
		///
		/// <code>
		/// // Get the first graph
		/// var navmesh = AstarPath.active.graphs[0];
		///
		/// // Get all contours of the graph (works for grid, navmesh and recast graphs)
		/// var segments = GraphUtilities.GetContours(navmesh);
		///
		/// // Every 2 elements form a line segment. The first segment is (segments[0], segments[1]), the second one is (segments[2], segments[3]) etc.
		/// // The line segments are oriented so that the navmesh is on the right side of the segments when seen from above.
		/// for (int i = 0; i < segments.Count; i += 2) {
		///     var start = segments[i];
		///     var end = segments[i+1];
		///     Debug.DrawLine(start, end, Color.red, 3);
		/// }
		/// </code>
		///
		/// [Open online documentation to see images]
		/// [Open online documentation to see images]
		/// </summary>
		public static List<Vector3> GetContours (NavGraph graph) {
            return default;
        }

        /// <summary>
        /// Traces the contour of a navmesh.
        ///
        /// [Open online documentation to see images]
        ///
        /// This image is just used to illustrate the difference between chains and cycles. That it shows a grid graph is not relevant.
        /// [Open online documentation to see images]
        ///
        /// See: <see cref="GetContours(NavGraph)"/>
        /// </summary>
        /// <param name="navmesh">The navmesh-like object to trace. This can be a recast or navmesh graph or it could be a single tile in one such graph.</param>
        /// <param name="results">Will be called once for each contour with the contour as a parameter as well as a boolean indicating if the contour is a cycle or a chain (see second image).</param>
        public static void GetContours(NavmeshBase navmesh, System.Action<List<Int3>, bool> results)
        {
        }

#if !ASTAR_NO_GRID_GRAPH
        /// <summary>
        /// Finds all contours of a collection of nodes in a grid graph.
        ///
        /// <code>
        /// var grid = AstarPath.active.data.gridGraph;
        ///
        /// // Find all contours in the graph and draw them using debug lines
        /// GraphUtilities.GetContours(grid, vertices => {
        ///     for (int i = 0; i < vertices.Length; i++) {
        ///         Debug.DrawLine(vertices[i], vertices[(i+1)%vertices.Length], Color.red, 4);
        ///     }
        /// }, 0);
        /// </code>
        ///
        /// In the image below you can see the contour of a graph.
        /// [Open online documentation to see images]
        ///
        /// In the image below you can see the contour of just a part of a grid graph (when the nodes parameter is supplied)
        /// [Open online documentation to see images]
        ///
        /// Contour of a hexagon graph
        /// [Open online documentation to see images]
        ///
        /// See: <see cref="GetContours(NavGraph)"/>
        /// </summary>
        /// <param name="grid">The grid to find the contours of</param>
        /// <param name="callback">The callback will be called once for every contour that is found with the vertices of the contour. The contour always forms a cycle.</param>
        /// <param name="yMergeThreshold">Contours will be simplified if the y coordinates for adjacent vertices differ by no more than this value.</param>
        /// <param name="nodes">Only these nodes will be searched. If this parameter is null then all nodes in the grid graph will be searched.</param>
        /// <param name="connectionFilter">Allows you to disable connections between nodes. If null, no additional filtering will be done. The filter must be symmetric, so that f(A,B) == f(B,A). A contour edge will be generated between two adjacent nodes if this function returns false for the pair.</param>
        public static void GetContours(GridGraph grid, System.Action<Vector3[]> callback, float yMergeThreshold, GridNodeBase[] nodes = null, System.Func<GraphNode, GraphNode, bool> connectionFilter = null)
        {
        }
#endif
    }
}
