using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Awaken.Utility;
using Awaken.Utility.Debugging;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Awaken.Utility.Maths.Data;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Object = System.Object;

namespace Awaken.TG.Editor.Terrains.Operations {
    public class TerrainOperator : OdinEditorWindow {
        [SerializeField] ProjectCleanup cleanup;
        [SerializeField] Splitter splitter;
        [SerializeField] Merger merger;
        [SerializeField] Neighbourer neighbourer;
        [SerializeField] SplatmapDeleter splatmapDeleter;
        [SerializeField] ColliderHolePainter colliderHolePainter;

        [MenuItem("TG/Scene Tools/Terrain Operator")]
        static void Open() => GetWindow<TerrainOperator>().Show();

        [Serializable]
        class ProjectCleanup {
            [SerializeField, FolderPath] string path;
            [SerializeField] GameObject root;

            [Button]
            void Cleanup() {
                foreach(var terrain in root.GetComponentsInChildren<Terrain>(true)) {
                    var data = terrain.terrainData;
                    var originPath = AssetDatabase.GetAssetPath(data);
                    AssetDatabase.MoveAsset(originPath, $"{path}\\{terrain.name}.asset");
                }
            }
        }
        
        [Serializable]
        class Splitter {
            [SerializeField] List<Terrain> terrains;

            [Button]
            void Split() {
                foreach (var terrain in terrains) {
                    TerrainSplitter.Split(terrain);
                }
                terrains.Clear();
            }
        }
        
        [Serializable]
        class Merger {
            [SerializeField] List<GameObject> terrains;

            [Button]
            void Merge() {
                foreach (var terrain in terrains) {
                    TerrainMerger.Merge(terrain);
                }
                terrains.Clear();
            }
        }

        [Serializable]
        class Neighbourer {
            [SerializeField] GameObject root;
            [SerializeField] Terrain source;

            [Button]
            void FixNeighboursGlobally() {
                var terrains = root.GetComponentsInChildren<Terrain>(true);

                MinMaxAABR bounds = MinMaxAABR.Empty;
                var compatibleTerrains = new UnsafePinnableList<Terrain>(terrains.Length);

                GetSelector(source, out var sourceSize, out var sourceResolution);
                foreach (Terrain terrain in terrains) {
                    GetSelector(terrain, out var terrainSize, out var terrainResolution);
                    if (terrainSize == sourceSize && terrainResolution == sourceResolution) {
                        var position = terrain.transform.position;
                        bounds.Encapsulate(position.xz());
                        terrain.groupingID = 0;
                        terrain.allowAutoConnect = true;
                        compatibleTerrains.Add(terrain);
                    } else {
                        terrain.groupingID = 1;
                        terrain.allowAutoConnect = false;
                        terrain.SetNeighbors(null, null, null, null);
                    }
                }

                TerrainUtil.DoNeighbouring(sourceSize, compatibleTerrains.AsSpan(), bounds);
            }

            static void GetSelector(Terrain terrain, out float size, out int resolution) {
                var data = terrain.terrainData;
                size = data.size.x;
                resolution = data.heightmapResolution;
            }
        }

        [Serializable]
        class SplatmapDeleter {
            [SerializeField] TerrainData data;
            [SerializeField] Texture2D alphamap;

            [ShowInInspector] TerrainLayer[] Layers => data?.terrainLayers;

            [Button]
            void Fix() {
                DestroyImmediate(alphamap, true);
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssetIfDirty(data);
            }

            bool IsEmpty(float[,,] alphamaps, int width, int height, int layer) {
                for (int x = 0; x < width; x++) {
                    for (int y = 0; y < height; y++) {
                        if (alphamaps[x, y, layer] > 0) {
                            return false;
                        }
                    }
                }
                return true;
            }
        }

