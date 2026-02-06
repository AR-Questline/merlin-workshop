using Awaken.Utility.LowLevel.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Awaken.TG.Editor.Terrains.Operations {
    // === Mesh Cache Data
    public struct MeshCacheData {
        public NativeArray<float3> vertices;
        public NativeArray<int> triangles;

        public MeshCacheData(UnityEngine.Mesh mesh, Allocator allocator) {
            var meshVertices = mesh.vertices;
            var meshTriangles = mesh.triangles;

            vertices = new NativeArray<float3>(meshVertices.Length, allocator);
            triangles = new NativeArray<int>(meshTriangles.Length, allocator);

            for (int i = 0; i < meshVertices.Length; i++) {
                vertices[i] = meshVertices[i];
            }

            triangles.CopyFrom(meshTriangles);
        }

        public void Dispose() {
            if (vertices.IsCreated) {
                vertices.Dispose();
            }
            if (triangles.IsCreated) {
                triangles.Dispose();
            }
        }
    }

    // === Ray-Triangle Intersection Job
    [BurstCompile]
    public struct RayTriangleIntersectionJob : IJob {
        [ReadOnly] public NativeArray<float3> vertices;
        [ReadOnly] public NativeArray<int> triangles;
        public float3 localRayOrigin;
        public float3 localRayDirection;
        [WriteOnly] public NativeArray<int> outIntersectionCount;

        public void Execute() {
            int intersectionCount = 0;

            for (int i = 0; i < triangles.Length; i += 3) {
                var v0 = vertices[triangles[i]];
                var v1 = vertices[triangles[i + 1]];
                var v2 = vertices[triangles[i + 2]];

                if (RayIntersectsTriangle(localRayOrigin, localRayDirection, v0, v1, v2, out float t)) {
                    if (t > 0.0001f) {
                        intersectionCount++;
                    }
                }
            }

            outIntersectionCount[0] = intersectionCount;
        }

        bool RayIntersectsTriangle(float3 rayOrigin, float3 rayDirection, float3 v0, float3 v1, float3 v2, out float t) {
            t = 0;
            const float epsilon = 0.0000001f;

            var edge1 = v1 - v0;
            var edge2 = v2 - v0;
            var h = math.cross(rayDirection, edge2);
            var a = math.dot(edge1, h);

            if (a > -epsilon && a < epsilon) {
                return false;
            }

            var f = 1f / a;
            var s = rayOrigin - v0;
            var u = f * math.dot(s, h);

            if (u < 0f || u > 1f) {
                return false;
            }

            var q = math.cross(s, edge1);
            var v = f * math.dot(rayDirection, q);

            if (v < 0f || u + v > 1f) {
                return false;
            }

            t = f * math.dot(edge2, q);
            return t > epsilon;
        }
    }

    // === Parallel Terrain Cell Processing Job
    [BurstCompile]
    public struct ProcessTerrainCellsJob : IJobParallelFor {
        [ReadOnly] public int minX;
        [ReadOnly] public int minZ;
        [ReadOnly] public int maxX;
        [ReadOnly] public int maxZ;
        [ReadOnly] public int holesResolution;
        [ReadOnly] public float3 terrainSize;
        [ReadOnly] public float3 terrainPosition;
        [ReadOnly] public int subSamplesPerCell;
        [ReadOnly] public float holeShrinkOffset;
        [ReadOnly] public bool invertHoles;

        [ReadOnly] public NativeArray<float3> vertices;
        [ReadOnly] public NativeArray<int> triangles;
        [ReadOnly] public NativeArray<float4x4> colliderTransforms;
        [ReadOnly] public NativeArray<float3> colliderBoundsMin;
        [ReadOnly] public NativeArray<float3> colliderBoundsMax;

        [ReadOnly] public NativeArray<float> terrainHeights;
        [ReadOnly] public int heightmapResolution;

        [WriteOnly] public NativeArray<bool> outHoles;

        public void Execute(int linearIndex) {
            int width = maxX - minX + 1;
            int x = linearIndex % width + minX;
            int z = linearIndex / width + minZ;

            if (x > maxX || z > maxZ) {
                return;
            }

            float cellSize = terrainSize.x / (holesResolution - 1);
            int insideCount = 0;
            int totalSamples = subSamplesPerCell * subSamplesPerCell;

            for (int sx = 0; sx < subSamplesPerCell; sx++) {
                for (int sz = 0; sz < subSamplesPerCell; sz++) {
                    float offsetX = (sx + 0.5f) / subSamplesPerCell - 0.5f;
                    float offsetZ = (sz + 0.5f) / subSamplesPerCell - 0.5f;

                    var baseWorldPos = HoleCoordToWorldWithTerrainHeight(x, z);
                    var worldPos = baseWorldPos + new float3(offsetX * cellSize, 0, offsetZ * cellSize);

                    var normalizedX = (worldPos.x - terrainPosition.x) / terrainSize.x;
                    var normalizedZ = (worldPos.z - terrainPosition.z) / terrainSize.z;

                    if (normalizedX >= 0 && normalizedX <= 1 && normalizedZ >= 0 && normalizedZ <= 1) {
                        var terrainHeight = GetInterpolatedHeight(normalizedZ, normalizedX);
                        worldPos.y = terrainPosition.y + terrainHeight;
                    }

                    bool isInsideAny = false;
                    for (int colliderIdx = 0; colliderIdx < colliderTransforms.Length; colliderIdx++) {
                        if (IsPointInsideColliderVolume(colliderIdx, worldPos)) {
                            isInsideAny = true;
                            break;
                        }
                    }

                    if (isInsideAny) {
                        insideCount++;
                    }
                }
            }

            if (insideCount > totalSamples / 2) {
                bool newValue = invertHoles ? true : false;
                outHoles[linearIndex] = newValue;
            }
            // else: keep existing value (already initialized from input)
        }

        float3 HoleCoordToWorldWithTerrainHeight(int holeCoordX, int holeCoordZ) {
            var normalizedX = (float)holeCoordX / (holesResolution - 1);
            var normalizedZ = (float)holeCoordZ / (holesResolution - 1);

            var height = GetInterpolatedHeight(normalizedZ, normalizedX);

            return new float3(
                terrainPosition.x + normalizedX * terrainSize.x,
                terrainPosition.y + height,
                terrainPosition.z + normalizedZ * terrainSize.z
            );
        }

        float GetInterpolatedHeight(float normalizedZ, float normalizedX) {
            float x = normalizedX * (heightmapResolution - 1);
            float z = normalizedZ * (heightmapResolution - 1);

            int x0 = (int)math.floor(x);
            int z0 = (int)math.floor(z);
            int x1 = math.min(x0 + 1, heightmapResolution - 1);
            int z1 = math.min(z0 + 1, heightmapResolution - 1);

            float fx = x - x0;
            float fz = z - z0;

            float h00 = terrainHeights[z0 * heightmapResolution + x0];
            float h10 = terrainHeights[z0 * heightmapResolution + x1];
            float h01 = terrainHeights[z1 * heightmapResolution + x0];
            float h11 = terrainHeights[z1 * heightmapResolution + x1];

            float h0 = math.lerp(h00, h10, fx);
            float h1 = math.lerp(h01, h11, fx);

            return math.lerp(h0, h1, fz) * terrainSize.y;
        }

        bool IsPointInsideColliderVolume(int colliderIdx, float3 point) {
            var transform = colliderTransforms[colliderIdx];
            var boundsMin = colliderBoundsMin[colliderIdx];
            var boundsMax = colliderBoundsMax[colliderIdx];

            float3 boundsCenter = (boundsMin + boundsMax) * 0.5f;
            float3 boundsExtents = (boundsMax - boundsMin) * 0.5f;

            if (holeShrinkOffset > 0.001f) {
                var directionFromCenter = math.normalize(point - boundsCenter);
                point = point + directionFromCenter * holeShrinkOffset;
            }

            if (point.x < boundsMin.x || point.x > boundsMax.x ||
                point.y < boundsMin.y || point.y > boundsMax.y ||
                point.z < boundsMin.z || point.z > boundsMax.z) {
                return false;
            }

            var localPoint = math.mul(math.inverse(transform), new float4(point, 1)).xyz;
            var rayDirection = new float3(1, 0, 0);

            int intersectionCount = CountRayTriangleIntersections(localPoint, rayDirection);

            return intersectionCount % 2 == 1;
        }

        int CountRayTriangleIntersections(float3 localRayOrigin, float3 localRayDirection) {
            int intersectionCount = 0;

            for (int i = 0; i < triangles.Length; i += 3) {
                var v0 = vertices[triangles[i]];
                var v1 = vertices[triangles[i + 1]];
                var v2 = vertices[triangles[i + 2]];

                if (RayIntersectsTriangle(localRayOrigin, localRayDirection, v0, v1, v2, out float t)) {
                    if (t > 0.0001f) {
                        intersectionCount++;
                    }
                }
            }

            return intersectionCount;
        }

        bool RayIntersectsTriangle(float3 rayOrigin, float3 rayDirection, float3 v0, float3 v1, float3 v2, out float t) {
            t = 0;
            const float epsilon = 0.0000001f;

            var edge1 = v1 - v0;
            var edge2 = v2 - v0;
            var h = math.cross(rayDirection, edge2);
            var a = math.dot(edge1, h);

            if (a > -epsilon && a < epsilon) {
                return false;
            }

            var f = 1f / a;
            var s = rayOrigin - v0;
            var u = f * math.dot(s, h);

            if (u < 0f || u > 1f) {
                return false;
            }

            var q = math.cross(s, edge1);
            var v = f * math.dot(rayDirection, q);

            if (v < 0f || u + v > 1f) {
                return false;
            }

            t = f * math.dot(edge2, q);
            return t > epsilon;
        }
    }

    // === Batch Multiple Meshes Processing Job
    [BurstCompile]
    public struct ProcessMultipleMeshesJob : IJobParallelFor {
        [ReadOnly] public int minX;
        [ReadOnly] public int minZ;
        [ReadOnly] public int maxX;
        [ReadOnly] public int maxZ;
        [ReadOnly] public int holesResolution;
        [ReadOnly] public float3 terrainSize;
        [ReadOnly] public float3 terrainPosition;
        [ReadOnly] public int subSamplesPerCell;
        [ReadOnly] public float holeShrinkOffset;
        [ReadOnly] public bool invertHoles;

        [ReadOnly] public NativeArray<float4x4> meshTransforms;
        [ReadOnly] public NativeArray<float3> meshBoundsMin;
        [ReadOnly] public NativeArray<float3> meshBoundsMax;
        [ReadOnly] public NativeArray<float3> meshBoundsCenter;
        [ReadOnly] public NativeArray<int> meshTrianglesStart;
        [ReadOnly] public NativeArray<int> meshTrianglesCount;
        [ReadOnly] public NativeArray<int> meshVerticesStart;

        [ReadOnly] public NativeArray<float3> sharedVertices;
        [ReadOnly] public NativeArray<int> sharedTriangles;

        [ReadOnly] public NativeArray<float> terrainHeights;
        [ReadOnly] public int heightmapResolution;

        [WriteOnly] public NativeArray<bool> outHoles;

        public void Execute(int linearIndex) {
            int width = maxX - minX + 1;
            int x = linearIndex % width + minX;
            int z = linearIndex / width + minZ;

            if (x > maxX || z > maxZ) {
                return;
            }

            float cellSize = terrainSize.x / (holesResolution - 1);
            int insideCount = 0;
            int totalSamples = subSamplesPerCell * subSamplesPerCell;

            for (int sx = 0; sx < subSamplesPerCell; sx++) {
                for (int sz = 0; sz < subSamplesPerCell; sz++) {
                    float offsetX = (sx + 0.5f) / subSamplesPerCell - 0.5f;
                    float offsetZ = (sz + 0.5f) / subSamplesPerCell - 0.5f;

                    var baseWorldPos = HoleCoordToWorldWithTerrainHeight(x, z);
                    var worldPos = baseWorldPos + new float3(offsetX * cellSize, 0, offsetZ * cellSize);

                    var normalizedX = (worldPos.x - terrainPosition.x) / terrainSize.x;
                    var normalizedZ = (worldPos.z - terrainPosition.z) / terrainSize.z;

                    if (normalizedX >= 0 && normalizedX <= 1 && normalizedZ >= 0 && normalizedZ <= 1) {
                        var terrainHeight = GetInterpolatedHeight(normalizedZ, normalizedX);
                        worldPos.y = terrainPosition.y + terrainHeight;
                    }

                    bool isInsideAny = false;
                    for (int instanceIdx = 0; instanceIdx < meshTransforms.Length; instanceIdx++) {
                        if (IsPointInsideMeshInstance(instanceIdx, worldPos)) {
                            isInsideAny = true;
                            break;
                        }
                    }

                    if (isInsideAny) {
                        insideCount++;
                    }
                }
            }

            if (insideCount > totalSamples / 2) {
                bool newValue = invertHoles ? true : false;
                outHoles[linearIndex] = newValue;
            }
            // else: keep existing value (already initialized from input)
        }

        float3 HoleCoordToWorldWithTerrainHeight(int holeCoordX, int holeCoordZ) {
            var normalizedX = (float)holeCoordX / (holesResolution - 1);
            var normalizedZ = (float)holeCoordZ / (holesResolution - 1);

            var height = GetInterpolatedHeight(normalizedZ, normalizedX);

            return new float3(
                terrainPosition.x + normalizedX * terrainSize.x,
                terrainPosition.y + height,
                terrainPosition.z + normalizedZ * terrainSize.z
            );
        }

        float GetInterpolatedHeight(float normalizedZ, float normalizedX) {
            float x = normalizedX * (heightmapResolution - 1);
            float z = normalizedZ * (heightmapResolution - 1);

            int x0 = (int)math.floor(x);
            int z0 = (int)math.floor(z);
            int x1 = math.min(x0 + 1, heightmapResolution - 1);
            int z1 = math.min(z0 + 1, heightmapResolution - 1);

            float fx = x - x0;
            float fz = z - z0;

            float h00 = terrainHeights[z0 * heightmapResolution + x0];
            float h10 = terrainHeights[z0 * heightmapResolution + x1];
            float h01 = terrainHeights[z1 * heightmapResolution + x0];
            float h11 = terrainHeights[z1 * heightmapResolution + x1];

            float h0 = math.lerp(h00, h10, fx);
            float h1 = math.lerp(h01, h11, fx);

            return math.lerp(h0, h1, fz) * terrainSize.y;
        }

        bool IsPointInsideMeshInstance(int instanceIdx, float3 point) {
            var boundsMin = meshBoundsMin[instanceIdx];
            var boundsMax = meshBoundsMax[instanceIdx];
            var boundsCenter = meshBoundsCenter[instanceIdx];
            var transform = meshTransforms[instanceIdx];
            var trianglesStart = meshTrianglesStart[instanceIdx];
            var trianglesCount = meshTrianglesCount[instanceIdx];

            if (holeShrinkOffset > 0.001f) {
                var directionFromCenter = math.normalize(point - boundsCenter);
                point = point + directionFromCenter * holeShrinkOffset;
            }

            if (point.x < boundsMin.x || point.x > boundsMax.x ||
                point.y < boundsMin.y || point.y > boundsMax.y ||
                point.z < boundsMin.z || point.z > boundsMax.z) {
                return false;
            }

            var localPoint = math.mul(math.inverse(transform), new float4(point, 1)).xyz;
            var rayDirection = new float3(1, 0, 0);

            int intersectionCount = 0;

            for (int i = trianglesStart; i < trianglesStart + trianglesCount; i += 3) {
                var v0 = sharedVertices[sharedTriangles[i]];
                var v1 = sharedVertices[sharedTriangles[i + 1]];
                var v2 = sharedVertices[sharedTriangles[i + 2]];

                if (RayIntersectsTriangle(localPoint, rayDirection, v0, v1, v2, out float t)) {
                    if (t > 0.0001f) {
                        intersectionCount++;
                    }
                }
            }

            return intersectionCount % 2 == 1;
        }

        bool RayIntersectsTriangle(float3 rayOrigin, float3 rayDirection, float3 v0, float3 v1, float3 v2, out float t) {
            t = 0;
            const float epsilon = 0.0000001f;

            var edge1 = v1 - v0;
            var edge2 = v2 - v0;
            var h = math.cross(rayDirection, edge2);
            var a = math.dot(edge1, h);

            if (a > -epsilon && a < epsilon) {
                return false;
            }

            var f = 1f / a;
            var s = rayOrigin - v0;
            var u = f * math.dot(s, h);

            if (u < 0f || u > 1f) {
                return false;
            }

            var q = math.cross(s, edge1);
            var v = f * math.dot(rayDirection, q);

            if (v < 0f || u + v > 1f) {
                return false;
            }

            t = f * math.dot(edge2, q);
            return t > epsilon;
        }
    }
}
