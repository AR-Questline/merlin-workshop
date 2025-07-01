using System.Collections.Generic;
using Pathfinding.Pooling;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

namespace Pathfinding {
	/// <summary>
	/// Simplifies a path on a grid graph using a string pulling algorithm.
	/// This is based on a paper called "Toward a String-Pulling Approach to Path Smoothing on Grid Graphs",
	/// with some optimizations as well as fixes for some edge cases that the paper didn't handle.
	///
	/// The result is conceptually similar to the well known funnel string pulling algorithm for navmesh graphs
	/// but it uses a different algorithm.
	///
	/// This class is used by the <see cref="FunnelModifier"/> on grid graphs.
	///
	/// See: <see cref="Funnel"/>
	/// See: <see cref="FunnelModifier"/>
	/// See: article: https://ojs.aaai.org/index.php/SOCS/article/view/18541
	/// </summary>
	public static class GridStringPulling {
		/// <summary>
		///         Z
		///         |
		///         |
		///
		///      3     2
		///       \ | /
		/// --    - X -    ----- X
		///       / | \
		///      0     1
		///
		///         |
		///         |
		/// </summary>
		static int2[] directionToCorners = new int2[] {
			new int2(0, 0),
			new int2(FixedPrecisionScale, 0),
			new int2(FixedPrecisionScale, FixedPrecisionScale),
			new int2(0, FixedPrecisionScale),
		};

		static long Cross (int2 lhs, int2 rhs) {
            return default;
        }

        static long Dot(int2 a, int2 b)
        {
            return default;
        }

        static bool RightOrColinear(int2 a, int2 b, int2 p)
        {
            return default;
        }

        static int2 Perpendicular(int2 v)
        {
            return default;
        }

        struct TriangleBounds {
			int2 d1, d2, d3;
			long t1, t2, t3;

			public TriangleBounds(int2 p1, int2 p2, int2 p3) : this()
            {
            }

            public bool Contains(int2 p)
            {
                return default;
            }
        }

        const int FixedPrecisionScale = 1024;

        static int2 ToFixedPrecision(Vector2 p)
        {
            return default;
        }

        static Vector2 FromFixedPrecision(int2 p)
        {
            return default;
        }

        /// <summary>Returns which side of the line a - b that p lies on</summary>
        static Side Side2D(int2 a, int2 b, int2 p)
        {
            return default;
        }

        static Unity.Profiling.ProfilerMarker marker1 = new Unity.Profiling.ProfilerMarker("Linecast hit");
        static Unity.Profiling.ProfilerMarker marker2 = new Unity.Profiling.ProfilerMarker("Linecast success");
		static Unity.Profiling.ProfilerMarker marker3 = new Unity.Profiling.ProfilerMarker("Trace");
		static Unity.Profiling.ProfilerMarker marker4 = new Unity.Profiling.ProfilerMarker("Neighbours");
		static Unity.Profiling.ProfilerMarker marker5 = new Unity.Profiling.ProfilerMarker("Re-evaluate linecast");
		static Unity.Profiling.ProfilerMarker marker6 = new Unity.Profiling.ProfilerMarker("Init");
		static Unity.Profiling.ProfilerMarker marker7 = new Unity.Profiling.ProfilerMarker("Initloop");

		/// <summary>
		/// Intersection length of the given segment with a square of size Int3.Precision centered at nodeCenter.
		/// The return value is between 0 and sqrt(2).
		/// </summary>
		public static float IntersectionLength (int2 nodeCenter, int2 segmentStart, int2 segmentEnd)
        {
            return default;
        }

        internal static void TestIntersectionLength()
        {
        }

        /// <summary>
        /// Cost of moving across all the nodes in the list, along the given segment.
        /// It is assumed that the segment intersects the nodes. Any potentially intersecting nodes that are not part of the list will be ignored.
        /// </summary>
        static uint LinecastCost(List<GraphNode> trace, int2 segmentStart, int2 segmentEnd, GridGraph gg, System.Func<GraphNode, uint> traversalCost)
        {
            return default;
        }

        enum PredicateFailMode
        {
            Undefined,
            Turn,
            LinecastObstacle,
            LinecastCost,
            ReachedEnd,
		}

        /// <summary>
        /// Simplifies a path on a grid graph using a string pulling algorithm.
        /// See the class documentation for more details.
        /// </summary>
        /// <param name="pathNodes">A list of input nodes. Only the slice of nodes from nodeStartIndex to nodeEndIndex (inclusive) will be used. These must all be of type GridNodeBase and must form a path (i.e. each node must be a neighbor to the next one in the list).</param>
        /// <param name="nodeStartIndex">The index in pathNodes to start from.</param>
        /// <param name="nodeEndIndex">The last index in pathNodes that is used.</param>
        /// <param name="startPoint">A more exact start point for the path. This should be a point inside the first node (if not, it will be clamped to the node's surface).</param>
        /// <param name="endPoint">A more exact end point for the path. This should be a point inside the first node (if not, it will be clamped to the node's surface).</param>
        /// <param name="traversalCost">Can be used to specify how much it costs to traverse each node. If this is null, node penalties and tag penalties will be completely ignored.</param>
        /// <param name="filter">Can be used to filter out additional nodes that should be treated as unwalkable. It is assumed that all nodes in pathNodes pass this filter.</param>
        /// <param name="maxCorners">If you only need the first N points of the result, you can specify that here, to avoid unnecessary work.</param>
        public static List<Vector3> Calculate (List<GraphNode> pathNodes, int nodeStartIndex, int nodeEndIndex, Vector3 startPoint, Vector3 endPoint, System.Func<GraphNode, uint> traversalCost = null, System.Func<GraphNode, bool> filter = null, int maxCorners = int.MaxValue) {
            return default;
        }
    }
}
