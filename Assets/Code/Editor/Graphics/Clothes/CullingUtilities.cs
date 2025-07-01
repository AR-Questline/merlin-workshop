using System.Collections.Generic;
using Awaken.Kandra;
using Awaken.Kandra.Data;
using Awaken.Utility;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Awaken.TG.Editor.Graphics.Clothes {
    [BurstCompile]
    public static class CullingUtilities {
        // === Global
        public static List<KandraTrisCuller.CulledRange> CalculateCulledRanges(uint trisCount, UnsafeBitmask visibleTriangles) {
            var ranges = new List<KandraTrisCuller.CulledRange>();
            var rangeStart = -1;
            var rangeLength = 0;
            for (var i = 0u; i < trisCount; ++i) {
                if (!visibleTriangles[i]) {
                    if (rangeStart == -1) {
                        rangeStart = (int)i;
                    }

                    ++rangeLength;
                } else if (rangeStart != -1) {
                    CheckRanges(rangeStart, rangeLength);
                    ranges.Add(new KandraTrisCuller.CulledRange {
                        start = (uint)rangeStart,
                        length = (ushort)rangeLength
                    });
                    rangeStart = -1;
                    rangeLength = 0;
                }
            }

            if (rangeStart != -1) {
                CheckRanges(rangeStart, rangeLength);
                ranges.Add(new KandraTrisCuller.CulledRange {
                    start = (uint)rangeStart,
                    length = (ushort)rangeLength
                });
            }
            return ranges;

            void CheckRanges(int start, int length) {
                if (length == 0) {
                    return;
                }

                if (start < 0) {
                    Log.Minor?.Error("Invalid range start");
                }

                if (length is < 0 or > ushort.MaxValue) {
                    Log.Minor?.Error("Invalid range length");
                }

                if (((uint)start + length) is < 0 or > int.MaxValue) {
                    Log.Minor?.Error("Invalid range end");
                }
            }
        }

        // === AutoCulling
        public static void AppendAutoCulling(KandraRenderer renderer, KandraRenderer culleeRenderer, uint trisCount, ref UnsafeBitmask visibleTriangles, in UnsafeArray<ushort>.Span indices, float cullingOffset, float cullingDistance) {
            var mesh = renderer.BakePoseMesh();
            var cullerTmpGo = new GameObject("Culler", typeof(MeshCollider));
            var cullerCollider = cullerTmpGo.GetComponent<MeshCollider>();
            cullerCollider.sharedMesh = mesh;
            var (vertices, additionalData) = culleeRenderer.BakePoseVertices(ARAlloc.Temp);

            var possibleTriangles = visibleTriangles.CountOnes();
            var rays = new UnsafeArray<Ray>(possibleTriangles * 4, ARAlloc.Temp);
            var rayIndex = 0u;
            CalculateRays(trisCount, visibleTriangles, indices, ref rays, rayIndex, vertices, cullingOffset);

            rayIndex = 0u;
            for (var i = 0u; i < trisCount; ++i) {
                if (!visibleTriangles[i]) {
                    continue;
                }

                bool shouldBeCulled = true;

                shouldBeCulled = shouldBeCulled && IsVertexToCull(rays[rayIndex + 0], cullerCollider, cullingDistance);
                shouldBeCulled = shouldBeCulled && IsVertexToCull(rays[rayIndex + 1], cullerCollider, cullingDistance);
                shouldBeCulled = shouldBeCulled && IsVertexToCull(rays[rayIndex + 2], cullerCollider, cullingDistance);
                shouldBeCulled = shouldBeCulled && IsVertexToCull(rays[rayIndex + 3], cullerCollider, cullingDistance);
                rayIndex += 4;

                if (shouldBeCulled) {
                    visibleTriangles.Down(i);
                }
            }

            GameObject.DestroyImmediate(cullerTmpGo);
            GameObject.DestroyImmediate(mesh);

            vertices.Dispose();
            additionalData.Dispose();
        }

        [BurstCompile]
        static void CalculateRays(uint trisCount, in UnsafeBitmask visibleTriangles, in UnsafeArray<ushort>.Span indices, ref UnsafeArray<Ray> rays, uint rayIndex, in UnsafeArray<CompressedVertex> vertices, float cullerOffset) {
            for (var i = 0u; i < trisCount; ++i) {
                if (!visibleTriangles[i]) {
                    continue;
                }

                rays[rayIndex++] = CalculateRay(vertices, indices, i, new float2(0, 0), cullerOffset);
                rays[rayIndex++] = CalculateRay(vertices, indices, i, new float2(1, 0), cullerOffset);
                rays[rayIndex++] = CalculateRay(vertices, indices, i, new float2(0, 1), cullerOffset);
                rays[rayIndex++] = CalculateRay(vertices, indices, i, new float2(0.33f, 0.33f), cullerOffset);
            }
        }

        static bool IsVertexToCull(Ray ray, MeshCollider cullerCollider, float cullerDistance) {
            return cullerCollider.Raycast(ray, out _, cullerDistance);
        }

        static Ray CalculateRay(UnsafeArray<CompressedVertex>.Span vertices, UnsafeArray<ushort>.Span indices, uint triangleIndex, float2 coords, float cullerOffset) {
            var i0 = indices[triangleIndex * 3 + 0];
            var i1 = indices[triangleIndex * 3 + 1];
            var i2 = indices[triangleIndex * 3 + 2];

            var v0 = vertices[i0].position;
            var v1 = vertices[i1].position;
            var v2 = vertices[i2].position;

            var n0 = vertices[i0].Normal;
            var n1 = vertices[i1].Normal;
            var n2 = vertices[i2].Normal;

            var samplePosition = v0 * coords.x + v1 * coords.y + v2 * (1f - coords.x - coords.y);
            var sampleNormal = math.normalize(n0 * coords.x + n1 * coords.y + n2 * (1f - coords.x - coords.y));

            var ray = new Ray(samplePosition + sampleNormal * cullerOffset, -sampleNormal);
            return ray;
        }

        // === Painting
        [BurstCompile]
        public static void BrushPaint(in float3 cameraNormal, in float3 rayOrigin, in float3 rayDirection, ref UnsafeBitmask visibleTriangles, in UnsafeArray<ushort>.Span indices, in UnsafeArray<CompressedVertex> vertices, float hideBackTrianglesFactor, float radius, bool culling) {
            const float AvgFactor = 1f / 3;
            const float NegligibleDistance = 0.65f;
            const float NegligibleDistanceSq = NegligibleDistance*NegligibleDistance;

            var radiusSq = radius * radius;

            var trisCount = visibleTriangles.ElementsLength;
            var currentCulling = new UnsafeBitmask(trisCount, ARAlloc.Temp);
            var distances = new UnsafeList<float>((int)trisCount, ARAlloc.Temp);
            var currentDistanceSq = float.MaxValue;

            for (var i = 0u; i < trisCount; i++) {
                var i0 = indices[i * 3 + 0];
                var i1 = indices[i * 3 + 1];
                var i2 = indices[i * 3 + 2];

                var n0 = vertices[i0].Normal;
                var n1 = vertices[i1].Normal;
                var n2 = vertices[i2].Normal;

                var triangleNormal = (n0 + n1 + n2) / 3;

                if (math.dot(triangleNormal, cameraNormal) < hideBackTrianglesFactor) {
                    continue;
                }

                var vertex0 = vertices[i0].position;
                var vertex1 = vertices[i1].position;
                var vertex2 = vertices[i2].position;
                var verticesInPaint = (math.lengthsq(math.cross(rayDirection, vertex0 - rayOrigin)) <= radiusSq) &
                                      (math.lengthsq(math.cross(rayDirection, vertex1 - rayOrigin)) <= radiusSq) &
                                      (math.lengthsq(math.cross(rayDirection, vertex2 - rayOrigin)) <= radiusSq);

                if (!verticesInPaint) {
                    continue;
                }

                var avgVertex = (vertex0 + vertex1 + vertex2) * AvgFactor;
                var distanceSq = math.distancesq(avgVertex, rayOrigin);
                var distancesDiffSq = math.abs(currentDistanceSq - distanceSq);
                if (distancesDiffSq <= NegligibleDistanceSq) {
                    currentCulling.Up(i);
                    distances.AddNoResize(distanceSq);
                    currentDistanceSq = distances.Avg();
                } else if (distanceSq < currentDistanceSq) {
                    currentCulling.Zero();
                    distances.Clear();

                    currentCulling.Up(i);

                    distances.AddNoResize(distanceSq);
                    currentDistanceSq = distances.Avg();
                }
            }
            distances.Dispose();

            if (culling) {
                foreach (var triangleIndex in currentCulling.EnumerateOnes()) {
                    visibleTriangles.Down(triangleIndex);
                }
            } else {
                foreach (var triangleIndex in currentCulling.EnumerateOnes()) {
                    visibleTriangles.Up(triangleIndex);
                }
            }

            currentCulling.Dispose();
        }

        [BurstCompile]
        public static void SinglePaint(in float3 cameraNormal, in float3 rayOrigin, in float3 rayDirection, ref UnsafeBitmask visibleTriangles, in UnsafeArray<ushort>.Span indices, in UnsafeArray<CompressedVertex> vertices, float hideBackTrianglesFactor, bool culling) {
            var trisCount = visibleTriangles.ElementsLength;
            var underMouseIndex = Optional<uint>.None;
            var underMouseDistanceSq = float.MaxValue;

            for (var i = 0u; i < trisCount; i++) {
                var intersection = IsTriangleUnderMouse(i, indices, vertices, cameraNormal, rayOrigin, rayDirection, hideBackTrianglesFactor);
                if (intersection) {
                    var distance = math.distancesq(intersection.Value, rayOrigin);
                    if (distance < underMouseDistanceSq) {
                        underMouseIndex = i;
                        underMouseDistanceSq = distance;
                    }
                }
            }

            if (underMouseIndex) {
                if (culling) {
                    visibleTriangles.Down(underMouseIndex.Value);
                } else {
                    visibleTriangles.Up(underMouseIndex.Value);
                }
            }
        }

        static Optional<float3> IsTriangleUnderMouse(uint trisCount, in UnsafeArray<ushort>.Span indices, in UnsafeArray<CompressedVertex> vertices, in float3 cameraNormal, in float3 rayOrigin, in float3 rayDirection, float hideBackTrianglesFactor) {
            var i0 = indices[trisCount * 3 + 0];
            var i1 = indices[trisCount * 3 + 1];
            var i2 = indices[trisCount * 3 + 2];

            var n0 = vertices[i0].Normal;
            var n1 = vertices[i1].Normal;
            var n2 = vertices[i2].Normal;

            var triangleNormal = (n0 + n1 + n2) / 3;

            if (math.dot(triangleNormal, cameraNormal) < hideBackTrianglesFactor) {
                return Optional<float3>.None;
            }

            var vertex0 = vertices[i0].position;
            var vertex1 = vertices[i1].position;
            var vertex2 = vertices[i2].position;
            return mathExt.IntersectRayTriangle(rayOrigin, rayDirection, vertex0, vertex1, vertex2);
        }

        // === Others
        [BurstCompile]
        public static void FillWireframeLineSegments(uint trisCount, in UnsafeArray<ushort>.Span indices, in UnsafeArray<CompressedVertex> vertices, in float3 cameraNormal, in UnsafeBitmask visibleTriangles, ref UnsafeList<int> visibleSegments, ref UnsafeList<int> culledSegments, float hideBackTrianglesFactor) {
            for (var i = 0u; i < trisCount; i++) {
                var i0 = indices[i * 3 + 0];
                var i1 = indices[i * 3 + 1];
                var i2 = indices[i * 3 + 2];

                var n0 = vertices[i0].Normal;
                var n1 = vertices[i1].Normal;
                var n2 = vertices[i2].Normal;

                var triangleNormal = (n0 + n1 + n2) / 3;
                if (math.dot(triangleNormal, cameraNormal) < hideBackTrianglesFactor) {
                    continue;
                }

                if (visibleTriangles[i]) {
                    visibleSegments.AddNoResize(i0);
                    visibleSegments.AddNoResize(i1);

                    visibleSegments.AddNoResize(i1);
                    visibleSegments.AddNoResize(i2);

                    visibleSegments.AddNoResize(i2);
                    visibleSegments.AddNoResize(i0);
                } else {
                    culledSegments.AddNoResize(i0);
                    culledSegments.AddNoResize(i1);

                    culledSegments.AddNoResize(i1);
                    culledSegments.AddNoResize(i2);

                    culledSegments.AddNoResize(i2);
                    culledSegments.AddNoResize(i0);
                }
            }
        }
    }
}
