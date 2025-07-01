using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Assertions;

namespace Pathfinding {
	using Pathfinding.Pooling;
	using Pathfinding.Collections;
	using Unity.Burst;
	using Unity.Collections;
	using Unity.Mathematics;
	using UnityEngine.Profiling;

	/// <summary>
	/// Implements the funnel algorithm as well as various related methods.
	/// See: http://digestingduck.blogspot.se/2010/03/simple-stupid-funnel-algorithm.html
	/// See: Usually you do not use this class directly. Instead use the <see cref="FunnelModifier"/> component.
	///
	/// <code>
	/// using UnityEngine;
	/// using Pathfinding;
	/// using Pathfinding.Drawing;
	///
	/// public class FunnelExample : MonoBehaviour {
	///     public Transform target = null;
	///
	///     void Update () {
	///         var path = ABPath.Construct(transform.position, target.position);
	///
	///         AstarPath.StartPath(path);
	///         path.BlockUntilCalculated();
	///
	///         // Apply some default adjustments to the path
	///         // not necessary if you are using the Seeker component
	///         new StartEndModifier().Apply(path);
	///
	///         // Split the path into segments and links
	///         var parts = Funnel.SplitIntoParts(path);
	///         // Optionally simplify the path to make it straighter
	///         var nodes = path.path;
	///         Funnel.Simplify(parts, ref nodes);
	///
	///         using (Draw.WithLineWidth(2)) {
	///             // Go through all the parts and draw them in the scene view
	///             for (int i = 0; i < parts.Count; i++) {
	///                 var part = parts[i];
	///                 if (part.type == Funnel.PartType.OffMeshLink) {
	///                     // Draw off-mesh links as a single line
	///                     Draw.Line(part.startPoint, part.endPoint, Color.cyan);
	///                 } else {
	///                     // Calculate the shortest path through the funnel
	///                     var portals = Funnel.ConstructFunnelPortals(nodes, part);
	///                     var pathThroghPortals = Funnel.Calculate(portals, splitAtEveryPortal: false);
	///                     Draw.Polyline(pathThroghPortals, Color.black);
	///                 }
	///             }
	///         }
	///     }
	/// }
	/// </code>
	///
	/// In the image you can see the output from the code example above. The cyan lines represent off-mesh links.
	///
	/// [Open online documentation to see images]
	/// </summary>
	[BurstCompile]
	public static class Funnel {
		/// <summary>Funnel in which the path to the target will be</summary>
		public struct FunnelPortals {
			public List<Vector3> left;
			public List<Vector3> right;
		}

		/// <summary>The type of a <see cref="PathPart"/></summary>
		public enum PartType {
			/// <summary>An off-mesh link between two nodes in the same or different graphs</summary>
			OffMeshLink,
			/// <summary>A sequence of adjacent nodes in the same graph</summary>
			NodeSequence,
		}

		/// <summary>
		/// Part of a path.
		/// This is either a sequence of adjacent triangles
		/// or a link.
		/// See: NodeLink2
		/// </summary>
		public struct PathPart {
			/// <summary>Index of the first node in this part</summary>
			public int startIndex;
			/// <summary>Index of the last node in this part</summary>
			public int endIndex;
			/// <summary>Exact start-point of this part or off-mesh link</summary>
			public Vector3 startPoint;
			/// <summary>Exact end-point of this part or off-mesh link</summary>
			public Vector3 endPoint;
			/// <summary>If this is an off-mesh link or a sequence of nodes in a single graph</summary>
			public PartType type;
		}

		/// <summary>Splits the path into a sequence of parts which are either off-mesh links or sequences of adjacent triangles</summary>

		public static List<PathPart> SplitIntoParts (Path path)
        {
            return default;
        }

        public static void Simplify(List<PathPart> parts, ref List<GraphNode> nodes)
        {
        }

        /// <summary>
        /// Simplifies a funnel path using linecasting.
        /// Running time is roughly O(n^2 log n) in the worst case (where n = end-start)
        /// Actually it depends on how the graph looks, so in theory the actual upper limit on the worst case running time is O(n*m log n) (where n = end-start and m = nodes in the graph)
        /// but O(n^2 log n) is a much more realistic worst case limit.
        ///
        /// Requires graph to implement IRaycastableGraph
        /// </summary>
        public static void Simplify(PathPart part, IRaycastableGraph graph, List<GraphNode> nodes, List<GraphNode> result, int[] tagPenalties, int traversableTags)
        {
        }

        /// <summary>
        /// Removes backtracking in the path.
        /// This can happen when the path goes A -> B -> C -> B -> D.
        /// This method will replace B -> C -> B with just B, when passed aroundIndex=C.
        /// </summary>
        static void RemoveBacktracking(List<GraphNode> nodes, int listStartIndex, int aroundIndex)
        {
        }

