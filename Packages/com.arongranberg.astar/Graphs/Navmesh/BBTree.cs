//#define ASTARDEBUG   //"BBTree Debug" If enables, some queries to the tree will show debug lines. Turn off multithreading when using this since DrawLine calls cannot be called from a different thread

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Awaken.PackageUtilities.Collections;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Profiling;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Pathfinding.Drawing;

namespace Pathfinding.Collections {
	using Pathfinding.Util;

	/// <summary>
	/// Axis Aligned Bounding Box Tree.
	/// Holds a bounding box tree of triangles.
	/// </summary>
	[BurstCompile]
	public struct BBTree {
		/// <summary>Holds all tree nodes</summary>
		ARUnsafeList<BBTreeBox> tree;
		ARUnsafeList<int> nodePermutation;

		const int MaximumLeafSize = 4;

		public IntRect Size => tree.Length == 0 ? default : tree[0].rect;

		// We need a stack while searching the tree.
		// We use a stack allocated array for this to avoid allocations.
		// A tile can at most contain NavmeshBase.VertexIndexMask triangles.
		// This works out to about a million. A perfectly balanced tree can fit this in log2(1000000/4) = 18 levels.
		// but we add a few more levels just to be safe, in case the tree is not perfectly balanced.
		const int MAX_TREE_HEIGHT = 26;

		public void Dispose () {
        }

        public static BBTree Create(UnsafeSpan<int> triangles, UnsafeSpan<Int3> vertices) {
            return default;
        }

        /// <summary>Build a BBTree from a list of triangles.</summary>
        /// <param name="triangles">The triangles. Each triplet of 3 indices represents a node. The triangles are assumed to be in clockwise order.</param>
        /// <param name="vertices">The vertices of the triangles.</param>
        BBTree(UnsafeSpan<int> triangles, UnsafeSpan<Int3> vertices) : this()
        {
        }

        [BurstCompile]
        static void Build(ref UnsafeSpan<int> triangles, ref UnsafeSpan<Int3> vertices, out BBTree bbTree)
        {
            bbTree = default(BBTree);
        }

        static int SplitByX(NativeArray<IntRect> nodesBounds, NativeArray<int> permutation, int from, int to, int divider)
        {
            return default;
        }

        static int SplitByZ(NativeArray<IntRect> nodesBounds, NativeArray<int> permutation, int from, int to, int divider)
        {
            return default;
        }

        static int BuildSubtree(NativeArray<int> permutation, NativeArray<IntRect> nodeBounds, ref ARUnsafeList<int> nodes, ref ARUnsafeList<BBTreeBox> tree, int from, int to, bool odd, int depth)
        {
            return default;
        }

        /// <summary>Calculates the bounding box in XZ space of all nodes between from (inclusive) and to (exclusive)</summary>
        static IntRect NodeBounds(NativeArray<int> permutation, NativeArray<IntRect> nodeBounds, int from, int to)
        {
            return default;
        }

        [BurstCompile]
		public readonly struct ProjectionParams {
			public readonly float2x3 planeProjection;
			public readonly float2 projectedUpNormalized;
			public readonly float3 projectionAxis;
			public readonly float distanceScaleAlongProjectionAxis;
			public readonly DistanceMetric distanceMetric;
			// bools are for some reason not blittable by the burst compiler, so we have to use a byte
			readonly byte alignedWithXZPlaneBacking;

			public bool alignedWithXZPlane => alignedWithXZPlaneBacking != 0;

			/// <summary>
			/// Calculates the squared distance from a point to a box when projected to 2D.
			///
			/// The input rectangle is assumed to be on the XZ plane, and to actually represent an infinitely tall box (along the Y axis).
			///
			/// The planeProjection matrix projects points from 3D to 2D. The box will also be projected.
			/// The upProjNormalized vector is the normalized direction orthogonal to the 2D projection.
			/// It is the direction pointing out of the plane from the projection's point of view.
			///
			/// In the special case that the projection just projects 3D coordinates onto the XZ plane, this is
			/// equivalent to the distance from a point to a rectangle in 2D.
			/// </summary>
			public float SquaredRectPointDistanceOnPlane (IntRect rect, float3 p) {
                return default;
            }

            [BurstCompile(FloatMode = FloatMode.Fast)]
            [IgnoredByDeepProfiler]
            private static float SquaredRectPointDistanceOnPlane(in ProjectionParams projection, ref IntRect rect, ref float3 p)
            {
                return default;
            }

            public ProjectionParams(NNConstraint constraint, GraphTransform graphTransform) : this()
            {
            }
        }