        [Serializable]
        class ColliderHolePainter {
            [SerializeField] Terrain terrain;
            [SerializeField] GameObject terrainParent;
            [SerializeField] List<GameObject> objectsWithColliders = new();
            [SerializeField] bool invertHoles;
            [SerializeField] bool clearExistingHoles;
            [SerializeField, Range(1, 5)] int subSamplesPerCell = 3;
            [SerializeField, Range(0f, 2f)] float holeShrinkOffset = 0.2f;
            [SerializeField, Tooltip("Use Burst-compiled jobs for significantly faster performance")] bool useBurstOptimization = true;

            [ShowInInspector, Sirenix.OdinInspector.ReadOnly] int lastProcessedCells;
            [ShowInInspector, Sirenix.OdinInspector.ReadOnly] int lastHolesPainted;
            [ShowInInspector, Sirenix.OdinInspector.ReadOnly] int lastColliderCount;
            [ShowInInspector, Sirenix.OdinInspector.ReadOnly] int lastUniqueMeshes;
            [ShowInInspector, Sirenix.OdinInspector.ReadOnly] int lastTerrainCount;
            [ShowInInspector, Sirenix.OdinInspector.ReadOnly] string lastProcessingTime;

            // Cache for mesh data to avoid recalculating
            readonly Dictionary<Mesh, MeshCache> _meshCache = new();
            readonly Dictionary<Mesh, MeshCacheData> _nativeMeshCache = new();

            [Button("Paint Holes from Colliders")]
            void PaintHoles() {
                var startTime = System.Diagnostics.Stopwatch.StartNew();

                // Collect terrains to process
                var terrains = new List<Terrain>();

                if (terrainParent != null) {
                    // Get all terrains from parent
                    var childTerrains = terrainParent.GetComponentsInChildren<Terrain>();
                    terrains.AddRange(childTerrains);
                } else if (terrain != null) {
                    // Use single terrain
                    terrains.Add(terrain);
                } else {
                    Log.Important?.Warning("Neither terrain nor terrain parent is assigned");
                    return;
                }

                if (terrains.Count == 0) {
                    Log.Important?.Warning("No terrains found");
                    return;
                }

                if (objectsWithColliders == null || objectsWithColliders.Count == 0) {
                    Log.Important?.Warning("No objects with colliders assigned");
                    return;
                }

                lastTerrainCount = terrains.Count;
                Log.Important?.Info($"Processing {lastTerrainCount} terrain(s) using {(useBurstOptimization ? "Burst-optimized" : "standard")} path...");

                // Process each terrain
                for (int terrainIndex = 0; terrainIndex < terrains.Count; terrainIndex++) {
                    var currentTerrain = terrains[terrainIndex];
                    var data = currentTerrain.terrainData;
                    var holesResolution = data.holesResolution;
                    var terrainSize = data.size;
                    var terrainPosition = currentTerrain.transform.position;

                    EditorUtility.DisplayProgressBar("Painting Terrain Holes",
                        $"Processing terrain {terrainIndex + 1}/{terrains.Count}: {currentTerrain.name}",
                        (float)terrainIndex / terrains.Count);

                    // Get existing holes or create new array
                    var holes = clearExistingHoles
                        ? new bool[holesResolution, holesResolution]
                        : data.GetHoles(0, 0, holesResolution, holesResolution);

                    // Initialize to true (no holes) if clearing
                    if (clearExistingHoles) {
                        for (int x = 0; x < holesResolution; x++) {
                            for (int z = 0; z < holesResolution; z++) {
                                holes[x, z] = true;
                            }
                        }
                    }

                    if (useBurstOptimization) {
                        ProcessTerrainBurst(currentTerrain, holes, holesResolution, terrainSize, terrainPosition, data, terrainIndex, terrains.Count);
                    } else {
                        ProcessTerrain(currentTerrain, holes, holesResolution, terrainSize, terrainPosition, data, terrainIndex, terrains.Count);
                    }
                }

                EditorUtility.ClearProgressBar();
                startTime.Stop();
                lastProcessingTime = $"{startTime.Elapsed.TotalSeconds:F2}s";
                Log.Important?.Info($"Completed in {lastProcessingTime}! Painted {lastHolesPainted} holes across {lastTerrainCount} terrain(s) from {lastProcessedCells} processed cells");
            }

