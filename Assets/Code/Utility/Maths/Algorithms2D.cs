using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Awaken.TG.Utility {
    [BurstCompile]
    public static class Algorithms2D {
        const float LineDenominatorEpsilon = 1e-5f;

        public static float cross(float2 a, float2 b) {
            return a.x * b.y - b.x * a.y;
        }

        // From https://www.habrador.com/tutorials/math/8-convex-hull/
        /// <summary>
        /// Create convex hull. Attention this can modify inputted list
        /// </summary>
        /// <param name="points"></param>
        /// <returns>If less points than 3 returns null, else if equals 3 returns input otherwise return new convex hull</returns>
        public static List<Vector2> GetConvexHull(List<Vector2> points) {
            //If we have just 3 points, then they are the convex hull, so return those
            if (points.Count == 3) {
                //These might not be ccw, and they may also be colinear
                return points;
            }

            //If fewer points, then we cant create a convex hull
            if (points.Count < 3) {
                return null;
            }


            //The list with points on the convex hull
            List<Vector2> convexHull = new List<Vector2>();

            //Step 1. Find the vertex with the smallest x coordinate
            //If several have the same x coordinate, find the one with the smallest y
            Vector2 startVertex = points[0];

            Vector2 startPos = startVertex;

            for (int i = 1; i < points.Count; i++) {
                var testPos = points[i];

                //Because of precision issues, we use Mathf.Approximately to test if the x positions are the same
                if (testPos.x < startPos.x || (Mathf.Approximately(testPos.x, startPos.x) && testPos.y < startPos.y)) {
                    startVertex = points[i];

                    startPos = startVertex;
                }
            }

            //This vertex is always on the convex hull
            convexHull.Add(startVertex);

            points.Remove(startVertex);


            //Step 2. Loop to generate the convex hull
            Vector2 currentPoint = convexHull[0];

            //Store collinear points here - better to create this list once than each loop
            List<Vector2> collinearPoints = new List<Vector2>();

            int counter = 0;

            while (true) {
                //After 2 iterations we have to add the start position again so we can terminate the algorithm
                //Cant use convexhull.count because of colinear points, so we need a counter
                if (counter == 2) {
                    points.Add(convexHull[0]);
                }

                //Pick next point randomly
                var nextPoint = points[Random.Range(0, points.Count)];

                //Test if there's a point to the right of ab, if so then it's the new b
                for (int i = 0; i < points.Count; i++) {
                    //Dont test the point we picked randomly
                    if (points[i].Equals(nextPoint)) {
                        continue;
                    }

                    Vector2 c = points[i];

                    float relation = PointToLineRelation(currentPoint, nextPoint, c);

                    //Collinear points
                    //Cant use exactly 0 because of floating point precision issues
                    //This accuracy is smallest possible, if smaller points will be missed if we are testing with a plane
                    float accuracy = 0.004f;

                    if (relation < accuracy && relation > -accuracy) {
                        collinearPoints.Add(points[i]);
                    }
                    //To the right = better point, so pick it as next point on the convex hull
                    else if (relation < accuracy) {
                        nextPoint = points[i];
                        
                        //Clear collinear points
                        collinearPoints.Clear();
                    }
                    //To the left = worse point so do nothing
                }

                //If we have collinear points
                if (collinearPoints.Count > 0) {
                    collinearPoints.Add(nextPoint);

                    //Sort this list, so we can add the colinear points in correct order
                    collinearPoints = collinearPoints.OrderBy(n => Vector2.SqrMagnitude(n - currentPoint)).ToList();

                    convexHull.Add(collinearPoints.Last());

                    currentPoint = collinearPoints[collinearPoints.Count - 1];

                    //Remove the points that are now on the convex hull
                    for (int i = 0; i < collinearPoints.Count; i++) {
                        points.Remove(collinearPoints[i]);
                    }

                    collinearPoints.Clear();
                } else {
                    convexHull.Add(nextPoint);

                    points.Remove(nextPoint);

                    currentPoint = nextPoint;
                }

                //Have we found the first point on the hull? If so we have completed the hull
                if (currentPoint.Equals(convexHull[0])) {
                    //Then remove it because it is the same as the first point, and we want a convex hull with no duplicates
                    convexHull.RemoveAt(convexHull.Count - 1);

                    break;
                }

                counter += 1;
            }

            return convexHull;
        }

        /// <summary>
        /// Probe relation between line and point
        /// </summary>
        /// <param name="linePoint1">Point determining line</param>
        /// <param name="linePoint2">Point determining line</param>
        /// <param name="point">Probing point</param>
        /// <returns> Relation between line and point expressed as float with value:
        /// &lt; 0 -&gt; to the right
        /// = 0 -> on the line
        /// &gt; 0 -&gt; to the left
        /// </returns>
        public static float PointToLineRelation(in Vector2 linePoint1, in Vector2 linePoint2, in Vector2 point) {
            float determinant = (linePoint1.x - point.x) * (linePoint2.y - point.y) - (linePoint1.y - point.y) * (linePoint2.x - point.x);
            return determinant;
        }

        [BurstCompile]
        public static bool InsideOfTriangle(in Vector2 v0, in Vector2 v1, in Vector2 v2, in Vector2 p) {
            var relation0 = PointToLineRelation(v0, v1, p);
            var relation1 = PointToLineRelation(v1, v2, p);
            var relation2 = PointToLineRelation(v2, v0, p);
            
            var nonNegative = relation0 >= 0 & relation1 >= 0 & relation2 >= 0;
            var nonPositive = relation0 <= 0 & relation1 <= 0 & relation2 <= 0;
            return nonNegative | nonPositive;
        }
        
        // FROM https://en.wikipedia.org/wiki/Centroid#Of_a_polygon
        /// <summary>
        /// Calculate centroid and area (unsigned) of n-polygon
        /// </summary>
        /// <param name="convexPoints">Vertex points of n-polygon. Must be in order.</param>
        public static void CentroidAndArea(UnsafeArray<float2> convexPoints, out float2 centroid, out float area) {
            float ASum = 0;
            float xSum = 0;
            float ySum = 0;
            for (var i = 0u; i < convexPoints.Length - 1; i++) {
                var acSum = convexPoints[i].x * convexPoints[i + 1].y - convexPoints[i + 1].x * convexPoints[i].y;
                ASum += acSum;
                
                var xcSum = convexPoints[i].x + convexPoints[i + 1].x;
                xcSum *= acSum;
                xSum += xcSum;
                
                var ycSum = convexPoints[i].y + convexPoints[i + 1].y;
                ycSum *= acSum;
                ySum += ycSum;
            }

            var lastIndex = convexPoints.Length - 1;
            {
                var acSum = convexPoints[lastIndex].x * convexPoints[0].y - convexPoints[0].x * convexPoints[lastIndex].y;
                ASum += acSum;
                
                var xcSum = convexPoints[lastIndex].x + convexPoints[0].x;
                xcSum *= acSum;
                xSum += xcSum;
                
                var ycSum = convexPoints[lastIndex].y + convexPoints[0].y;
                ycSum *= acSum;
                ySum += ycSum;
            }
            
            ASum *= 0.5f;
            var multiplier = 1f / (6 * ASum);
            area = math.abs(ASum);

            centroid.x = multiplier * xSum;
            centroid.y = multiplier * ySum;
        }

        // FROM: https://gist.github.com/sinbad/68cb88e980eeaed0505210d052573724
        public static void LineSegmentsIntersection(in LineSegment2D lineSegment1, in LineSegment2D lineSegment2,
            out float2 intersectionPoint, out bool intersects) {
            intersects = false;
            intersectionPoint = float2.zero;

            var r = lineSegment1.end - lineSegment1.start;
            var s = lineSegment2.end - lineSegment2.start;
            var qMinusP = lineSegment2.start - lineSegment1.start;

            float crossRS = cross(r, s);

            if (Approximately(crossRS, 0f)) { // Parallel lines
                if (Approximately(cross(qMinusP, r), 0f)) { // Co-linear lines, could overlap
                    float rDotR = math.dot(r, r);
                    float sDotR = math.dot(s, r);
                    float t0 = math.dot(qMinusP, r / rDotR);
                    float t1 = t0 + sDotR / rDotR;
                    if (sDotR < 0) {
                        // lines were facing in different directions so t1 > t0, swap to simplify check
                        (t0, t1) = (t1, t0);
                    }

                    if (t0 <= 1 && t1 >= 0) { // Overlapping
                        // Nice half-way point intersection
                        float t = math.lerp(math.max(0, t0), math.min(1, t1), 0.5f);
                        intersectionPoint = lineSegment1.start + t * r;
                        intersects = true;
                    }
                }
            } else {
                // Not parallel, calculate t and u
                float t = cross(qMinusP, s) / crossRS;
                float u = cross(qMinusP, r) / crossRS;
                if ((t >= 0 & t <= 1) & (u >= 0 & u <= 1)) {
                    intersectionPoint = lineSegment1.start + t * r;
                    intersects = true;
                }
            }

            static bool Approximately(float a, float b) {
                return math.abs(a - b) <= LineDenominatorEpsilon;
            }
        }

        public static void LinesIntersection(in Line2D line1, in Line2D line2, out float2 intersectionPoint) {
            var r = line1.point2 - line1.point1;
            var s = line2.point2 - line2.point1;
            var qMinusP = line2.point1 - line1.point1;

            float crossRS = cross(r, s);

            intersectionPoint = float2.zero;

            if (Approximately(crossRS, 0f)) { // Parallel lines
                if (Approximately(cross(qMinusP, r), 0f)) { // Co-linear lines
                    float rDotR = math.dot(r, r);
                    float sDotR = math.dot(s, r);
                    float t0 = math.dot(qMinusP, r / rDotR);
                    float t1 = t0 + sDotR / rDotR;
                    if (sDotR < 0) {
                        // lines were facing in different directions so t1 > t0, swap to simplify check
                        (t0, t1) = (t1, t0);
                    }

                    // Nice half-way point intersection
                    float t = math.lerp(math.max(0, t0), math.min(1, t1), 0.5f);
                    intersectionPoint = line1.point1 + t * r;
                }
            } else { // Not parallel
                float t = cross(qMinusP, s) / crossRS;
                intersectionPoint = line1.point1 + t * r;
            }

            static bool Approximately(float a, float b) {
                return math.abs(a - b) <= LineDenominatorEpsilon;
            }
        }

        /// <summary>
        /// Produces coefficients A, B, C of line equation by two points provided
        /// </summary>
        public static void LineCoefficients(in LineSegment2D lineSegment, out Line2DCoefficients coefficients) {
            LineCoefficients(lineSegment.start, lineSegment.end, out coefficients);
        }

        /// <summary>
        /// Produces coefficients A, B, C of line equation by two points provided
        /// </summary>
        public static void LineCoefficients(in Line2D line, out Line2DCoefficients coefficients) {
            LineCoefficients(line.point1, line.point2, out coefficients);
        }

        /// <summary>
        /// Produces coefficients A, B, C of line equation by two points provided
        /// </summary>
        public static void LineCoefficients(in float2 p1, in float2 p2, out Line2DCoefficients coefficients) {
            coefficients = new Line2DCoefficients(
                p1.y - p2.y,
                p2.x - p1.x,
                p1.x * p2.y - p2.x * p1.y
                );
        }
        
        // FROM https://www.codeproject.com/Articles/18936/A-C-Implementation-of-Douglas-Peucker-Line-Appro
        public static void DouglasPeuckerReduction(List<Vector2> points, int firstPoint, int lastPoint,
            float tolerance, List<int> pointIndexesToKeep) {
            float maxDistance = 0;
            int indexFarthest = 0;

            for (int index = firstPoint; index < lastPoint; index++) {
                float distance = PerpendicularDistance(points[firstPoint], points[lastPoint], points[index]);
                if (distance > maxDistance) {
                    maxDistance = distance;
                    indexFarthest = index;
                }
            }

            if (maxDistance > tolerance && indexFarthest != 0) {
                pointIndexesToKeep.Add(indexFarthest);

                DouglasPeuckerReduction(points, firstPoint, indexFarthest, tolerance, pointIndexesToKeep);
                DouglasPeuckerReduction(points, indexFarthest, lastPoint, tolerance, pointIndexesToKeep);
            }
        }
        
        /// <param name="maxDensity">Maximum points per unit of space in which segments layes</param>
        [BurstCompile]
        public static void RandomPointsOnSegments(in UnsafeArray<LineSegment2D> segments, uint wantedPointCount, float maxDensity, ref Unity.Mathematics.Random random, Allocator allocator, out UnsafeArray<float2> points) {
            var totalLineSegmentLength = 0f;
            var segmentCount = segments.Length;
            
            for (var i = 0u; i < segmentCount; i++) {
                totalLineSegmentLength += math.distance(segments[i].start, segments[i].end);
            }
            wantedPointCount = (uint) math.min(wantedPointCount, totalLineSegmentLength * maxDensity);
            
            points = new UnsafeArray<float2>(wantedPointCount, allocator);
            
            for (var i = 0u; i < wantedPointCount; i++) {
                var randomLength = random.NextFloat(0, totalLineSegmentLength);
                var currentLength = 0f;
                
                for (var j = 0u; j < segmentCount; j++) {
                    var lineSegmentLength = math.distance(segments[j].start, segments[j].end);
                    if (currentLength + lineSegmentLength >= randomLength) {
                        var t = (randomLength - currentLength) / lineSegmentLength;
                        points[i] = math.lerp(segments[j].start, segments[j].end, t);
                        break;
                    }

                    currentLength += lineSegmentLength;
                }
            }
        }

        public static void DistanceToLineSegmentSq(in float2 point, in LineSegment2D lineSegment, out float distanceSq) {
            var distanceToLinePointX = point.x - lineSegment.start.x;
            var distanceToLinePointY = point.y - lineSegment.start.y;
            var lineLengthX = lineSegment.end.x - lineSegment.start.x;
            var lineLengthY = lineSegment.end.y - lineSegment.start.y;

            var dot = distanceToLinePointX * lineLengthX + distanceToLinePointY * lineLengthY;
            var rcpLineLength = math.rcp(math.square(lineLengthX) + math.square(lineLengthY));
            var param = dot * rcpLineLength;

            float2 closestPointOnSegment;

            if (param < 0) {
                closestPointOnSegment.x = lineSegment.start.x;
                closestPointOnSegment.y = lineSegment.start.y;
            } else if (param > 1) {
                closestPointOnSegment.x = lineSegment.end.x;
                closestPointOnSegment.y = lineSegment.end.y;
            } else {
                closestPointOnSegment.x = lineSegment.start.x + param * lineLengthX;
                closestPointOnSegment.y = lineSegment.start.y + param * lineLengthY;
            }

            var dx = point.x - closestPointOnSegment.x;
            var dy = point.y - closestPointOnSegment.y;
            distanceSq = math.square(dx) + math.square(dy);
        }

        public static void DistanceToLineSegment(in float2 point, in LineSegment2D lineSegment, out float distance) {
            DistanceToLineSegmentSq(point, lineSegment, out var distanceSq);
            distance = math.sqrt(distanceSq);
        }

        static float PerpendicularDistance(Vector2 point1, Vector2 point2, Vector2 point3) {
            float area = 0.5f *  Mathf.Abs((point1.x * point2.y + point2.x *
                point3.y + point3.x * point1.y - point2.x * point1.y - point3.x *
                point2.y - point1.x * point3.y));
            float bottom = Mathf.Sqrt(Mathf.Pow(point1.x - point2.x, 2) + Mathf.Pow(point1.y - point2.y, 2));
            float height = area / bottom * 2;

            return height;
        }
        
        // Gift wrapping algorithm
        public static NativeList<float2> GetPolygon2dConvexHull(NativeArray<float2> polygonPoints, Allocator allocator) {
            int pointsCount = polygonPoints.Length;
            var hullPoints = new NativeList<float2>(pointsCount, allocator);

            if (polygonPoints.Length <= 3) {
                hullPoints.CopyFrom(polygonPoints);
                return hullPoints;
            }
            polygonPoints.Sort(new LeftMostPointComparer());
            var start = polygonPoints[0];
            var current = start;

            do {
                hullPoints.Add(current);
                var next = polygonPoints[0];

                for (int i = 0; i < pointsCount; i++) {
                    var point = polygonPoints[i];
                    if (point.Equals(current)) {
                        continue;
                    }

                    float cross = CrossProduct(current, next, point);
                    if (next.Equals(current) || cross > 0 || (cross == 0 && math.distance(current, point) > math.distance(current, next))) {
                        next = point;
                    }
                }
                    
                current = next;
            } while (current.Equals(start) == false);

            return hullPoints;
                
            static float CrossProduct(float2 p1, float2 p2, float2 p3) {
                return (p2.x - p1.x) * (p3.y - p2.y) - (p2.y - p1.y) * (p3.x - p2.x);
            }
        }

        public static float2 GetCentroid(NativeArray<float2> points) {
            int pointsCount = points.Length;
            if (pointsCount == 0) {
                throw new ArgumentException("The points array must contain at least one point.");
            }

            var centroid = new float2(0, 0);
            for (int i = 0; i < pointsCount; i++) {
                centroid += points[i];
            }

            centroid /= pointsCount;
            return centroid;
        }
        
        class LeftMostPointComparer : IComparer<float2> {
            public int Compare(float2 a, float2 b) {
                int result = a.x.CompareTo(b.x);
                if (result == 0) {
                    result = a.y.CompareTo(b.y);
                }

                return result;
            }
        }

        // Rect distribution
        /// <summary>
        /// Ensures that none of the rectangles overlaps with minimal distance between them
        /// </summary>
        public static void DistributeRects(Span<Rect> rects) {
            var rectCount = rects.Length;
            if (rectCount < 2) {
                return;
            }

            Span<float2> velocities = stackalloc float2[rectCount];

            var anyOverlap = true;
            do {
                anyOverlap = false;

                for (int i = 0; i < rectCount - 1; i++) {
                    var rectA = rects[i];
                    for (int j = i + 1; j < rectCount; j++) {
                        var rectB = rects[j];
                        if (rectA.Overlaps(rectB)) {
                            anyOverlap = true;
                            var delta = rectA.center - rectB.center;
                            var distance = math.length(delta);
                            if (distance > 0.5f) {
                                var overlapRect = Rect.MinMaxRect(
                                    math.max(rectA.xMin, rectB.xMin),
                                    math.max(rectA.yMin, rectB.yMin),
                                    math.min(rectA.xMax, rectB.xMax),
                                    math.min(rectA.yMax, rectB.yMax));
                                var overlapArea = overlapRect.width * overlapRect.height;
                                var overlapDistance = math.sqrt(overlapArea);
                                var overlapDirection = (float2)(delta / distance * overlapDistance * 0.51f);
                                velocities[i] += overlapDirection;
                                velocities[j] -= overlapDirection;
                            } else {
                                var rng = new Unity.Mathematics.Random(math.hash(rectA.position + rectA.min + rectA.max + rectB.min + rectB.max));
                                var direction = rng.NextFloat2Direction();
                                velocities[i] += direction;
                                velocities[j] -= direction;
                            }
                        }
                    }
                }

                if (anyOverlap) {
                    for (int i = 0; i < rectCount; i++) {
                        ref var rect = ref rects[i];
                        var velocity = velocities[i];
                        rect.position += (Vector2)velocity;
                        velocities[i] = float2.zero;
                    }
                }
            }
            while (anyOverlap);

        }
    }
}