		public float DistanceSqrLowerBound (float3 p, in ProjectionParams projection) {
            return default;
        }

        /// <summary>
        /// Queries the tree for the closest node to p constrained by the NNConstraint trying to improve an existing solution.
        /// Note that this function will only fill in the constrained node.
        /// If you want a node not constrained by any NNConstraint, do an additional search with constraint = NNConstraint.None
        /// </summary>
        /// <param name="p">Point to search around</param>
        /// <param name="constraint">Optionally set to constrain which nodes to return</param>
        /// <param name="distanceSqr">The best squared distance for the previous solution. Will be updated with the best distance
        /// after this search. Supply positive infinity to start the search from scratch.</param>
        /// <param name="previous">This search will start from the previous NNInfo and improve it if possible. Will be updated with the new result.
        /// Even if the search fails on this call, the solution will never be worse than previous.</param>
        /// <param name="nodes">The nodes what this BBTree was built from</param>
        /// <param name="triangles">The triangles that this BBTree was built from</param>
        /// <param name="vertices">The vertices that this BBTree was built from</param>
        /// <param name="projection">Projection parameters derived from the constraint</param>
        public void QueryClosest(float3 p, NNConstraint constraint, in ProjectionParams projection, ref float distanceSqr, ref NNInfo previous, GraphNode[] nodes, UnsafeSpan<int> triangles, UnsafeSpan<Int3> vertices)
        {
        }

        struct CloseNode {
			public int node;
			public float distanceSq;
			public float tieBreakingDistance;
			public float3 closestPointOnNode;
		}

		public enum DistanceMetric: byte {
			Euclidean,
			ScaledManhattan,
		}

		[BurstCompile]
		struct NearbyNodesIterator : IEnumerator<CloseNode> {
			public UnsafeSpan<BoxWithDist> stack;
			public int stackSize;
			public UnsafeSpan<BBTreeBox> tree;
			public UnsafeSpan<int> nodes;
			public UnsafeSpan<int> triangles;
			public UnsafeSpan<Int3> vertices;
			public int indexInLeaf;
			public float3 point;
			public ProjectionParams projection;
			public float distanceThresholdSqr;
			public float tieBreakingDistanceThreshold;
			internal CloseNode current;

			public CloseNode Current => current;

			public struct BoxWithDist {
				public int index;
				public float distSqr;
			}

			public bool MoveNext () {
                return default;
            }

            void IDisposable.Dispose()
            {
            }

            void System.Collections.IEnumerator.Reset() => throw new NotSupportedException();
			object System.Collections.IEnumerator.Current => throw new NotSupportedException();

			// Note: Using FloatMode=Fast here can cause NaNs in rare cases.
			// I have not tracked down why, but it is not unreasonable given that FloatMode=Fast assumes that infinities do not happen.
			[BurstCompile(FloatMode = FloatMode.Default)]
			static bool MoveNext (ref NearbyNodesIterator it) {
                return default;
            }
        }

		struct BBTreeBox {
			public IntRect rect;

			public int nodeOffset;
			public int left, right;

			public bool IsLeaf => nodeOffset >= 0;

			public BBTreeBox (IntRect rect) : this()
            {
            }
        }

        public void DrawGizmos(CommandBuilder draw)
        {
        }

        void DrawGizmos(ref CommandBuilder draw, int boxi, int depth)
        {
        }

        public static unsafe class MemoryInfo {
			static readonly uint IntSize = sizeof(int);
			static readonly uint BBTreeBoxSize = (uint)sizeof(BBTreeBox);

			public static (ulong allocated, ulong used) GetSize(BBTree tree) {
                return default;
            }
        }
	}
}