        public static FunnelPortals ConstructFunnelPortals(List<GraphNode> nodes, PathPart part)
        {
            return default;
        }

        [BurstCompile]
        public struct FunnelState
        {
            /// <summary>Left side of the funnel</summary>
            public NativeCircularBuffer<float3> leftFunnel;
            /// <summary>Right side of the funnel</summary>
            public NativeCircularBuffer<float3> rightFunnel;
            /// <summary>
            /// Unwrapped version of the funnel portals in 2D space.
            ///
            /// The input is a funnel like in the image below. It may be rotated and twisted.
            /// [Open online documentation to see images]
            /// The output will be a funnel in 2D space like in the image below. All twists and bends will have been straightened out.
            /// [Open online documentation to see images]
            ///
            /// This array is used as a cache and the unwrapped portals are calculated on demand. Thus it may not contain all portals.
            /// </summary>
            public NativeCircularBuffer<float4> unwrappedPortals;

            /// <summary>
            /// If set to anything other than (0,0,0), then all portals will be projected on a plane with this normal.
            ///
            /// This is used to make the funnel fit a rotated graph better.
            /// It is ideally used for grid graphs, but navmesh/recast graphs are probably better off with it set to zero.
            ///
            /// The vector should be normalized (unless zero), in world space, and should never be changed after the first portal has been added (unless the funnel is cleared first).
            /// </summary>
            public float3 projectionAxis;


            public FunnelState(int initialCapacity, Allocator allocator) : this()
            {
            }

            public FunnelState(FunnelPortals portals, Allocator allocator) : this(portals.left.Count, allocator)
            {
            }

            public FunnelState Clone()
            {
                return default;
            }

            public void Clear()
            {
            }

            public void PopStart()
            {
            }

            public void PopEnd()
            {
            }

            public void Pop(bool fromStart)
            {
            }

            public void PushStart(float3 newLeftPortal, float3 newRightPortal)
            {
            }

            /// <summary>True if a and b lie on different sides of the infinite line that passes through start and end</summary>
            static bool DifferentSidesOfLine(float3 start, float3 end, float3 a, float3 b)
            {
                return default;
            }

            /// <summary>
            /// True if it is reasonable that the given start point has passed the first portal in the funnel.
            ///
            /// If this is true, it is most likely better to pop the start/end portal of the funnel first.
            ///
            /// This can be used as a heuristic to determine if the agent has passed a portal and we should pop it,
            /// in those cases when node information is not available (e.g. because the path has been invalidated).
            /// </summary>
            public bool IsReasonableToPopStart(float3 startPoint, float3 endPoint)
            {
                return default;
            }

            /// <summary>Like <see cref="IsReasonableToPopStart"/> but for the end of the funnel</summary>
            public bool IsReasonableToPopEnd(float3 startPoint, float3 endPoint)
            {
                return default;
            }

            [BurstCompile]
            static void PushStart(ref NativeCircularBuffer<float3> leftPortals, ref NativeCircularBuffer<float3> rightPortals, ref NativeCircularBuffer<float4> unwrappedPortals, ref float3 newLeftPortal, ref float3 newRightPortal, ref float3 projectionAxis)
            {
            }

            public void Splice(int startIndex, int toRemove, List<float3> newLeftPortal, List<float3> newRightPortal)
            {
            }

            public void PushEnd(Vector3 newLeftPortal, Vector3 newRightPortal)
            {
            }

            public void Push(bool toStart, Vector3 newLeftPortal, Vector3 newRightPortal)
            {
            }

            public void Dispose()
            {
            }

            /// <summary>
            /// Calculate the shortest path through the funnel.
            ///
            /// Returns: The number of corners added to the result array.
            ///
            /// See: http://digestingduck.blogspot.se/2010/03/simple-stupid-funnel-algorithm.html
            /// </summary>
            /// <param name="maxCorners">The maximum number of corners to add to the result array. Should be positive.</param>
            /// <param name="result">Output indices. Contains an index as well as possibly the \reflink{RightSideBit} set. Corresponds to an index of the left or right portals, depending on if \reflink{RightSideBit} is set. This must point to an array which is at least maxCorners long.</param>
            /// <param name="startPoint">Start point of the funnel. The agent will move from here to the best point along the first portal.</param>
            /// <param name="endPoint">End point of the funnel.</param>
            /// <param name="lastCorner">True if the final corner of the path was reached. If true, then the return value is guaranteed to be at most maxCorners - 1 (unless maxCorners = 0).</param>
            public int CalculateNextCornerIndices(int maxCorners, NativeArray<int> result, float3 startPoint, float3 endPoint, out bool lastCorner)
            {
                lastCorner = default(bool);
                return default;
            }

            public void CalculateNextCorners(int maxCorners, bool splitAtEveryPortal, float3 startPoint, float3 endPoint, NativeList<float3> result)
            {
            }

