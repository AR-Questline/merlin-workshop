using Awaken.Utility.Collections;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;
using Random = UnityEngine.Random;

namespace Awaken.TG.Editor.MapPainter {
    public static class MapPainterUtility {
        static Collider[] s_nearby = new Collider[64];

        /// <summary>
        /// Checks if a painted prefab of the SAME TYPE already exists too close to the target position
        /// </summary>
        public static bool IsPrefabTooClose(Vector3 position, float minDistance, LayerMask layerMask, GameObject prefab) {
            var hits = Physics.OverlapSphereNonAlloc(position, minDistance, s_nearby, layerMask);
            for (var i = 0; i < hits; i++) {
                var hit = s_nearby[i];
                var prefabObject = PrefabUtility.GetCorrespondingObjectFromSource(hit.gameObject);
                // Check if hit is part of the same prefab as prefab parameter
                if (prefabObject && prefabObject.transform.root == prefab.transform) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Creates a parent group for organized hierarchy
        /// </summary>
        public static Transform GetOrCreatePaintGroup(string groupName) {
            GameObject group = GameObject.Find(groupName);
            if (group == null) {
                group = new GameObject(groupName);
                Undo.RegisterCreatedObjectUndo(group, "Create Paint Group");
            }
            return group.transform;
        }
        
        /// <summary>
        /// Applies vegetation-style positioning with proper normal alignment
        /// </summary>
        public static void AlignToSurfaceNormal(Transform transform, Vector3 normal, bool randomRotation = true) {
            if (randomRotation) {
                float randomYRotation = Random.Range(0f, 360f);
                Quaternion rotationSpace = Quaternion.AngleAxis(randomYRotation, normal);
                transform.rotation = Quaternion.LookRotation(Vector3.Cross(normal, transform.right), normal) * rotationSpace;
            } else {
                transform.rotation = Quaternion.LookRotation(Vector3.Cross(normal, Vector3.right), normal);
            }
        }
        
        /// <summary>
        /// Distribution patterns for different painting modes
        /// </summary>
        public enum DistributionPattern : byte {
            Random,
            Grid,
            PoissonDisk,
            Scattered
        }

        [BurstCompile]
        public struct SpawnPointsGeneratorJob : IJob {
            public float3 center;
            public float radius;
            public int count;
            public DistributionPattern pattern;
            public Unity.Mathematics.Random random;

            public NativeList<float3> outSpawnPoints;

            public void Execute() {
                if (pattern == DistributionPattern.Random) {
                    for (int i = 0; i < count; i++) {
                        var randomCircle = random.NextFloat3Direction() * radius;
                        outSpawnPoints.AddNoResize(center + new float3(randomCircle.x, 0, randomCircle.y));
                    }
                } else if (pattern == DistributionPattern.Grid) {
                    var gridSize = (int)math.ceil(math.sqrt(count));
                    var step = (radius * 2f) / gridSize;
                    var startPos = center - new float3(radius, 0, radius);
                    var radiusSq = radius * radius;

                    for (int x = 0; x < gridSize && outSpawnPoints.Length < count; x++) {
                        for (int z = 0; z < gridSize && outSpawnPoints.Length < count; z++) {
                            var pos = startPos + new float3(x * step, 0, z * step);
                            if (math.distancesq(center, pos) <= radiusSq) {
                                outSpawnPoints.AddNoResize(pos);
                            }
                        }
                    }
                } else if (pattern == DistributionPattern.PoissonDisk) {
                    GeneratePoissonDiskSampling(center, radius, count, ref random, outSpawnPoints);
                } else if (pattern == DistributionPattern.Scattered) {
                    // Similar to random but with minimum distance constraints
                    var minDistance = radius / math.sqrt(count);
                    var minDistanceSq = math.square(minDistance);
                    var attempts = 0;

                    while (outSpawnPoints.Length < count && attempts < count * 10) {
                        var randomCircle = random.NextFloat3Direction() * radius;
                        var candidate = center + new float3(randomCircle.x, 0, randomCircle.y);

                        bool tooClose = false;
                        foreach (var existing in outSpawnPoints) {
                            if (math.distancesq(candidate, existing) < minDistanceSq) {
                                tooClose = true;
                                break;
                            }
                        }

                        if (!tooClose) {
                            outSpawnPoints.AddNoResize(candidate);
                        }

                        attempts++;
                    }
                }
            }
        }

        static void GeneratePoissonDiskSampling(float3 center, float radius, int count, ref Unity.Mathematics.Random rng, NativeList<float3> outPositions) {
            var activeList = new NativeList<float3>(count, ARAlloc.InJobTempJob);

            var radiusSq = math.square(radius);
            var minDistance = radius / math.sqrt(count);
            var minDistanceSq = math.square(minDistance);
            var maxAttempts = 30;
            
            // Add initial point
            outPositions.AddNoResize(center);
            activeList.Add(center);
            
            while (activeList.Length > 0 && outPositions.Length < count) {
                var activeIndex = rng.NextInt(activeList.Length);
                var activePoint = activeList[activeIndex];
                var foundValidPoint = false;
                
                for (int attempt = 0; attempt < maxAttempts; attempt++) {
                    float angle = rng.NextFloat(math.PI2);
                    float distance = rng.NextFloat(minDistance, 2f * minDistance);
                    
                    var candidate = activePoint + new float3(
                        math.cos(angle) * distance,
                        0,
                        math.sin(angle) * distance
                    );
                    
                    if (math.distancesq(candidate, center) > radiusSq) continue;
                    
                    bool valid = true;
                    foreach (var point in outPositions) {
                        if (math.distancesq(candidate, point) < minDistanceSq) {
                            valid = false;
                            break;
                        }
                    }
                    
                    if (valid) {
                        outPositions.AddNoResize(candidate);
                        activeList.Add(candidate);
                        foundValidPoint = true;
                        break;
                    }
                }
                
                if (!foundValidPoint) {
                    activeList.RemoveAt(activeIndex);
                }
            }

            activeList.Dispose();
        }
    }
}