            void ProcessTerrain(Terrain currentTerrain, bool[,] holes, int holesResolution, Vector3 terrainSize, Vector3 terrainPosition, TerrainData data, int terrainIndex, int totalTerrains) {
                if (terrainIndex == 0) {
                    lastProcessedCells = 0;
                    lastHolesPainted = 0;
                    lastColliderCount = 0;
                    lastUniqueMeshes = 0;
                }

                // Collect all valid colliders and group by mesh
                var meshColliderGroups = new Dictionary<Mesh, List<MeshCollider>>();
                var nonMeshColliders = new List<Collider>();

                foreach (var obj in objectsWithColliders) {
                    if (obj == null) {
                        continue;
                    }

                    var colliders = obj.GetComponentsInChildren<Collider>();
                    if (colliders.Length == 0) {
                        Log.Important?.Warning($"Object {obj.name} has no colliders");
                        continue;
                    }

                    foreach (var collider in colliders) {
                        if (collider == null || collider is TerrainCollider) {
                            continue;
                        }

                        lastColliderCount++;

                        if (collider is MeshCollider meshCollider && meshCollider.sharedMesh != null) {
                            var mesh = meshCollider.sharedMesh;
                            if (!meshColliderGroups.ContainsKey(mesh)) {
                                meshColliderGroups[mesh] = new List<MeshCollider>();
                            }
                            meshColliderGroups[mesh].Add(meshCollider);
                        } else {
                            nonMeshColliders.Add(collider);
                        }
                    }
                }

                lastUniqueMeshes = meshColliderGroups.Count;
                Log.Important?.Info($"Processing {lastColliderCount} colliders ({lastUniqueMeshes} unique meshes)...");

                int progress = 0;
                int totalWork = meshColliderGroups.Count + nonMeshColliders.Count;

                // Process each unique mesh once, then apply to all instances
                foreach (var kvp in meshColliderGroups) {
                    var mesh = kvp.Key;
                    var colliders = kvp.Value;

                    EditorUtility.DisplayProgressBar("Painting Terrain Holes",
                        $"Processing unique mesh ({progress + 1}/{totalWork}): {mesh.name} ({colliders.Count} instances)",
                        (float)progress / totalWork);

                    // Process all instances of this mesh efficiently
                    ProcessMeshColliderInstances(colliders, holes, holesResolution, terrainSize, terrainPosition, data);

                    progress++;
                }

                // Process non-mesh colliders
                foreach (var collider in nonMeshColliders) {
                    EditorUtility.DisplayProgressBar("Painting Terrain Holes",
                        $"Processing collider ({progress + 1}/{totalWork}): {collider.name}",
                        (float)progress / totalWork);

                    ProcessCollider(collider, holes, holesResolution, terrainSize, terrainPosition);
                    progress++;
                }

                EditorUtility.ClearProgressBar();

                // Apply holes to terrain
                data.SetHoles(0, 0, holes);
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssetIfDirty(data);
            }

            void ProcessTerrainBurst(Terrain currentTerrain, bool[,] holes, int holesResolution, Vector3 terrainSize, Vector3 terrainPosition, TerrainData data, int terrainIndex, int totalTerrains) {
                if (terrainIndex == 0) {
                    lastProcessedCells = 0;
                    lastHolesPainted = 0;
                    lastColliderCount = 0;
                    lastUniqueMeshes = 0;
                }

                // Collect all valid colliders and group by mesh
                var meshColliderGroups = new Dictionary<Mesh, List<MeshCollider>>();

                foreach (var obj in objectsWithColliders) {
                    if (obj == null) {
                        continue;
                    }

                    var colliders = obj.GetComponentsInChildren<MeshCollider>();
                    foreach (var collider in colliders) {
                        if (collider == null || collider.sharedMesh == null) {
                            continue;
                        }

                        lastColliderCount++;

                        var mesh = collider.sharedMesh;
                        if (!meshColliderGroups.ContainsKey(mesh)) {
                            meshColliderGroups[mesh] = new List<MeshCollider>();
                        }
                        meshColliderGroups[mesh].Add(collider);
                    }
                }

                lastUniqueMeshes = meshColliderGroups.Count;
                Log.Important?.Info($"Processing {lastColliderCount} mesh colliders ({lastUniqueMeshes} unique meshes) with Burst...");

                foreach (var kvp in meshColliderGroups) {
                    var mesh = kvp.Key;
                    var colliders = kvp.Value;

                    ProcessMeshColliderInstancesBurst(mesh, colliders, holes, holesResolution, terrainSize, terrainPosition, data);
                }

                // Apply holes to terrain
                data.SetHoles(0, 0, holes);
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssetIfDirty(data);
            }

