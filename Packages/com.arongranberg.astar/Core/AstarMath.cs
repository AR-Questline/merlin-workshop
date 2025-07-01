using UnityEngine;
using System.Collections.Generic;
using System;

namespace Pathfinding {
	using Pathfinding.Util;
	using Unity.Mathematics;
	using Unity.Burst;
	using Pathfinding.Collections;
	using Pathfinding.Pooling;

	/// <summary>Contains various spline functions.</summary>
	public static class AstarSplines {
		public static Vector3 CatmullRom (Vector3 previous, Vector3 start, Vector3 end, Vector3 next, float elapsedTime) {
            return default;
        }

        /// <summary>Returns a point on a cubic bezier curve. t is clamped between 0 and 1</summary>
        public static Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            return default;
        }

        /// <summary>Returns the derivative for a point on a cubic bezier curve. t is clamped between 0 and 1</summary>
        public static Vector3 CubicBezierDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            return default;
        }

        /// <summary>Returns the second derivative for a point on a cubic bezier curve. t is clamped between 0 and 1</summary>
        public static Vector3 CubicBezierSecondDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            return default;
        }
    }

	/// <summary>
	/// Various vector math utility functions.
	/// Version: A lot of functions in the Polygon class have been moved to this class
	/// the names have changed slightly and everything now consistently assumes a left handed
	/// coordinate system now instead of sometimes using a left handed one and sometimes
	/// using a right handed one. This is why the 'Left' methods in the Polygon class redirect
	/// to methods named 'Right'. The functionality is exactly the same.
	///
	/// Note the difference between segments and lines. Lines are infinitely
	/// long but segments have only a finite length.
	/// </summary>
	public static class VectorMath {
		/// <summary>
		/// Complex number multiplication.
		/// Returns: a * b
		///
		/// Used to rotate vectors in an efficient way.
		///
		/// See: https://en.wikipedia.org/wiki/Complex_number<see cref="Multiplication_and_division"/>
		/// </summary>
		public static Vector2 ComplexMultiply (Vector2 a, Vector2 b) {
            return default;
        }

        /// <summary>
        /// Complex number multiplication.
        /// Returns: a * b
        ///
        /// Used to rotate vectors in an efficient way.
        ///
        /// See: https://en.wikipedia.org/wiki/Complex_number<see cref="Multiplication_and_division"/>
        /// </summary>
        public static float2 ComplexMultiply (float2 a, float2 b) {
            return default;
        }

        /// <summary>
        /// Complex number multiplication.
        /// Returns: a * conjugate(b)
        ///
        /// Used to rotate vectors in an efficient way.
        ///
        /// See: https://en.wikipedia.org/wiki/Complex_number<see cref="Multiplication_and_division"/>
        /// See: https://en.wikipedia.org/wiki/Complex_conjugate
        /// </summary>
        public static float2 ComplexMultiplyConjugate (float2 a, float2 b) {
            return default;
        }

        /// <summary>
        /// Complex number multiplication.
        /// Returns: a * conjugate(b)
        ///
        /// Used to rotate vectors in an efficient way.
        ///
        /// See: https://en.wikipedia.org/wiki/Complex_number<see cref="Multiplication_and_division"/>
        /// See: https://en.wikipedia.org/wiki/Complex_conjugate
        /// </summary>
        public static Vector2 ComplexMultiplyConjugate (Vector2 a, Vector2 b) {
            return default;
        }

        /// <summary>
        /// Returns the closest point on the line.
        /// The line is treated as infinite.
        /// See: ClosestPointOnSegment
        /// See: ClosestPointOnLineFactor
        /// </summary>
        public static Vector3 ClosestPointOnLine (Vector3 lineStart, Vector3 lineEnd, Vector3 point) {
            return default;
        }

        /// <summary>
        /// Factor along the line which is closest to the point.
        /// Returned value is in the range [0,1] if the point lies on the segment otherwise it just lies on the line.
        /// The closest point can be calculated using (end-start)*factor + start.
        ///
        /// See: ClosestPointOnLine
        /// See: ClosestPointOnSegment
        /// </summary>
        public static float ClosestPointOnLineFactor (Vector3 lineStart, Vector3 lineEnd, Vector3 point) {
            return default;
        }

        /// <summary>
        /// Factor along the line which is closest to the point.
        /// Returned value is in the range [0,1] if the point lies on the segment otherwise it just lies on the line.
        /// The closest point can be calculated using (end-start)*factor + start
        /// </summary>
        public static float ClosestPointOnLineFactor (float3 lineStart, float3 lineEnd, float3 point) {
            return default;
        }

        /// <summary>
        /// Factor along the line which is closest to the point.
        /// Returned value is in the range [0,1] if the point lies on the segment otherwise it just lies on the line.
        /// The closest point can be calculated using (end-start)*factor + start
        /// </summary>
        public static float ClosestPointOnLineFactor (Int3 lineStart, Int3 lineEnd, Int3 point) {
            return default;
        }

        /// <summary>
        /// Factor of the nearest point on the segment.
        /// Returned value is in the range [0,1] if the point lies on the segment otherwise it just lies on the line.
        /// The closest point can be calculated using (end-start)*factor + start;
        /// </summary>
        public static float ClosestPointOnLineFactor(Vector2Int lineStart, Vector2Int lineEnd, Vector2Int point)
        {
            return default;
        }

        /// <summary>
        /// Returns the closest point on the segment.
        /// The segment is NOT treated as infinite.
        /// See: ClosestPointOnLine
        /// See: ClosestPointOnSegmentXZ
        /// </summary>
        public static Vector3 ClosestPointOnSegment (Vector3 lineStart, Vector3 lineEnd, Vector3 point) {
            return default;
        }

        /// <summary>
        /// Returns the closest point on the segment in the XZ plane.
        /// The y coordinate of the result will be the same as the y coordinate of the point parameter.
        ///
        /// The segment is NOT treated as infinite.
        /// See: ClosestPointOnSegment
        /// See: ClosestPointOnLine
        /// </summary>
        public static Vector3 ClosestPointOnSegmentXZ (Vector3 lineStart, Vector3 lineEnd, Vector3 point) {
            return default;
        }

        /// <summary>
        /// Returns the approximate shortest squared distance between x,z and the segment p-q.
        /// The segment is not considered infinite.
        /// This function is not entirely exact, but it is about twice as fast as DistancePointSegment2.
        /// TODO: Is this actually approximate? It looks exact.
        /// </summary>
        public static float SqrDistancePointSegmentApproximate(int x, int z, int px, int pz, int qx, int qz)
        {
            return default;
        }

        /// <summary>
        /// Returns the approximate shortest squared distance between x,z and the segment p-q.
        /// The segment is not considered infinite.
        /// This function is not entirely exact, but it is about twice as fast as DistancePointSegment2.
        /// TODO: Is this actually approximate? It looks exact.
        /// </summary>
        public static float SqrDistancePointSegmentApproximate (Int3 a, Int3 b, Int3 p) {
            return default;
        }

        /// <summary>
        /// Returns the squared distance between p and the segment a-b.
        /// The line is not considered infinite.
        /// </summary>
        public static float SqrDistancePointSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            return default;
        }

        /// <summary>
        /// 3D minimum distance between 2 segments.
        /// Input: two 3D line segments S1 and S2
        /// Returns: the shortest squared distance between S1 and S2
        /// </summary>
        public static float SqrDistanceSegmentSegment(Vector3 s1, Vector3 e1, Vector3 s2, Vector3 e2)
        {
            return default;
        }

        /// <summary>
        /// Determinant of the 2x2 matrix [c1, c2].
        ///
        /// This is useful for many things, like calculating distances between lines and points.
        ///
        /// Equivalent to Cross(new float3(c1, 0), new float 3(c2, 0)).z
        /// </summary>
        public static float Determinant(float2 c1, float2 c2)
        {
            return default;
        }

        /// <summary>Squared distance between two points in the XZ plane</summary>
        public static float SqrDistanceXZ(Vector3 a, Vector3 b)
        {
            return default;
        }

        /// <summary>
        /// Signed area of a triangle multiplied by 2.
        /// This will be negative for clockwise triangles and positive for counter-clockwise ones
        /// </summary>
        public static long SignedTriangleAreaTimes2(int2 a, int2 b, int2 c)
        {
            return default;
        }

        /// <summary>
        /// Signed area of a triangle in the XZ plane multiplied by 2.
        /// This will be negative for clockwise triangles and positive for counter-clockwise ones
        /// </summary>
        public static long SignedTriangleAreaTimes2XZ(Int3 a, Int3 b, Int3 c)
        {
            return default;
        }

        /// <summary>
        /// Signed area of a triangle in the XZ plane multiplied by 2.
        /// This will be negative for clockwise triangles and positive for counter-clockwise ones.
        /// </summary>
        public static float SignedTriangleAreaTimes2XZ(Vector3 a, Vector3 b, Vector3 c)
        {
            return default;
        }

        /// <summary>
        /// Returns if p lies on the right side of the line a - b.
        /// Uses XZ space. Does not return true if the points are colinear.
        /// </summary>
        public static bool RightXZ(Vector3 a, Vector3 b, Vector3 p)
        {
            return default;
        }

        /// <summary>
        /// Returns if p lies on the right side of the line a - b.
        /// Uses XZ space. Does not return true if the points are colinear.
        /// </summary>
        public static bool RightXZ(Int3 a, Int3 b, Int3 p)
        {
            return default;
        }

        /// <summary>
        /// Returns if p lies on the right side of the line a - b.
        /// Does not return true if the points are colinear.
        /// </summary>
        public static bool Right(int2 a, int2 b, int2 p)
        {
            return default;
        }

        /// <summary>
        /// Returns which side of the line a - b that p lies on.
        /// Uses XZ space.
        /// </summary>
        public static Side SideXZ(Int3 a, Int3 b, Int3 p)
        {
            return default;
        }

        /// <summary>
        /// Returns if p lies on the right side of the line a - b.
        /// Also returns true if the points are colinear.
        /// </summary>
        public static bool RightOrColinear(Vector2 a, Vector2 b, Vector2 p)
        {
            return default;
        }

        /// <summary>
        /// Returns if p lies on the right side of the line a - b.
        /// Also returns true if the points are colinear.
        /// </summary>
        public static bool RightOrColinear(Vector2Int a, Vector2Int b, Vector2Int p)
        {
            return default;
        }

        /// <summary>
        /// Returns if p lies on the left side of the line a - b.
        /// Uses XZ space. Also returns true if the points are colinear.
        /// </summary>
        public static bool RightOrColinearXZ(Vector3 a, Vector3 b, Vector3 p)
        {
            return default;
        }

        /// <summary>
        /// Returns if p lies on the left side of the line a - b.
        /// Uses XZ space. Also returns true if the points are colinear.
        /// </summary>
        public static bool RightOrColinearXZ(Int3 a, Int3 b, Int3 p)
        {
            return default;
        }

        /// <summary>
        /// Returns if the points a in a clockwise order.
        /// Will return true even if the points are colinear or very slightly counter-clockwise
        /// (if the signed area of the triangle formed by the points has an area less than or equals to float.Epsilon)
        /// </summary>
        public static bool IsClockwiseMarginXZ(Vector3 a, Vector3 b, Vector3 c)
        {
            return default;
        }

        /// <summary>Returns if the points a in a clockwise order</summary>
        public static bool IsClockwiseXZ(Vector3 a, Vector3 b, Vector3 c)
        {
            return default;
        }

        /// <summary>Returns if the points a in a clockwise order</summary>
        public static bool IsClockwiseXZ(Int3 a, Int3 b, Int3 c)
        {
            return default;
        }

        /// <summary>Returns if the points a in a clockwise order</summary>
        public static bool IsClockwise(int2 a, int2 b, int2 c)
        {
            return default;
        }

        /// <summary>Returns true if the points a in a clockwise order or if they are colinear</summary>
        public static bool IsClockwiseOrColinearXZ(Int3 a, Int3 b, Int3 c)
        {
            return default;
        }

        /// <summary>Returns true if the points a in a clockwise order or if they are colinear</summary>
        public static bool IsClockwiseOrColinear(Vector2Int a, Vector2Int b, Vector2Int c)
        {
            return default;
        }

        /// <summary>Returns if the points are colinear (lie on a straight line)</summary>
        public static bool IsColinear(Vector3 a, Vector3 b, Vector3 c)
        {
            return default;
        }

        /// <summary>Returns if the points are colinear (lie on a straight line)</summary>
        public static bool IsColinear(Vector2 a, Vector2 b, Vector2 c)
        {
            return default;
        }

        /// <summary>Returns if the points are colinear (lie on a straight line)</summary>
        public static bool IsColinearXZ(Int3 a, Int3 b, Int3 c)
        {
            return default;
        }

        /// <summary>Returns if the points are colinear (lie on a straight line)</summary>
        public static bool IsColinearXZ(Vector3 a, Vector3 b, Vector3 c)
        {
            return default;
        }

        /// <summary>Returns if the points are colinear (lie on a straight line)</summary>
        public static bool IsColinearAlmostXZ(Int3 a, Int3 b, Int3 c)
        {
            return default;
        }

        /// <summary>
        /// Returns if the line segment start2 - end2 intersects the line segment start1 - end1.
        /// If only the endpoints coincide, the result is undefined (may be true or false).
        /// </summary>
        public static bool SegmentsIntersect(Vector2Int start1, Vector2Int end1, Vector2Int start2, Vector2Int end2)
        {
            return default;
        }

        /// <summary>
        /// Returns if the line segment start2 - end2 intersects the line segment start1 - end1.
        /// If only the endpoints coincide, the result is undefined (may be true or false).
        ///
        /// Note: XZ space
        /// </summary>
        public static bool SegmentsIntersectXZ(Int3 start1, Int3 end1, Int3 start2, Int3 end2)
        {
            return default;
        }

        /// <summary>
        /// Returns if the two line segments intersects. The lines are NOT treated as infinite (just for clarification)
        /// See: IntersectionPoint
        /// </summary>
        public static bool SegmentsIntersectXZ(Vector3 start1, Vector3 end1, Vector3 start2, Vector3 end2)
        {
            return default;
        }

        /// <summary>
        /// Calculates the intersection points between a "capsule" (segment expanded by a radius), and a line.
        ///
        /// Returns: (t1, t2), the intersection points on the form lineStart + lineDir*t. Where t2 >= t1. If t2 < t1 then there are no intersections.
        /// </summary>
        /// <param name="capsuleStart">Center of the capsule's first circle</param>
        /// <param name="capsuleDir">Main axis of the capsule. Must be normalized.</param>
        /// <param name="capsuleLength">Distance betwen the capsule's circle centers.</param>
        /// <param name="lineStart">A point on the line</param>
        /// <param name="lineDir">The (normalized) direction of the line.</param>
        /// <param name="radius">The radius of the circle.</param>
        public static float2 CapsuleLineIntersectionFactors(float2 capsuleStart, float2 capsuleDir, float capsuleLength, float2 lineStart, float2 lineDir, float radius)
        {
            return default;
        }

        /// <summary>
        /// Calculates the point start1 + dir1*t where the two infinite lines intersect.
        /// Returns false if the lines are close to parallel.
        /// </summary>
        public static bool LineLineIntersectionFactor(float2 start1, float2 dir1, float2 start2, float2 dir2, out float t)
        {
            t = default(float);
            return default;
        }

        /// <summary>
        /// Calculates the point start1 + dir1*factor1 == start2 + dir2*factor2 where the two infinite lines intersect.
        /// Returns false if the lines are close to parallel.
        /// </summary>
        public static bool LineLineIntersectionFactors(float2 start1, float2 dir1, float2 start2, float2 dir2, out float factor1, out float factor2)
        {
            factor1 = default(float);
            factor2 = default(float);
            return default;
        }

        /// <summary>
        /// Intersection point between two infinite lines.
        /// Note that start points and directions are taken as parameters instead of start and end points.
        /// Lines are treated as infinite. If the lines are parallel 'start1' will be returned.
        /// Intersections are calculated on the XZ plane.
        ///
        /// See: LineIntersectionPointXZ
        /// </summary>
        public static Vector3 LineDirIntersectionPointXZ(Vector3 start1, Vector3 dir1, Vector3 start2, Vector3 dir2)
        {
            return default;
        }

        /// <summary>
        /// Intersection point between two infinite lines.
        /// Note that start points and directions are taken as parameters instead of start and end points.
        /// Lines are treated as infinite. If the lines are parallel 'start1' will be returned.
        /// Intersections are calculated on the XZ plane.
        ///
        /// See: LineIntersectionPointXZ
        /// </summary>
        public static Vector3 LineDirIntersectionPointXZ(Vector3 start1, Vector3 dir1, Vector3 start2, Vector3 dir2, out bool intersects)
        {
            intersects = default(bool);
            return default;
        }

        /// <summary>
        /// Returns if the ray (start1, end1) intersects the segment (start2, end2).
        /// false is returned if the lines are parallel.
        /// Only the XZ coordinates are used.
        /// TODO: Double check that this actually works
        /// </summary>
        public static bool RaySegmentIntersectXZ(Int3 start1, Int3 end1, Int3 start2, Int3 end2)
        {
            return default;
        }

        /// <summary>
        /// Returns the intersection factors for line 1 and line 2. The intersection factors is a distance along the line start - end where the other line intersects it.
        /// <code> intersectionPoint = start1 + factor1 * (end1-start1) </code>
        /// <code> intersectionPoint2 = start2 + factor2 * (end2-start2) </code>
        /// Lines are treated as infinite.
        /// false is returned if the lines are parallel and true if they are not.
        /// Only the XZ coordinates are used.
        /// </summary>
        public static bool LineIntersectionFactorXZ(Int3 start1, Int3 end1, Int3 start2, Int3 end2, out float factor1, out float factor2)
        {
            factor1 = default(float);
            factor2 = default(float);
            return default;
        }

        /// <summary>
        /// Returns the intersection factors for line 1 and line 2. The intersection factors is a distance along the line start - end where the other line intersects it.
        /// <code> intersectionPoint = start1 + factor1 * (end1-start1) </code>
        /// <code> intersectionPoint2 = start2 + factor2 * (end2-start2) </code>
        /// Lines are treated as infinite.
        /// false is returned if the lines are parallel and true if they are not.
        /// Only the XZ coordinates are used.
        /// </summary>
        public static bool LineIntersectionFactorXZ(Vector3 start1, Vector3 end1, Vector3 start2, Vector3 end2, out float factor1, out float factor2)
        {
            factor1 = default(float);
            factor2 = default(float);
            return default;
        }

        /// <summary>
        /// Returns the intersection factor for line 1 with ray 2.
        /// The intersection factors is a factor distance along the line start - end where the other line intersects it.
        /// <code> intersectionPoint = start1 + factor * (end1-start1) </code>
        /// Lines are treated as infinite.
        ///
        /// The second "line" is treated as a ray, meaning only matches on start2 or forwards towards end2 (and beyond) will be returned
        /// If the point lies on the wrong side of the ray start, Nan will be returned.
        ///
        /// NaN is returned if the lines are parallel.
        /// </summary>
        public static float LineRayIntersectionFactorXZ(Int3 start1, Int3 end1, Int3 start2, Int3 end2)
        {
            return default;
        }

        /// <summary>
        /// Returns the intersection factor for line 1 with line 2.
        /// The intersection factor is a distance along the line start1 - end1 where the line start2 - end2 intersects it.
        /// <code> intersectionPoint = start1 + intersectionFactor * (end1-start1) </code>.
        /// Lines are treated as infinite.
        /// -1 is returned if the lines are parallel (note that this is a valid return value if they are not parallel too)
        /// </summary>
        public static float LineIntersectionFactorXZ(Vector3 start1, Vector3 end1, Vector3 start2, Vector3 end2)
        {
            return default;
        }

        /// <summary>Returns the intersection point between the two lines. Lines are treated as infinite. start1 is returned if the lines are parallel</summary>
        public static Vector3 LineIntersectionPointXZ(Vector3 start1, Vector3 end1, Vector3 start2, Vector3 end2)
        {
            return default;
        }

        /// <summary>Returns the intersection point between the two lines. Lines are treated as infinite. start1 is returned if the lines are parallel</summary>
        public static Vector3 LineIntersectionPointXZ(Vector3 start1, Vector3 end1, Vector3 start2, Vector3 end2, out bool intersects)
        {
            intersects = default(bool);
            return default;
        }

        /// <summary>Returns the intersection point between the two lines. Lines are treated as infinite. start1 is returned if the lines are parallel</summary>
        public static Vector2 LineIntersectionPoint(Vector2 start1, Vector2 end1, Vector2 start2, Vector2 end2)
        {
            return default;
        }

        /// <summary>Returns the intersection point between the two lines. Lines are treated as infinite. start1 is returned if the lines are parallel</summary>
        public static Vector2 LineIntersectionPoint(Vector2 start1, Vector2 end1, Vector2 start2, Vector2 end2, out bool intersects)
        {
            intersects = default(bool);
            return default;
        }

        /// <summary>
        /// Returns the intersection point between the two line segments in XZ space.
        /// Lines are NOT treated as infinite. start1 is returned if the line segments do not intersect
        /// The point will be returned along the line [start1, end1] (this matters only for the y coordinate).
        /// </summary>
        public static Vector3 SegmentIntersectionPointXZ(Vector3 start1, Vector3 end1, Vector3 start2, Vector3 end2, out bool intersects)
        {
            intersects = default(bool);
            return default;
        }

        /// <summary>
        /// Does the line segment intersect the bounding box.
        /// The line is NOT treated as infinite.
        /// \author Slightly modified code from http://www.3dkingdoms.com/weekly/weekly.php?a=21
        /// </summary>
        public static bool SegmentIntersectsBounds(Bounds bounds, Vector3 a, Vector3 b)
        {
            return default;
        }

        /// <summary>
        /// Calculates the two intersection points (point + direction*t) on the line where it intersects with a circle at the origin.
        ///
        /// t1 will always be less than or equal to t2 if there are intersections.
        ///
        /// Returns false if there are no intersections.
        /// </summary>
        /// <param name="point">A point on the line</param>
        /// <param name="direction">The normalized direction of the line</param>
        /// <param name="radius">The radius of the circle at the origin.</param>
        /// <param name="t1">The first intersection (if any).</param>
        /// <param name="t2">The second intersection (if any).</param>
        public static bool LineCircleIntersectionFactors(float2 point, float2 direction, float radius, out float t1, out float t2)
        {
            t1 = default(float);
            t2 = default(float);
            return default;
        }

        /// <summary>
        /// Calculates the two intersection points (lerp(point1, point2, t)) on the segment where it intersects with a circle at the origin.
        ///
        /// t1 will always be less than or equal to t2 if there are intersections.
        ///
        /// Returns false if there are no intersections.
        /// </summary>
        /// <param name="point1">Start of the segment</param>
        /// <param name="point2">End of the segment</param>
        /// <param name="radiusSq">The squared radius of the circle at the origin.</param>
        /// <param name="t1">The first intersection (if any). Between 0 and 1.</param>
        /// <param name="t2">The second intersection (if any). Between 0 and 1.</param>
        public static bool SegmentCircleIntersectionFactors(float2 point1, float2 point2, float radiusSq, out float t1, out float t2)
        {
            t1 = default(float);
            t2 = default(float);
            return default;
        }

        /// <summary>
        /// Intersection of a line and a circle.
        /// Returns the greatest t such that segmentStart+t*(segmentEnd-segmentStart) lies on the circle.
        ///
        /// In case the line does not intersect with the circle, the closest point on the line
        /// to the circle will be returned.
        ///
        /// Note: Works for line and sphere in 3D space as well.
        ///
        /// See: http://mathworld.wolfram.com/Circle-LineIntersection.html
        /// See: https://en.wikipedia.org/wiki/Intersection_(Euclidean_geometry)<see cref="A_line_and_a_circle"/>
        /// </summary>
        public static float LineCircleIntersectionFactor(Vector3 circleCenter, Vector3 linePoint1, Vector3 linePoint2, float radius)
        {
            return default;
        }

        /// <summary>
        /// True if the matrix will reverse orientations of faces.
        ///
        /// Scaling by a negative value along an odd number of axes will reverse
        /// the orientation of e.g faces on a mesh. This must be counter adjusted
        /// by for example the recast rasterization system to be able to handle
        /// meshes with negative scales properly.
        ///
        /// We can find out if they are flipped by finding out how the signed
        /// volume of a unit cube is transformed when applying the matrix
        ///
        /// If the (signed) volume turns out to be negative
        /// that also means that the orientation of it has been reversed.
        ///
        /// See: https://en.wikipedia.org/wiki/Normal_(geometry)
        /// See: https://en.wikipedia.org/wiki/Parallelepiped
        /// </summary>
        public static bool ReversesFaceOrientations(Matrix4x4 matrix)
        {
            return default;
        }

        /// <summary>
        /// Normalize vector and also return the magnitude.
        /// This is more efficient than calculating the magnitude and normalizing separately
        /// </summary>
        public static Vector3 Normalize(Vector3 v, out float magnitude)
        {
            magnitude = default(float);
            return default;
        }

        /// <summary>
        /// Normalize vector and also return the magnitude.
        /// This is more efficient than calculating the magnitude and normalizing separately
        /// </summary>
        public static Vector2 Normalize(Vector2 v, out float magnitude)
        {
            magnitude = default(float);
            return default;
        }

        /* Clamp magnitude along the X and Z axes.
		 * The y component will not be changed.
		 */
        public static Vector3 ClampMagnitudeXZ(Vector3 v, float maxMagnitude)
        {
            return default;
        }

        /* Magnitude in the XZ plane */
        public static float MagnitudeXZ(Vector3 v)
        {
            return default;
        }

        /// <summary>
        /// Number of radians that this quaternion rotates around its axis of rotation.
        /// Will be in the range [-PI, PI].
        ///
        /// Note: A quaternion of q and -q represent the same rotation, but their axis of rotation point in opposite directions, so the angle will be different.
        /// </summary>
        public static float QuaternionAngle(quaternion rot)
        {
            return default;
        }
    }

    /// <summary>
    /// Utility functions for working with numbers and strings.
    ///
    /// See: Polygon
    /// See: VectorMath
    /// </summary>
    public static class AstarMath
    {
        static Unity.Mathematics.Random GlobalRandom = Unity.Mathematics.Random.CreateFromIndex(0);
        static object GlobalRandomLock = new object();

        public static float ThreadSafeRandomFloat()
        {
            return default;
        }

        public static float2 ThreadSafeRandomFloat2()
        {
            return default;
        }

        /// <summary>Converts a non-negative float to a long, saturating at long.MaxValue if the value is too large</summary>
        public static long SaturatingConvertFloatToLong(float v) => v > (float)long.MaxValue ? long.MaxValue : (long)v;

        /// <summary>Maps a value between startMin and startMax to be between targetMin and targetMax</summary>
        public static float MapTo(float startMin, float startMax, float targetMin, float targetMax, float value)
        {
            return default;
        }

        /// <summary>
        /// Returns bit number b from int a. The bit number is zero based. Relevant b values are from 0 to 31.
        /// Equals to (a >> b) & 1
        /// </summary>
        static int Bit(int a, int b)
        {
            return default;
        }

        /// <summary>
        /// Returns a nice color from int i with alpha a. Got code from the open-source Recast project, works really well.
        /// Seems like there are only 64 possible colors from studying the code
        /// </summary>
        public static Color IntToColor(int i, float a)
        {
            return default;
        }

        /// <summary>
        /// Converts an HSV color to an RGB color.
        /// According to the algorithm described at http://en.wikipedia.org/wiki/HSL_and_HSV
        ///
        /// @author Wikipedia
        /// @return the RGB representation of the color.
        /// </summary>
        public static Color HSVToRGB(float h, float s, float v)
        {
            return default;
        }

        /// <summary>
        /// Calculates the shortest difference between two given angles given in radians.
        ///
        /// The return value will be between -pi/2 and +pi/2.
        /// </summary>
        public static float DeltaAngle(float angle1, float angle2)
        {
            return default;
        }
    }

    /// <summary>
    /// Utility functions for working with polygons, lines, and other vector math.
    /// All functions which accepts Vector3s but work in 2D space uses the XZ space if nothing else is said.
    ///
    /// Version: A lot of functions in this class have been moved to the VectorMath class
    /// the names have changed slightly and everything now consistently assumes a left handed
    /// coordinate system now instead of sometimes using a left handed one and sometimes
    /// using a right handed one. This is why the 'Left' methods redirect to methods
    /// named 'Right'. The functionality is exactly the same.
    /// </summary>
    [BurstCompile]
    public static class Polygon
    {
        /// <summary>
        /// Returns if the triangle ABC contains the point p in XZ space.
        /// The triangle vertices are assumed to be laid out in clockwise order.
        /// </summary>
        public static bool ContainsPointXZ(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
        {
            return default;
        }

        /// <summary>
        /// Returns if the triangle ABC contains the point p.
        /// The triangle vertices are assumed to be laid out in clockwise order.
        /// </summary>
        public static bool ContainsPointXZ(Int3 a, Int3 b, Int3 c, Int3 p)
        {
            return default;
        }

        /// <summary>
        /// Returns if the triangle ABC contains the point p.
        /// The triangle vertices are assumed to be laid out in clockwise order.
        /// </summary>
        public static bool ContainsPoint(Vector2Int a, Vector2Int b, Vector2Int c, Vector2Int p)
        {
            return default;
        }

        /// <summary>
        /// Checks if p is inside the polygon.
        /// \author http://unifycommunity.com/wiki/index.php?title=PolyContainsPoint (Eric5h5)
        /// </summary>
        public static bool ContainsPoint(Vector2[] polyPoints, Vector2 p)
        {
            return default;
        }

        /// <summary>
        /// Checks if p is inside the polygon (XZ space).
        /// \author http://unifycommunity.com/wiki/index.php?title=PolyContainsPoint (Eric5h5)
        /// </summary>
        public static bool ContainsPointXZ(Vector3[] polyPoints, Vector3 p)
        {
            return default;
        }

        /// <summary>
        /// Returns if the triangle contains the point p when projected on the movement plane.
        /// The triangle vertices may be clockwise or counter-clockwise.
        ///
        /// This method is numerically robust, as in, if the point is contained in exactly one of two adjacent triangles, then this
        /// function will return true for at least one of them (both if the point is exactly on the edge between them).
        /// If it was less numerically robust, it could conceivably return false for both of them if the point was on the edge between them, which would be bad.
        /// </summary>
        [BurstCompile]
        public static bool ContainsPoint(ref int3 aWorld, ref int3 bWorld, ref int3 cWorld, ref int3 pWorld, ref NativeMovementPlane movementPlane)
        {
            return default;
        }

        /// <summary>
        /// Returns if the triangle contains the point p when projected on a plane using the given projection.
        /// The triangle vertices may be clockwise or counter-clockwise.
        ///
        /// This method is numerically robust, as in, if the point is contained in exactly one of two adjacent triangles, then this
        /// function will return true for at least one of them (both if the point is exactly on the edge between them).
        /// If it was less numerically robust, it could conceivably return false for both of them if the point was on the edge between them, which would be bad.
        /// </summary>
        public static bool ContainsPoint(ref int3 aWorld, ref int3 bWorld, ref int3 cWorld, ref int3 pWorld, in float2x3 planeProjection)
        {
            return default;
        }

        public struct BarycentricTriangleInterpolator
        {
            int2 origin;
            double2x2 barycentricMapping;
            double3 thresholds, linear1, linear2, linear3, ys;

            public BarycentricTriangleInterpolator(Int3 p1, Int3 p2, Int3 p3) : this()
            {
            }

            public int SampleY(int2 p)
            {
                return default;
            }
        }

        /// <summary>
        /// Calculates convex hull in XZ space for the points.
        /// Implemented using the very simple Gift Wrapping Algorithm
        /// which has a complexity of O(nh) where n is the number of points and h is the number of points on the hull,
        /// so it is in the worst case quadratic.
        /// </summary>
        public static Vector3[] ConvexHullXZ(Vector3[] points)
        {
            return default;
        }

        /// <summary>
        /// Closest point on the triangle abc to the point p.
        /// See: 'Real Time Collision Detection' by Christer Ericson, chapter 5.1, page 141
        /// </summary>
        public static Vector2 ClosestPointOnTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
        {
            return default;
        }

        /// <summary>
        /// Closest point on the triangle abc to the point p when seen from above.
        /// See: 'Real Time Collision Detection' by Christer Ericson, chapter 5.1, page 141
        /// </summary>
        public static Vector3 ClosestPointOnTriangleXZ(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
        {
            return default;
        }

        /// <summary>
        /// Closest point on the triangle abc to the point p.
        /// See: 'Real Time Collision Detection' by Christer Ericson, chapter 5.1, page 141
        /// </summary>
        public static float3 ClosestPointOnTriangle(float3 a, float3 b, float3 c, float3 p)
        {
            return default;
        }

        /// <summary>
        /// Closest point on the triangle abc to the point p.
        ///
        /// Takes arguments by reference to be able to be burst-compiled.
        ///
        /// See: 'Real Time Collision Detection' by Christer Ericson, chapter 5.1, page 141
        ///
        /// Returns: True if the point is inside the triangle, false otherwise, after the point has been projected on the plane that the triangle is in.
        /// </summary>
        [BurstCompile]
        public static bool ClosestPointOnTriangleByRef(in float3 a, in float3 b, in float3 c, in float3 p, [NoAlias] out float3 output)
        {
            output = default(float3);
            return default;
        }

        /// <summary>
        /// Closest point on the triangle abc to the point p as barycentric coordinates.
        ///
        /// See: 'Real Time Collision Detection' by Christer Ericson, chapter 5.1, page 141
        /// </summary>
        public static float3 ClosestPointOnTriangleBarycentric(float2 a, float2 b, float2 c, float2 p)
        {
            return default;
        }

        /// <summary>
        /// Closest point on the triangle abc to the point p as barycentric coordinates.
        ///
        /// See: 'Real Time Collision Detection' by Christer Ericson, chapter 5.1, page 141
        /// </summary>
        public static float3 ClosestPointOnTriangleBarycentric(float3 a, float3 b, float3 c, float3 p)
        {
            return default;
        }

        /// <summary>
        /// Closest point on a triangle when one axis is scaled.
        ///
        /// Project the triangle onto the plane defined by the projection axis.
        /// Then find the closest point on the triangle in the plane.
        /// Calculate the distance to the closest point in the plane, call that D1.
        /// Convert the closest point into 3D space, and calculate the distance to the
        /// query point along the plane's normal, call that D2.
        /// The final cost for a given point is D1 + D2 * distanceScaleAlongProjectionDirection.
        ///
        /// This will form a diamond shape of equivalent cost points around the query point (x).
        /// The ratio of the width of this diamond to the height is equal to distanceScaleAlongProjectionDirection.
        ///
        ///     ^
        ///    / \
        ///   /   \
        ///  /  x  \
        ///  \	  /
        ///   \   /
        ///    \ /
        ///     v
        ///
        /// See: <see cref="DistanceMetric.ClosestAsSeenFromAboveSoft(Vector3)"/>
        /// </summary>
        /// <param name="vi1">First vertex of the triangle, in graph space.</param>
        /// <param name="vi2">Second vertex of the triangle, in graph space.</param>
        /// <param name="vi3">Third vertex of the triangle, in graph space.</param>
        /// <param name="projection">Projection parameters that are for example constructed from a movement plane.</param>
        /// <param name="point">Point to find the closest point to.</param>
        /// <param name="closest">Closest point on the triangle to the point.</param>
        /// <param name="sqrDist">Squared cost from the point to the closest point on the triangle.</param>
        /// <param name="distAlongProjection">Distance from the point to the closest point on the triangle along the projection axis.</param>
        [BurstCompile]
        public static void ClosestPointOnTriangleProjected(ref Int3 vi1, ref Int3 vi2, ref Int3 vi3, ref BBTree.ProjectionParams projection, ref float3 point, [NoAlias] out float3 closest, [NoAlias] out float sqrDist, [NoAlias] out float distAlongProjection)
        {
            closest = default(float3);
            sqrDist = default(float);
            distAlongProjection = default(float);
        }

        /// <summary>Cached dictionary to avoid excessive allocations</summary>
        static readonly Dictionary<Int3, int> cached_Int3_int_dict = new Dictionary<Int3, int>();

        /// <summary>
        /// Compress the mesh by removing duplicate vertices.
        ///
        /// Vertices that differ by only 1 along the y coordinate will also be merged together.
        /// Warning: This function is not threadsafe. It uses some cached structures to reduce allocations.
        /// </summary>
        /// <param name="vertices">Vertices of the input mesh</param>
        /// <param name="triangles">Triangles of the input mesh</param>
        /// <param name="tags">Tags of the input mesh. One for each triangle.</param>
        /// <param name="outVertices">Vertices of the output mesh.</param>
        /// <param name="outTriangles">Triangles of the output mesh.</param>
        /// <param name="outTags">Tags of the output mesh. One for each triangle.</param>
        public static void CompressMesh(List<Int3> vertices, List<int> triangles, List<uint> tags, out Int3[] outVertices, out int[] outTriangles, out uint[] outTags)
        {
            outVertices = default(Int3[]);
            outTriangles = default(int[]);
            outTags = default(uint[]);
        }

        /// <summary>
        /// Given a set of edges between vertices, follows those edges and returns them as chains and cycles.
        ///
        /// [Open online documentation to see images]
        /// </summary>
        /// <param name="outline">outline[a] = b if there is an edge from a to b.</param>
        /// <param name="hasInEdge">hasInEdge should contain b if outline[a] = b for any key a.</param>
        /// <param name="results">Will be called once for each contour with the contour as a parameter as well as a boolean indicating if the contour is a cycle or a chain (see image).</param>
        public static void TraceContours(Dictionary<int, int> outline, HashSet<int> hasInEdge, System.Action<List<int>, bool> results)
        {
        }

        /// <summary>Divides each segment in the list into subSegments segments and fills the result list with the new points</summary>
        public static void Subdivide(List<Vector3> points, List<Vector3> result, int subSegments)
        {
        }
    }
}
