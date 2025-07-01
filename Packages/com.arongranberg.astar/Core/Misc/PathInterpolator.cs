using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Pathfinding.Util {
	/// <summary>Interpolates along a sequence of points</summary>
	public class PathInterpolator {
		/// <summary>
		/// Represents a single point on the polyline represented by the <see cref="PathInterpolator"/>.
		/// The cursor is a lightweight structure which can be used to move backwards and forwards along a <see cref="PathInterpolator"/>.
		///
		/// If the <see cref="PathInterpolator"/> changes (e.g. has its path swapped out), then this cursor is invalidated and cannot be used anymore.
		/// </summary>
		public struct Cursor {
			private PathInterpolator interpolator;
			private int version;
			private float currentDistance;
			private float distanceToSegmentStart;
			private float currentSegmentLength;

			/// <summary>
			/// Current segment.
			/// The start and end points of the segment are path[value] and path[value+1].
			/// </summary>
			int segmentIndex { get; set; }

			public int segmentCount {
				get {
					AssertValid();
					return interpolator.path.Count - 1;
				}
			}

			/// <summary>Last point in the path</summary>
			public Vector3 endPoint {
				get {
					AssertValid();
					return interpolator.path[interpolator.path.Count-1];
				}
			}

			/// <summary>
			/// Fraction of the way along the current segment.
			/// 0 is at the start of the segment, 1 is at the end of the segment.
			/// </summary>
			public float fractionAlongCurrentSegment {
				get {
					return currentSegmentLength > 0 ? (currentDistance - distanceToSegmentStart) / currentSegmentLength : 1f;
				}
				set {
					currentDistance = distanceToSegmentStart + Mathf.Clamp01(value) * currentSegmentLength;
				}
			}

			/// <summary>A cursor at the start of the polyline represented by the interpolator</summary>
			public static Cursor StartOfPath (PathInterpolator interpolator) {
                return default;
            }

            /// <summary>
            /// True if this instance has a path set.
            /// See: SetPath
            /// </summary>
            public bool valid {
				get {
					return interpolator != null && interpolator.version == version;
				}
			}

			/// <summary>
			/// Tangent of the curve at the current position.
			/// Not necessarily normalized.
			/// </summary>
			public Vector3 tangent {
				get {
					AssertValid();
					return interpolator.path[segmentIndex+1] - interpolator.path[segmentIndex];
				}
			}

			/// <summary>Remaining distance until the end of the path</summary>
			public float remainingDistance {
				get {
					AssertValid();
					return interpolator.totalDistance - distance;
				}
				set {
					AssertValid();
					distance = interpolator.totalDistance - value;
				}
			}

			/// <summary>Traversed distance from the start of the path</summary>
			public float distance {
				get {
					return currentDistance;
				}
				set {
					AssertValid();
					currentDistance = value;

					while (currentDistance < distanceToSegmentStart && segmentIndex > 0) PrevSegment();
					while (currentDistance > distanceToSegmentStart + currentSegmentLength && segmentIndex < interpolator.path.Count - 2) NextSegment();
				}
			}

			/// <summary>Current position</summary>
			public Vector3 position {
				get {
					AssertValid();
					float t = currentSegmentLength > 0.0001f ? (currentDistance - distanceToSegmentStart) / currentSegmentLength : 0f;
					return Vector3.Lerp(interpolator.path[segmentIndex], interpolator.path[segmentIndex+1], t);
				}
			}

			/// <summary>Appends the remaining path between <see cref="position"/> and <see cref="endPoint"/> to buffer</summary>
			public void GetRemainingPath (List<Vector3> buffer) {
            }

            void AssertValid () {
            }

            /// <summary>
            /// The tangent(s) of the curve at the current position.
            /// Not necessarily normalized.
            ///
            /// Will output t1=<see cref="tangent"/>, t2=<see cref="tangent"/> if on a straight line segment.
            /// Will output the previous and next tangents for the adjacent line segments when on a corner.
            ///
            /// This is similar to <see cref="tangent"/> but can output two tangents instead of one when on a corner.
            /// </summary>
            public void GetTangents (out Vector3 t1, out Vector3 t2) {
                t1 = default(Vector3);
                t2 = default(Vector3);
            }

            /// <summary>
            /// A vector parallel to the local curvature.
            ///
            /// This will be zero on straight line segments, and in the same direction as the rotation axis when on a corner.
            ///
            /// Since this interpolator follows a polyline, the curvature is always either 0 or infinite.
            /// Therefore the magnitude of this vector has no meaning when non-zero. Only the direction matters.
            /// </summary>
            public Vector3 curvatureDirection {
				get {
					GetTangents(out var t1, out var t2);
					var up = Vector3.Cross(t1, t2);
					return up.sqrMagnitude <= 0.000001f ? Vector3.zero : up;
				}
			}

			/// <summary>
			/// Moves the cursor to the next geometric corner in the path.
			///
			/// This is the next geometric corner.
			/// If the original path contained any zero-length segments, they will be skipped over.
			/// </summary>
			public void MoveToNextCorner () {
            }

            /// <summary>
            /// Moves to the closest intersection of the line segment (origin + direction*range.x, origin + direction*range.y).
            /// The closest intersection as measured by the distance along the path is returned.
            ///
            /// If no intersection is found, false will be returned and the cursor remains unchanged.
            ///
            /// The intersection is calculated in XZ space.
            /// </summary>
            /// <param name="origin">A point on the line</param>
            /// <param name="direction">The direction of the line. Need not be normalized.</param>
            /// <param name="range">The range of the line segment along the line. The segment is (origin + direction*range.x, origin + direction*range.y). May be (-inf, +inf) to consider an infinite line.</param>
            public bool MoveToClosestIntersectionWithLineSegment (Vector3 origin, Vector3 direction, Vector2 range) {
                return default;
            }

            /// <summary>Move to the specified segment and move a fraction of the way to the next segment</summary>
            void MoveToSegment (int index, float fractionAlongSegment) {
            }

            /// <summary>Move as close as possible to the specified point</summary>
            public void MoveToClosestPoint (Vector3 point) {
            }

            public void MoveToLocallyClosestPoint (Vector3 point, bool allowForwards = true, bool allowBackwards = true) {
            }

            public void MoveToCircleIntersection2D<T>(Vector3 circleCenter3D, float radius, T transform) where T : IMovementPlane
            {
            }

            /// <summary>
            /// Integrates exp(-|x|/smoothingDistance)/(2*smoothingDistance) from a to b.
            /// The integral from -inf to +inf is 1.
            /// </summary>
            static float IntegrateSmoothingKernel(float a, float b, float smoothingDistance)
            {
                return default;
            }

            /// <summary>Integrates (x - a)*exp(-x/smoothingDistance)/(2*smoothingDistance) from a to b.</summary>
            static float IntegrateSmoothingKernel2(float a, float b, float smoothingDistance)
            {
                return default;
            }

            static Vector3 IntegrateSmoothTangent(Vector3 p1, Vector3 p2, ref Vector3 tangent, ref float distance, float expectedRadius, float smoothingDistance)
            {
                return default;
            }

            public Vector3 EstimateSmoothTangent(Vector3 normalizedTangent, float smoothingDistance, float expectedRadius, Vector3 beforePathStartContribution, bool forward = true, bool backward = true)
            {
                return default;
            }

            public Vector3 EstimateSmoothCurvature(Vector3 tangent, float smoothingDistance, float expectedRadius)
            {
                return default;
            }

            /// <summary>
            /// Moves the agent along the path, stopping to rotate on the spot when the path changes direction.
            ///
            /// Note: The cursor state does not include the rotation of the agent. So if an agent stops in the middle of a rotation, the final state of this struct will be as if the agent completed its rotation.
            ///       If you want to preserve the rotation state as well, keep track of the output tangent, and pass it along to the next call to this function.
            /// </summary>
            /// <param name="time">The number of seconds to move forwards or backwards (if negative).</param>
            /// <param name="speed">Speed in meters/second.</param>
            /// <param name="turningSpeed">Turning speed in radians/second.</param>
            /// <param name="tangent">The current forwards direction of the agent. May be set to the #tangent property if you have no other needs.
            ///               If set to something other than #tangent, the agent will start by rotating to face the #tangent direction.
            ///               This will be replaced with the forwards direction of the agent after moving.
            ///               It will be smoothly interpolated as the agent rotates from one segment to the next.
            ///               It is more precise than the #tangent property after this call, which does not take rotation into account.
            ///               This value is not necessarily normalized.</param>
            public void MoveWithTurningSpeed (float time, float speed, float turningSpeed, ref Vector3 tangent) {
            }

            void PrevSegment()
            {
            }

            void NextSegment()
            {
            }
        }

		List<Vector3> path;
		int version = 1;
		float totalDistance;

		/// <summary>
		/// True if this instance has a path set.
		/// See: SetPath
		/// </summary>
		public bool valid {
			get {
				return path != null;
			}
		}

		public Cursor start {
			get {
				return Cursor.StartOfPath(this);
			}
		}

		public Cursor AtDistanceFromStart (float distance) {
            return default;
        }

        /// <summary>
        /// Set the path to interpolate along.
        /// This will invalidate all existing cursors.
        /// </summary>
        public void SetPath(List<Vector3> path)
        {
        }
    }
}
