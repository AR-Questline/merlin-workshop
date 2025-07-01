using System;
using Awaken.TG.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Awaken.Utility.Maths {
    [BurstCompile]
    public static class Polygon2DUtils {
        [BurstCompile]
        public static void IsInPolygon(in float2 p, in Polygon2D polygon, out bool inside) {
            inside = false;

            if (polygon.Length < 3) {
                return;
            }

            var lastLength = polygon.Length - 1;
            var oldPoint = new float2(polygon[lastLength].x, polygon[lastLength].y);

            for (var i = 0u; i < polygon.Length; i++) {
                var newPoint = new float2(polygon[i].x, polygon[i].y);

                float2 p1;
                float2 p2;
                if (newPoint.x > oldPoint.x) {
                    p1 = oldPoint;
                    p2 = newPoint;
                } else {
                    p1 = newPoint;
                    p2 = oldPoint;
                }

                if (newPoint.x < p.x == p.x <= oldPoint.x &&
                    (p.y - (long)p1.y) * (p2.x - p1.x) < (p2.y - (long)p1.y) * (p.x - p1.x)) {
                    inside = !inside;
                }

                oldPoint = newPoint;
            }
        }

        [BurstCompile]
        public static void Distance(in float2 p, in UnsafeArray<float2> polygonPoints, out float distance) {
            if (polygonPoints.Length == 0) {
                distance = float.MaxValue;
            } else if (polygonPoints.Length == 1) {
                distance = math.distance(p, polygonPoints[0]);
            } else if (polygonPoints.Length == 2) {
                var polygonLine = new LineSegment2D(polygonPoints[0], polygonPoints[1]);
                Algorithms2D.DistanceToLineSegment(p, polygonLine, out distance);
            } else {
                var minDistanceSq = float.MaxValue;

                var lastLength = polygonPoints.Length - 1;
                var oldPoint = new float2(polygonPoints[lastLength].x, polygonPoints[lastLength].y);

                for (var i = 0u; i < polygonPoints.Length; i++) {
                    var newPoint = new float2(polygonPoints[i].x, polygonPoints[i].y);
                    var polygonLine = new LineSegment2D(oldPoint, newPoint);

                    Algorithms2D.DistanceToLineSegmentSq(p, polygonLine, out var distanceSq);
                    minDistanceSq = math.min(minDistanceSq, distanceSq);

                    oldPoint = newPoint;
                }

                distance = math.sqrt(minDistanceSq);
            }
        }

        [BurstCompile]
        public static void Intersects(in MinMaxAABR aabr, in Polygon2D polygon, out bool intersects) {
            if (!aabr.Overlaps(polygon.bounds)) {
                intersects = false;
                return;
            }

            if (polygon.Length == 0) {
                intersects = false;
            } else if (polygon.Length == 1) {
                intersects = aabr.Contains(polygon[0]);

            } else if (polygon.Length == 2) {
                intersects = aabr.Contains(polygon[0]) | aabr.Contains(polygon[1]);
                if (intersects) {
                    return;
                }

                Span<float2> corners = stackalloc float2[4];
                aabr.FillCorners(ref corners);
                Span<LineSegment2D> aabrLines = stackalloc LineSegment2D[4];
                SegmentsAABR(corners, ref aabrLines);
                var polygonLine = new LineSegment2D(polygon[0], polygon[1]);
                Algorithms2D.LineSegmentsIntersection(polygonLine, aabrLines[0], out _, out var intersects1);
                Algorithms2D.LineSegmentsIntersection(polygonLine, aabrLines[1], out _, out var intersects2);
                Algorithms2D.LineSegmentsIntersection(polygonLine, aabrLines[2], out _, out var intersects3);
                Algorithms2D.LineSegmentsIntersection(polygonLine, aabrLines[3], out _, out var intersects4);
                intersects = intersects1 | intersects2 | intersects3 | intersects4;
            } else {
                Span<float2> corners = stackalloc float2[4];
                aabr.FillCorners(ref corners);

                // AABR point inside polygon
                for (var i = 0; i < 4; i++) {
                    Polygon2DUtils.IsInPolygon(corners[i], polygon, out intersects);
                    if (intersects) {
                        return;
                    }
                }

                // Polygon point inside AABR
                for (var i = 0u; i < polygon.Length; i++) {
                    if (aabr.Contains(polygon[i])) {
                        intersects = true;
                        return;
                    }
                }

                // AABR and Polygon intersection
                Span<LineSegment2D> aabrLines = stackalloc LineSegment2D[4];
                SegmentsAABR(corners, ref aabrLines);

                var lastIndex = polygon.Length - 1;
                var oldPoint = polygon.points[lastIndex];

                for (var i = 0u; i < polygon.Length; i++) {
                    var newPoint = polygon.points[i];
                    var line = new LineSegment2D(oldPoint, newPoint);

                    for (var j = 0; j < 4; j++) {
                        Algorithms2D.LineSegmentsIntersection(aabrLines[j], line, out _, out intersects);
                        if (intersects) {
                            return;
                        }
                    }

                    oldPoint = newPoint;
                }

                intersects = false;
            }

            void SegmentsAABR(in Span<float2> corners, ref Span<LineSegment2D> lines) {
                lines[0] = new LineSegment2D(corners[0], corners[1]);
                lines[1] = new LineSegment2D(corners[1], corners[2]);
                lines[2] = new LineSegment2D(corners[2], corners[3]);
                lines[3] = new LineSegment2D(corners[3], corners[0]);
            }
        }

        [BurstCompile]
        public static void Bounds(in UnsafeArray<float2> polygonPoints, out MinMaxAABR bounds) {
            bounds = MinMaxAABR.Empty;

            for (var i = 0u; i < polygonPoints.Length; i++) {
                bounds.Encapsulate(polygonPoints[i]);
            }
        }

        /// <remarks>Naive implementation, which works for common/easy cases but will fail for complex/edge cases</remarks>
        [BurstCompile]
        public static void Inflate(in UnsafeArray<float2> polygonPoints, float offset, Allocator allocator, out UnsafeArray<float2> result) {
            if (polygonPoints.Length < 3) {
                throw new ArgumentOutOfRangeException(nameof(polygonPoints), "Polygon must have at least 3 points");
            }

            // Calculate line normals and move lines in such direction by offset
            var movedLines = new UnsafeArray<Line2D>(polygonPoints.Length, ARAlloc.Temp, NativeArrayOptions.UninitializedMemory);

            for (var i = 0u; i < polygonPoints.Length; ++i) {
                var nextIndex = (i + 1) % polygonPoints.Length;
                var vectorAlongLine = polygonPoints[nextIndex] - polygonPoints[i];
                var normal = math.normalize(new float2(vectorAlongLine.y, -vectorAlongLine.x));

                var displacement = normal * offset;
                movedLines[i] = new Line2D(polygonPoints[i] + displacement, polygonPoints[nextIndex] + displacement);
            }

            // Calculate new points (naive 1:1) by intersection of moved lines
            var naivePoints = new UnsafeList<float2>(polygonPoints.LengthInt, ARAlloc.Temp);
            for (var i = 0u; i < polygonPoints.Length; ++i) {
                var nextIndex = (i + 1) % polygonPoints.Length;
                Algorithms2D.LinesIntersection(movedLines[i], movedLines[nextIndex], out var intersection);
                naivePoints.Add(intersection);
            }
            movedLines.Dispose();

            // Remove points inside new polygon
            for (var i = 0; i < naivePoints.Length; ++i) {
                var nextIndex = (i + 1) % naivePoints.Length;
                var currentLine = new LineSegment2D(naivePoints[i], naivePoints[nextIndex]);

                for (var j = 2; j < naivePoints.Length-2; ++j) {
                    var c = (i + j) % naivePoints.Length;
                    var n = (i + j + 1) % naivePoints.Length;
                    var nextLine = new LineSegment2D(naivePoints[c], naivePoints[n]);

                    Algorithms2D.LineSegmentsIntersection(currentLine, nextLine, out var intersection, out var intersects);
                    if (intersects) {
                        var removeStart = nextIndex;
                        var removeEnd = c;

                        var removeCount = CalculateRemoveRange(removeStart, removeEnd, naivePoints.Length);
                        var (otherStart, otherEnd, otherCount) = InverseRange(removeStart, removeEnd, naivePoints.Length);

                        if (otherCount < removeCount) {
                            removeStart = otherStart;
                            removeEnd = otherEnd;
                            removeCount = otherCount;
                        }

                        if (removeEnd > removeStart) {
                            naivePoints.RemoveRange(removeStart, removeCount);
                        } else {
                            naivePoints.RemoveRange(removeStart, naivePoints.Length - removeStart);
                            naivePoints.RemoveRange(0, removeEnd + 1);
                            i -= (removeEnd+1);
                            removeStart -= (removeEnd+1);
                        }
                        naivePoints.InsertRange(removeStart, 1);
                        naivePoints[removeStart] = intersection;
                    }
                }
            }

            result = new UnsafeArray<float2>(naivePoints, allocator);
            naivePoints.Dispose();

            static int CalculateRemoveRange(int removeStart, int removeEnd, int length) {
                if (removeEnd > removeStart) {
                    return removeEnd - removeStart + 1;
                } else {
                    return (length - removeStart) + (removeEnd + 1);
                }
            }

            static (int, int, int) InverseRange(int removeStart, int removeEnd, int length) {
                var newStart = (removeEnd + 1) % length;
                var newEnd = (removeStart - 1 + length) % length;
                return (newStart, newEnd, CalculateRemoveRange(newStart, newEnd, length));
            }
        }

        [BurstCompile]
        public static void SegmentsInRadius(in UnsafeArray<float2> polygonPoints, in float2 referencePoint, float radiusSq, Allocator allocator, out UnsafeArray<LineSegment2D> resultPoints) {
            if (polygonPoints.Length < 2) {
                resultPoints = new UnsafeArray<LineSegment2D>(0, allocator);
                return;
            }
            
            var result = new UnsafeList<LineSegment2D>(polygonPoints.LengthInt / 2, ARAlloc.Temp);
            
            for (var i = 0u; i < polygonPoints.Length; i++) {
                var nextIndex = (i + 1) % polygonPoints.Length;
                var currentPoint = polygonPoints[i];
                var nextPoint = polygonPoints[nextIndex];

                var lineSegment = new LineSegment2D(currentPoint, nextPoint);
                Algorithms2D.DistanceToLineSegmentSq(referencePoint, lineSegment, out var currentDistanceSq);

                if (currentDistanceSq <= radiusSq) {
                    result.Add(lineSegment);
                }
            }

            resultPoints = new UnsafeArray<LineSegment2D>(result, allocator);
            result.Dispose();
        }
    }
}