            void ProcessMeshColliderInstancesBurst(Mesh mesh, List<MeshCollider> colliders, bool[,] holes, int holesResolution, Vector3 terrainSize, Vector3 terrainPosition, TerrainData terrainData) {
                if (colliders.Count == 0) {
                    return;
                }

                // Get or create cached mesh data
                if (!_nativeMeshCache.TryGetValue(mesh, out var meshCache)) {
                    meshCache = new MeshCacheData(mesh, Allocator.Persistent);
                    _nativeMeshCache[mesh] = meshCache;
                }

                // Find combined bounds
                var combinedBounds = colliders[0].bounds;
                foreach (var collider in colliders) {
                    combinedBounds.Encapsulate(collider.bounds);
                }

                // Convert bounds to terrain hole coordinates
                var minX = WorldToHoleCoord(combinedBounds.min.x, terrainPosition.x, terrainSize.x, holesResolution);
                var maxX = WorldToHoleCoord(combinedBounds.max.x, terrainPosition.x, terrainSize.x, holesResolution);
                var minZ = WorldToHoleCoord(combinedBounds.min.z, terrainPosition.z, terrainSize.z, holesResolution);
                var maxZ = WorldToHoleCoord(combinedBounds.max.z, terrainPosition.z, terrainSize.z, holesResolution);

                minX = math.max(0, minX);
                maxX = math.min(holesResolution - 1, maxX);
                minZ = math.max(0, minZ);
                maxZ = math.min(holesResolution - 1, maxZ);

                int width = maxX - minX + 1;
                int height = maxZ - minZ + 1;
                int totalCells = width * height;

                if (totalCells <= 0) {
                    return;
                }

                lastProcessedCells += totalCells;

                // Prepare collider transforms and bounds
                var colliderTransforms = new NativeArray<float4x4>(colliders.Count, Allocator.TempJob);
                var colliderBoundsMin = new NativeArray<float3>(colliders.Count, Allocator.TempJob);
                var colliderBoundsMax = new NativeArray<float3>(colliders.Count, Allocator.TempJob);

                for (int i = 0; i < colliders.Count; i++) {
                    var collider = colliders[i];
                    var transform = collider.transform;
                    colliderTransforms[i] = float4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                    colliderBoundsMin[i] = collider.bounds.min;
                    colliderBoundsMax[i] = collider.bounds.max;
                }

                // Get terrain heights
                var heightmapResolution = terrainData.heightmapResolution;
                var heights = terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution);
                var terrainHeights = new NativeArray<float>(heightmapResolution * heightmapResolution, Allocator.TempJob);

                for (int z = 0; z < heightmapResolution; z++) {
                    for (int x = 0; x < heightmapResolution; x++) {
                        terrainHeights[z * heightmapResolution + x] = heights[z, x];
                    }
                }

                // Prepare output array - initialize with existing holes data
                var outHoles = new NativeArray<bool>(totalCells, Allocator.TempJob);
                for (int i = 0; i < totalCells; i++) {
                    int x = i % width + minX;
                    int z = i / width + minZ;
                    if (z >= 0 && z < holesResolution && x >= 0 && x < holesResolution) {
                        outHoles[i] = holes[z, x];
                    } else {
                        outHoles[i] = true; // Default to no hole
                    }
                }