            public void ConvertCornerIndicesToPath(NativeArray<int> indices, int numCorners, bool splitAtEveryPortal, float3 startPoint, float3 endPoint, bool lastCorner, NativeList<float3> result)
            {
            }

            public void ConvertCornerIndicesToPathProjected(UnsafeSpan<int> indices, bool splitAtEveryPortal, float3 startPoint, float3 endPoint, bool lastCorner, NativeList<float3> result, float3 up)
            {
            }

            public float4x3 UnwrappedPortalsToWorldMatrix(float3 up)
            {
                return default;
            }

            [BurstCompile]
            public static void ConvertCornerIndicesToPathProjected (ref FunnelState funnelState, ref UnsafeSpan<int> indices, bool splitAtEveryPortal, in float3 startPoint, in float3 endPoint, bool lastCorner, in float3 projectionAxis, ref UnsafeSpan<float3> result, in float3 up) {
            }

            static void CalculatePortalIntersections(int startIndex, int endIndex, NativeCircularBuffer<float3> leftPortals, NativeCircularBuffer<float3> rightPortals, NativeCircularBuffer<float4> unwrappedPortals, float2 from, float2 to, NativeList<float3> result)
            {
            }
        }

        private static float2 Unwrap(float3 leftPortal, float3 rightPortal, float2 leftUnwrappedPortal, float2 rightUnwrappedPortal, float3 point, float sideMultiplier, float3 projectionAxis)
        {
            return default;
        }

        /// <summary>True if b is to the right of or on the line from (0,0) to a</summary>
        private static bool RightOrColinear(Vector2 a, Vector2 b)
        {
            return default;
        }

        /// <summary>True if b is to the left of or on the line from (0,0) to a</summary>
        private static bool LeftOrColinear (Vector2 a, Vector2 b) {
            return default;
        }

        /// <summary>
        /// Calculate the shortest path through the funnel.
        ///
        /// The path will be unwrapped into 2D space before the funnel algorithm runs.
        /// This makes it possible to support the funnel algorithm in XY space as well as in more complicated cases, such as on curved worlds.
        /// [Open online documentation to see images]
        ///
        /// [Open online documentation to see images]
        ///
        /// See: Unwrap
        /// </summary>
        /// <param name="funnel">The portals of the funnel. The first and last vertices portals must be single points (so for example left[0] == right[0]).</param>
        /// <param name="splitAtEveryPortal">If true, then a vertex will be inserted every time the path crosses a portal
        ///  instead of only at the corners of the path. The result will have exactly one vertex per portal if this is enabled.
        ///  This may introduce vertices with the same position in the output (esp. in corners where many portals meet).</param>
        public static List<Vector3> Calculate(FunnelPortals funnel, bool splitAtEveryPortal)
        {
            return default;
        }

        public const int RightSideBit = 1 << 30;
		public const int FunnelPortalIndexMask = RightSideBit - 1;

		/// <summary>
		/// Calculate the shortest path through the funnel.
		///
		/// Returns: The number of corners added to the funnelPath array.
		///
		/// See: http://digestingduck.blogspot.se/2010/03/simple-stupid-funnel-algorithm.html
		/// </summary>
		/// <param name="leftPortals">Left side of the funnel. Should not contain the start point.</param>
		/// <param name="rightPortals">Right side of the funnel. Should not contain the end point.</param>
		/// <param name="unwrappedPortals">Cache of unwrapped portal segments. This may be empty, but it will be filled with unwrapped portals and next time you run the algorithm it will be faster.</param>
		/// <param name="startPoint">Start point of the funnel. The agent will move from here to the best point between leftPortals[0] and rightPortals[0].</param>
		/// <param name="endPoint">End point of the funnel.</param>
		/// <param name="funnelPath">Output indices. Contains an index as well as possibly the \reflink{RightSideBit} set. Corresponds to an index into leftPortals or rightPortals depending on if \reflink{RightSideBit} is set. This must point to an array which is at least maxCorners long.</param>
		/// <param name="lastCorner">True if the final corner of the path was reached. If true, then the return value is guaranteed to be at most maxCorners - 1 (unless maxCorners = 0).</param>
		/// <param name="maxCorners">The first N corners of the optimized path will be calculated. Calculating fewer corners is faster. Pass int.MaxValue if you want to calculate all corners.</param>
		/// <param name="projectionAxis">If set to anything other than (0,0,0), then all portals will be projected on a plane with this normal.</param>
		[BurstCompile]
		static unsafe int Calculate (ref NativeCircularBuffer<float4> unwrappedPortals, ref NativeCircularBuffer<float3> leftPortals, ref NativeCircularBuffer<float3> rightPortals, ref float3 startPoint, ref float3 endPoint, ref UnsafeSpan<int> funnelPath, int maxCorners, ref float3 projectionAxis, out bool lastCorner) {
            lastCorner = default(bool);
            return default;
        }
    }
}
