using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Pathfinding {
	/// <summary>
	/// Defines a shape for a Pathfinding.GraphUpdateObject.
	/// The shape consists of a number of points which it can either calculate the convex hull of or use as a polygon directly.
	///
	/// A shape is essentially a 2D shape however it can be rotated arbitrarily.
	/// When a matrix and a list of points is specified in the constructor the matrix decides what direction
	/// is the 'up' direction. When checking if a point is contained in the shape, the point will be projected down
	/// on a plane where the 'up' direction is the normal and then it will check if the shape contains the point.
	///
	/// See: Pathfinding.GraphUpdateObject.shape
	/// </summary>
	public class GraphUpdateShape {
		Vector3[] _points;
		Vector3[] _convexPoints;
		bool _convex;
		Vector3 right = Vector3.right;
		Vector3 forward = Vector3.forward;
		Vector3 up = Vector3.up;
		Vector3 origin;
		public float minimumHeight;

		/// <summary>Shape optimized for burst</summary>
		public struct BurstShape {
			[DeallocateOnJobCompletion]
			NativeArray<Vector3> points;
			float3 origin, right, forward;
			bool containsEverything;

			public BurstShape(GraphUpdateShape scene, Allocator allocator) : this()
            {
            }

            /// <summary>Shape that contains everything</summary>
            public static BurstShape Everything => new BurstShape
            {
                points = new NativeArray<Vector3>(0, Allocator.Persistent),
                origin = float3.zero,
                right = float3.zero,
                forward = float3.zero,
                containsEverything = true,
            };

            public bool Contains(float3 point)
            {
                return default;
            }
        }

		/// <summary>
		/// Gets or sets the points of the polygon in the shape.
		/// These points should be specified in clockwise order.
		/// Will automatically calculate the convex hull if <see cref="convex"/> is set to true
		/// </summary>
		public Vector3[] points {
			get {
				return _points;
			}
			set {
				_points = value;
				if (convex) CalculateConvexHull();
			}
		}

		/// <summary>
		/// Sets if the convex hull of the points should be calculated.
		/// Convex hulls are faster but non-convex hulls can be used to specify more complicated shapes.
		/// </summary>
		public bool convex {
			get {
				return _convex;
			}
			set {
				if (_convex != value && value) {
					CalculateConvexHull();
				}
				_convex = value;
			}
		}

		public GraphUpdateShape () {
        }

        /// <summary>
        /// Construct a shape.
        /// See: <see cref="convex"/>
        /// </summary>
        /// <param name="points">Contour of the shape in local space with respect to the matrix (i.e the shape should be in the XZ plane, the Y coordinate will only affect the bounds)</param>
        /// <param name="convex">If true, the convex hull of the points will be calculated.</param>
        /// <param name="matrix">local to world space matrix for the points. The matrix determines the up direction of the shape.</param>
        /// <param name="minimumHeight">If the points would be in the XZ plane only, the shape would not have a height and then it might not
        /// 		include any points inside it (as testing for inclusion is done in 3D space when updating graphs). This ensures
        /// 		 that the shape has at least the minimum height (in the up direction that the matrix specifies).</param>
        public GraphUpdateShape (Vector3[] points, bool convex, Matrix4x4 matrix, float minimumHeight) {
        }

        void CalculateConvexHull()
        {
        }

        /// <summary>World space bounding box of this shape</summary>
        public Bounds GetBounds()
        {
            return default;
        }

        public static Bounds GetBounds(Vector3[] points, Matrix4x4 matrix, float minimumHeight)
        {
            return default;
        }

        static Bounds GetBounds (Vector3[] points, Vector3 right, Vector3 up, Vector3 forward, Vector3 origin, float minimumHeight) {
            return default;
        }

        public bool Contains(GraphNode node)
        {
            return default;
        }

        public bool Contains(Vector3 point)
        {
            return default;
        }
    }
}