                // Create and schedule job
                var job = new ProcessTerrainCellsJob {
                    minX = minX,
                    minZ = minZ,
                    maxX = maxX,
                    maxZ = maxZ,
                    holesResolution = holesResolution,
                    terrainSize = terrainSize,
                    terrainPosition = terrainPosition,
                    subSamplesPerCell = subSamplesPerCell,
                    holeShrinkOffset = holeShrinkOffset,
                    invertHoles = invertHoles,
                    vertices = meshCache.vertices,
                    triangles = meshCache.triangles,
                    colliderTransforms = colliderTransforms,
                    colliderBoundsMin = colliderBoundsMin,
                    colliderBoundsMax = colliderBoundsMax,
                    terrainHeights = terrainHeights,
                    heightmapResolution = heightmapResolution,
                    outHoles = outHoles
                };

                var handle = job.Schedule(totalCells, 64);
                handle.Complete();

                // Copy results back to holes array
                for (int i = 0; i < totalCells; i++) {
                    int x = i % width + minX;
                    int z = i / width + minZ;

                    // Check bounds before accessing
                    if (z >= 0 && z < holesResolution && x >= 0 && x < holesResolution) {
                        bool newValue = outHoles[i];
                        if (newValue != holes[z, x]) {
                            holes[z, x] = newValue;
                            if (!newValue) {
                                lastHolesPainted++;
                            }
                        }
                    }
                }

                // Dispose native arrays
                colliderTransforms.Dispose();
                colliderBoundsMin.Dispose();
                colliderBoundsMax.Dispose();
                terrainHeights.Dispose();
                outHoles.Dispose();
            }

            void ProcessMeshColliderInstances(List<MeshCollider> colliders, bool[,] holes, int holesResolution, Vector3 terrainSize, Vector3 terrainPosition, TerrainData terrainData) {
                if (colliders.Count == 0) {
                    return;
                }

                // Get the first collider as reference (they all share the same mesh)
                var firstCollider = colliders[0];
                var mesh = firstCollider.sharedMesh;

                // Find the bounds that encompasses ALL instances
                var combinedBounds = firstCollider.bounds;
                foreach (var collider in colliders) {
                    combinedBounds.Encapsulate(collider.bounds);
                }

                // Convert combined bounds to terrain hole coordinates
                var minX = WorldToHoleCoord(combinedBounds.min.x, terrainPosition.x, terrainSize.x, holesResolution);
                var maxX = WorldToHoleCoord(combinedBounds.max.x, terrainPosition.x, terrainSize.x, holesResolution);
                var minZ = WorldToHoleCoord(combinedBounds.min.z, terrainPosition.z, terrainSize.z, holesResolution);
                var maxZ = WorldToHoleCoord(combinedBounds.max.z, terrainPosition.z, terrainSize.z, holesResolution);

                minX = Mathf.Max(0, minX);
                maxX = Mathf.Min(holesResolution - 1, maxX);
                minZ = Mathf.Max(0, minZ);
                maxZ = Mathf.Min(holesResolution - 1, maxZ);

                var cellSize = terrainSize.x / (holesResolution - 1);

                // Test each terrain cell against ALL instances
                for (int x = minX; x <= maxX; x++) {
                    for (int z = minZ; z <= maxZ; z++) {
                        lastProcessedCells++;

                        // Test multiple sub-samples within this cell
                        int insideCount = 0;
                        int totalSamples = subSamplesPerCell * subSamplesPerCell;

                        for (int sx = 0; sx < subSamplesPerCell; sx++) {
                            for (int sz = 0; sz < subSamplesPerCell; sz++) {
                                float offsetX = (sx + 0.5f) / subSamplesPerCell - 0.5f;
                                float offsetZ = (sz + 0.5f) / subSamplesPerCell - 0.5f;

                                var baseWorldPos = HoleCoordToWorldWithTerrainHeight(x, z, terrainPosition, terrainSize, holesResolution, terrainData);
                                var worldPos = baseWorldPos + new Vector3(offsetX * cellSize, 0, offsetZ * cellSize);

                                var normalizedX = (worldPos.x - terrainPosition.x) / terrainSize.x;
                                var normalizedZ = (worldPos.z - terrainPosition.z) / terrainSize.z;
                                if (normalizedX >= 0 && normalizedX <= 1 && normalizedZ >= 0 && normalizedZ <= 1) {
                                    var terrainHeight = terrainData.GetInterpolatedHeight(normalizedZ, normalizedX);
                                    worldPos.y = terrainPosition.y + terrainHeight;
                                }

                                // Test this point against ALL instances of this mesh
                                bool isInsideAny = false;
                                foreach (var collider in colliders) {
                                    if (IsPointInsideColliderVolume(collider, worldPos)) {
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
                            if (holes[z, x] != newValue) {
                                holes[z, x] = newValue;
                                lastHolesPainted++;
                            }
                        }
                    }
                }
            }

            void ProcessCollider(Collider collider, bool[,] holes, int holesResolution, Vector3 terrainSize, Vector3 terrainPosition) {
                // Get collider bounds to limit search area
                var bounds = collider.bounds;

                // Convert bounds to terrain hole coordinates
                var minX = WorldToHoleCoord(bounds.min.x, terrainPosition.x, terrainSize.x, holesResolution);
                var maxX = WorldToHoleCoord(bounds.max.x, terrainPosition.x, terrainSize.x, holesResolution);
                var minZ = WorldToHoleCoord(bounds.min.z, terrainPosition.z, terrainSize.z, holesResolution);
                var maxZ = WorldToHoleCoord(bounds.max.z, terrainPosition.z, terrainSize.z, holesResolution);

                // Clamp to valid range
                minX = Mathf.Max(0, minX);
                maxX = Mathf.Min(holesResolution - 1, maxX);
                minZ = Mathf.Max(0, minZ);
                maxZ = Mathf.Min(holesResolution - 1, maxZ);

                var data = terrain.terrainData;
                var width = maxX - minX + 1;
                var arrayHeight = maxZ - minZ + 1;
                var tempHoles = new bool[width, arrayHeight];

                // Calculate cell size in world units
                var cellSize = terrainSize.x / (holesResolution - 1);

                // Sample every point with sub-sampling for better precision
                for (int x = minX; x <= maxX; x++) {
                    for (int z = minZ; z <= maxZ; z++) {
                        lastProcessedCells++;

                        // Test multiple points within this cell
                        int insideCount = 0;
                        int totalSamples = subSamplesPerCell * subSamplesPerCell;

                        for (int sx = 0; sx < subSamplesPerCell; sx++) {
                            for (int sz = 0; sz < subSamplesPerCell; sz++) {
                                // Calculate sub-sample offset within the cell
                                float offsetX = (sx + 0.5f) / subSamplesPerCell - 0.5f;
                                float offsetZ = (sz + 0.5f) / subSamplesPerCell - 0.5f;

                                // Get world position at terrain surface height with offset
                                var baseWorldPos = HoleCoordToWorldWithTerrainHeight(x, z, terrainPosition, terrainSize, holesResolution, data);
                                var worldPos = baseWorldPos + new Vector3(offsetX * cellSize, 0, offsetZ * cellSize);

                                // Update Y to terrain height at this offset position
                                var normalizedX = (worldPos.x - terrainPosition.x) / terrainSize.x;
                                var normalizedZ = (worldPos.z - terrainPosition.z) / terrainSize.z;
                                if (normalizedX >= 0 && normalizedX <= 1 && normalizedZ >= 0 && normalizedZ <= 1) {
                                    var terrainHeight = data.GetInterpolatedHeight(normalizedZ, normalizedX);
                                    worldPos.y = terrainPosition.y + terrainHeight;
                                }

                                // Check if this sub-sample point is inside the collider volume
                                if (IsPointInsideColliderVolume(collider, worldPos)) {
                                    insideCount++;
                                }
                            }
                        }

                        // If more than half the sub-samples are inside, mark this cell as a hole
                        if (insideCount > totalSamples / 2) {
                            tempHoles[x - minX, z - minZ] = true;
                        }
                    }
                }

                // Copy to final holes array
                for (int x = 0; x < width; x++) {
                    for (int z = 0; z < arrayHeight; z++) {
                        if (tempHoles[x, z]) {
                            bool newValue = invertHoles ? true : false;
                            int actualX = minX + x;
                            int actualZ = minZ + z;
                            if (holes[actualZ, actualX] != newValue) {
                                holes[actualZ, actualX] = newValue;
                                lastHolesPainted++;
                            }
                        }
                    }
                }
            }

            bool IsPointInsideColliderVolume(Collider collider, Vector3 point) {
                // Apply shrink offset by moving point outward from collider center
                if (holeShrinkOffset > 0.001f) {
                    var colliderCenter = collider.bounds.center;
                    var directionFromCenter = (point - colliderCenter).normalized;
                    point = point + directionFromCenter * holeShrinkOffset;
                }

                // First check: if point is outside bounds, it's definitely outside
                if (!collider.bounds.Contains(point)) {
                    return false;
                }

                // Try to get the mesh from the collider
                Mesh mesh = null;
                Transform transform = collider.transform;

                if (collider is MeshCollider meshCollider) {
                    mesh = meshCollider.sharedMesh;
                } else {
                    // For other collider types, fall back to raycast method
                    return IsPointInsideColliderVolumeRaycast(collider, point);
                }

                if (mesh == null) {
                    return IsPointInsideColliderVolumeRaycast(collider, point);
                }

                // Use ray-triangle intersection counting (odd-even rule)
                // Cast ray from point in arbitrary direction and count triangle intersections
                var localPoint = transform.InverseTransformPoint(point);
                var rayDirection = Vector3.right; // Arbitrary direction

                int intersectionCount = CountRayTriangleIntersections(mesh, localPoint, rayDirection);

                // Odd number of intersections = inside
                return intersectionCount % 2 == 1;
            }

            bool IsPointInsideColliderVolumeRaycast(Collider collider, Vector3 point) {
                // Fallback raycast method for non-mesh colliders
                var bounds = collider.bounds;
                var testDirections = new[] {
                    Vector3.up,
                    Vector3.down,
                    Vector3.left,
                    Vector3.right,
                    Vector3.forward,
                    Vector3.back
                };

                int insideCount = 0;

                foreach (var direction in testDirections) {
                    var rayOrigin = point - direction * (bounds.extents.magnitude * 2f);
                    var rayDistance = bounds.extents.magnitude * 4f;
                    var hits = Physics.RaycastAll(rayOrigin, direction, rayDistance);
                    float distanceToPoint = (bounds.extents.magnitude * 2f);

                    foreach (var hit in hits) {
                        if (hit.collider == collider && hit.distance < distanceToPoint - 0.01f) {
                            insideCount++;
                            break;
                        }
                    }
                }

                return insideCount >= 3;
            }

            int CountRayTriangleIntersections(Mesh mesh, Vector3 localRayOrigin, Vector3 localRayDirection) {
                // Get or create cached mesh data
                if (!_meshCache.TryGetValue(mesh, out var cache)) {
                    cache = new MeshCache(mesh);
                    _meshCache[mesh] = cache;
                }

                var vertices = cache.vertices;
                var triangles = cache.triangles;
                int intersectionCount = 0;

                for (int i = 0; i < triangles.Length; i += 3) {
                    var v0 = vertices[triangles[i]];
                    var v1 = vertices[triangles[i + 1]];
                    var v2 = vertices[triangles[i + 2]];

                    if (RayIntersectsTriangle(localRayOrigin, localRayDirection, v0, v1, v2, out float t)) {
                        if (t > 0.0001f) { // Only count forward intersections
                            intersectionCount++;
                        }
                    }
                }

                return intersectionCount;
            }

            bool RayIntersectsTriangle(Vector3 rayOrigin, Vector3 rayDirection, Vector3 v0, Vector3 v1, Vector3 v2, out float t) {
                // Möller–Trumbore ray-triangle intersection algorithm
                t = 0;
                const float epsilon = 0.0000001f;

                var edge1 = v1 - v0;
                var edge2 = v2 - v0;
                var h = Vector3.Cross(rayDirection, edge2);
                var a = Vector3.Dot(edge1, h);

                if (a > -epsilon && a < epsilon) {
                    return false; // Ray is parallel to triangle
                }

                var f = 1f / a;
                var s = rayOrigin - v0;
                var u = f * Vector3.Dot(s, h);

                if (u < 0f || u > 1f) {
                    return false;
                }

                var q = Vector3.Cross(s, edge1);
                var v = f * Vector3.Dot(rayDirection, q);

                if (v < 0f || u + v > 1f) {
                    return false;
                }

                t = f * Vector3.Dot(edge2, q);
                return t > epsilon;
            }

            int WorldToHoleCoord(float worldCoord, float terrainWorldCoord, float terrainSize, int holesResolution) {
                var normalized = (worldCoord - terrainWorldCoord) / terrainSize;
                return Mathf.RoundToInt(normalized * (holesResolution - 1));
            }

            Vector3 HoleCoordToWorld(int holeCoord, int holeCoordZ, Vector3 terrainPosition, Vector3 terrainSize, int holesResolution) {
                var normalizedX = (float)holeCoord / (holesResolution - 1);
                var normalizedZ = (float)holeCoordZ / (holesResolution - 1);

                return new Vector3(
                    terrainPosition.x + normalizedX * terrainSize.x,
                    terrainPosition.y + terrainSize.y * 0.5f, // Middle of terrain height
                    terrainPosition.z + normalizedZ * terrainSize.z
                );
            }

            Vector3 HoleCoordToWorldWithTerrainHeight(int holeCoordX, int holeCoordZ, Vector3 terrainPosition, Vector3 terrainSize, int holesResolution, TerrainData data) {
                var normalizedX = (float)holeCoordX / (holesResolution - 1);
                var normalizedZ = (float)holeCoordZ / (holesResolution - 1);

                // Get actual terrain height at this position
                var height = data.GetInterpolatedHeight(normalizedZ, normalizedX);

                return new Vector3(
                    terrainPosition.x + normalizedX * terrainSize.x,
                    terrainPosition.y + height,
                    terrainPosition.z + normalizedZ * terrainSize.z
                );
            }

            [Button("Clear All Holes")]
            void ClearAllHoles() {
                if (terrain == null) {
                    Log.Important?.Warning("Terrain is not assigned");
                    return;
                }

                var data = terrain.terrainData;
                var holesResolution = data.holesResolution;
                var holes = new bool[holesResolution, holesResolution];

                // Set all to true (no holes)
                for (int x = 0; x < holesResolution; x++) {
                    for (int z = 0; z < holesResolution; z++) {
                        holes[x, z] = true;
                    }
                }

                data.SetHoles(0, 0, holes);
                EditorUtility.SetDirty(data);
                AssetDatabase.SaveAssetIfDirty(data);

                Log.Important?.Info("Cleared all terrain holes");
            }

            [Button("Clear Mesh Cache")]
            void ClearCache() {
                _meshCache.Clear();

                foreach (var cache in _nativeMeshCache.Values) {
                    cache.Dispose();
                }
                _nativeMeshCache.Clear();

                Log.Important?.Info("Mesh cache cleared");
            }

            class MeshCache {
                public Vector3[] vertices;
                public int[] triangles;

                public MeshCache(Mesh mesh) {
                    vertices = mesh.vertices;
                    triangles = mesh.triangles;
                }
            }
        }
    }
}