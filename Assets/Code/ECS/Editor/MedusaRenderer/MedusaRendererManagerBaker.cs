using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.CommonInterfaces;
using Awaken.ECS.MedusaRenderer;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Editor.Scenes;
using Awaken.Utility.Extensions;
using Awaken.Utility.Graphics;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.ECS.Editor.MedusaRenderer {
    public class MedusaRendererManagerBaker : SceneProcessor {
        public override int callbackOrder => ProcessSceneOrder.MedusaBuild;
        public override bool canProcessSceneInIsolation => true;

        public static void ClearMedusaLibraryAssets() {
            var directoryPath = MedusaPersistence.BakingDirectoryPath;
            if (Directory.Exists(directoryPath)) {
                Directory.Delete(directoryPath, true);
            }
        }

#if !SCENES_PROCESSED
        [InitializeOnLoadMethod]
        static void PlaymodeWatcher() {
            EditorApplication.playModeStateChanged += state => {
                if (state == PlayModeStateChange.EnteredEditMode) {
                    ClearMedusaLibraryAssets();
                }
            };
        }
#endif

        protected override void OnProcessScene(Scene scene, bool processingInPlaymode) {
            GameObject[] roots = scene.GetRootGameObjects();
            MedusaRendererManager manager = null;

            var toConvert = new List<MedusaRendererPrefab>();
            foreach (GameObject root in roots.Where(static r => r.activeSelf)) {
                toConvert.AddRange(root.GetComponentsInChildren<MedusaRendererPrefab>());
                manager ??= root.GetComponentInChildren<MedusaRendererManager>();
            }

            if (toConvert.Count == 0) {
                return;
            }

            var lodGroups = toConvert.Select(static p => p.GetComponent<LODGroup>()).ToArray();

            if (manager) {
                Bake(new MedusaRendererManager.EditorAccess(manager), scene, lodGroups);
                EditorUtility.SetDirty(manager);
            } else {
                Debug.LogError($"For scene: {scene.name} MedusaRendererManager is not found, so it won't render medusa marked objects");
            }

            for (var i = 0; i < lodGroups.Length; i++) {
                var lods = lodGroups[i].GetLODs();
                for (int j = 0; j < lods.Length; j++) {
                    LOD lod = lods[j];
                    for (int k = 0; k < lod.renderers.Length; k++) {
                        UnityEngine.Renderer lodRenderer = lod.renderers[k];
                        if (lodRenderer is not MeshRenderer meshRenderer) {
                            continue;
                        }
                        var rendererTransform = meshRenderer.transform;
                        var meshFilter = meshRenderer.GetComponent<MeshFilter>();

                        if (rendererTransform.childCount == 0 && rendererTransform.GetComponents<Component>().Length == 3) {
                            UnityEngine.Object.DestroyImmediate(rendererTransform.gameObject);
                        } else {
                            UnityEngine.Object.DestroyImmediate(meshRenderer);
                            UnityEngine.Object.DestroyImmediate(meshFilter);
                        }
                    }
                }

                var currentTransform = lodGroups[i].transform;
                if (currentTransform.childCount == 0 && currentTransform.GetComponents<Component>().Length == 3) {
                    UnityEngine.Object.DestroyImmediate(currentTransform.gameObject);
                } else {
                    UnityEngine.Object.DestroyImmediate(toConvert[i]);
                    UnityEngine.Object.DestroyImmediate(lodGroups[i]);
                }
            }
        }

        void Bake(MedusaRendererManager.EditorAccess manager, Scene scene, LODGroup[] lodGroups) {
            // --- Transforms
            manager.TransformsCount = lodGroups.Length;
            var uCount = (uint)lodGroups.Length;
            var xs = new UnsafeArray<float>(uCount, Allocator.Temp);
            var ys = new UnsafeArray<float>(uCount, Allocator.Temp);
            var zs = new UnsafeArray<float>(uCount, Allocator.Temp);
            var radii = new UnsafeArray<float>(uCount, Allocator.Temp);
            var lodDistancesSq0 = new UnsafeArray<float>(uCount, Allocator.Temp);
            var lodDistancesSq1 = new UnsafeArray<float>(uCount, Allocator.Temp);
            var lodDistancesSq2 = new UnsafeArray<float>(uCount, Allocator.Temp);
            var lodDistancesSq3 = new UnsafeArray<float>(uCount, Allocator.Temp);
            var lodDistancesSq4 = new UnsafeArray<float>(uCount, Allocator.Temp);
            var lodDistancesSq5 = new UnsafeArray<float>(uCount, Allocator.Temp);
            var lodDistancesSq6 = new UnsafeArray<float>(uCount, Allocator.Temp);
            var lodDistancesSq7 = new UnsafeArray<float>(uCount, Allocator.Temp);
            var lastLodMasks = new UnsafeArray<byte>(uCount, Allocator.Temp);

            var packedMatrices = new UnsafeArray<PackedMatrix>(uCount, Allocator.Temp);
            var packedMatricesInv = new UnsafeArray<PackedMatrix>(uCount, Allocator.Temp);
            var radiusPerMesh = new Dictionary<Mesh, float>();

            Span<float> lodDistancesSq = stackalloc float[8];
            for (uint i = 0; i < lodGroups.Length; ++i) {
                var lodTransform = lodGroups[i].transform;
                var localToWorld = lodTransform.localToWorldMatrix;
                var lods = lodGroups[i].GetLODs();
                var worldSize = LodUtils.GetWorldSpaceScale(lodTransform) * lodGroups[i].size;
                var center = localToWorld.MultiplyPoint(lodGroups[i].localReferencePoint);
                xs[i] = center.x;
                ys[i] = center.y;
                zs[i] = center.z;
                var radius = CalculateWorldRadius(radiusPerMesh, lodGroups[i], lodGroups[i].localReferencePoint, lods);
                radii[i] = radius;

                for (var j = 0; j < lods.Length; ++j) {
                    var d = worldSize / lods[j].screenRelativeTransitionHeight;
                    lodDistancesSq[j] = math.square(d);
                }
                for (var j = lods.Length; j < 8; ++j) {
                    lodDistancesSq[j] = float.PositiveInfinity;
                }

                lodDistancesSq0[i] = lodDistancesSq[0];
                lodDistancesSq1[i] = lodDistancesSq[1];
                lodDistancesSq2[i] = lodDistancesSq[2];
                lodDistancesSq3[i] = lodDistancesSq[3];
                lodDistancesSq4[i] = lodDistancesSq[4];
                lodDistancesSq5[i] = lodDistancesSq[5];
                lodDistancesSq6[i] = lodDistancesSq[6];
                lodDistancesSq7[i] = lodDistancesSq[7];
                lastLodMasks[i] = (byte)(1 << lods.Length - 1);

                packedMatrices[i] = new PackedMatrix(localToWorld);
                // maybe we should do it in job?
                packedMatricesInv[i] = packedMatrices[i].Inverse();
            }

            var medusaBasePath = Path.Combine(MedusaPersistence.BakingDirectoryPath, scene.name);
            Directory.CreateDirectory(medusaBasePath);

            var transformsBufferPath = MedusaPersistence.TransformsPath(medusaBasePath);
            var transformsBufferStream = new FileStream(transformsBufferPath, FileMode.Create);
            transformsBufferStream.Write(xs.AsByteSpan());
            transformsBufferStream.Write(ys.AsByteSpan());
            transformsBufferStream.Write(zs.AsByteSpan());
            transformsBufferStream.Write(radii.AsByteSpan());
            transformsBufferStream.Write(lodDistancesSq0.AsByteSpan());
            transformsBufferStream.Write(lodDistancesSq1.AsByteSpan());
            transformsBufferStream.Write(lodDistancesSq2.AsByteSpan());
            transformsBufferStream.Write(lodDistancesSq3.AsByteSpan());
            transformsBufferStream.Write(lodDistancesSq4.AsByteSpan());
            transformsBufferStream.Write(lodDistancesSq5.AsByteSpan());
            transformsBufferStream.Write(lodDistancesSq6.AsByteSpan());
            transformsBufferStream.Write(lodDistancesSq7.AsByteSpan());
            transformsBufferStream.Write(lastLodMasks.AsByteSpan());
            transformsBufferStream.Dispose();

            var matricesPath = MedusaPersistence.MatricesPath(medusaBasePath);
            var matricesBufferStream = new FileStream(matricesPath, FileMode.Create);
            matricesBufferStream.Write(packedMatrices.AsByteSpan());
            matricesBufferStream.Write(packedMatricesInv.AsByteSpan());
            matricesBufferStream.Dispose();

            xs.Dispose();
            ys.Dispose();
            zs.Dispose();
            radii.Dispose();
            lodDistancesSq0.Dispose();
            lodDistancesSq1.Dispose();
            lodDistancesSq2.Dispose();
            lodDistancesSq3.Dispose();
            lodDistancesSq4.Dispose();
            lodDistancesSq5.Dispose();
            lodDistancesSq6.Dispose();
            lodDistancesSq7.Dispose();
            lastLodMasks.Dispose();
            packedMatrices.Dispose();
            packedMatricesInv.Dispose();

            // --- Renderers
            var renderers = new List<Awaken.ECS.MedusaRenderer.Renderer>();
            var transformIndices = new NativeList<NativeList<uint>>(manager.TransformsCount, Allocator.Temp);
            var reciprocalUvDistributions = new NativeList<NativeList<float>>(manager.TransformsCount, Allocator.Temp);

            var rendererDatumBuffer = new List<RenderDatum>(32);

            manager.AllRenderersCount = 0;
            manager.AllUvDistributionsCount = 0;
            for (int i = 0; i < lodGroups.Length; i++) {
                var scaleSq = math.square(lodGroups[i].transform.lossyScale.Max());
                var lods = lodGroups[i].GetLODs();
                for (int j = 0; j < lods.Length; j++) {
                    LOD lod = lods[j];
                    var renderer = new Awaken.ECS.MedusaRenderer.Renderer {
                        lodMask = (byte)(1 << j),
                    };
                    for (int k = 0; k < lod.renderers.Length; k++) {
                        UnityEngine.Renderer lodRenderer = lod.renderers[k];
                        if (lodRenderer is not MeshRenderer meshRenderer) {
                            continue;
                        }
                        var materials = meshRenderer.sharedMaterials;
                        var mesh = meshRenderer.GetComponent<MeshFilter>().sharedMesh;
                        for (ushort l = 0; l < materials.Length; l++) {
                            var renderDatum = new RenderDatum {
                                mesh = mesh,
                                material = materials[l],
                                subMeshIndex = l,
                            };
                            rendererDatumBuffer.Add(renderDatum);
                        }
                    }
                    renderer.renderData = rendererDatumBuffer;
                    var rendererIndex = renderers.IndexOf(renderer);
                    if (Hint.Unlikely(rendererIndex == -1)) {
                        rendererIndex = renderers.Count;
                        renderer.renderData = rendererDatumBuffer.ToList(); // Do copy
                        renderers.Add(renderer);
                        transformIndices.Add(new NativeList<uint>(lodGroups.Length, Allocator.Temp));
                        reciprocalUvDistributions.Add(new NativeList<float>(lodGroups.Length, Allocator.Temp));
                    }

                    renderer = renderers[rendererIndex];
                    ++renderer.instancesCount;
                    renderers[rendererIndex] = renderer;

                    transformIndices[rendererIndex].Add((uint)i);
                    for (int k = 0; k < rendererDatumBuffer.Count; k++) {
                        var mesh = rendererDatumBuffer[k].mesh;
                        var uvDistribution = mesh.GetUVDistributionMetric(0);
                        reciprocalUvDistributions[rendererIndex].Add(math.rcp(uvDistribution * scaleSq));
                        manager.AllUvDistributionsCount++;
                    }
                    rendererDatumBuffer.Clear();
                    ++manager.AllRenderersCount;
                }
            }

            var renderersPath = MedusaPersistence.RenderersPath(medusaBasePath);
            var renderersBufferStream = new FileStream(renderersPath, FileMode.Create);
            for (int i = 0; i < transformIndices.Length; i++) {
                renderersBufferStream.Write(transformIndices[i].AsByteSpan());
            }
            renderersBufferStream.Dispose();

            var reciprocalUvDistributionsPath = MedusaPersistence.ReciprocalUvDistributions(medusaBasePath);
            var reciprocalUvDistributionsBufferStream = new FileStream(reciprocalUvDistributionsPath, FileMode.Create);
            for (int i = 0; i < reciprocalUvDistributions.Length; i++) {
                reciprocalUvDistributionsBufferStream.Write(reciprocalUvDistributions[i].AsByteSpan());
            }
            reciprocalUvDistributionsBufferStream.Dispose();

            manager.Renderers = renderers.ToArray();
            // Cleanup
            for (int i = 0; i < transformIndices.Length; i++) {
                transformIndices[i].Dispose();
                reciprocalUvDistributions[i].Dispose();
            }
            transformIndices.Dispose();
            reciprocalUvDistributions.Dispose();
        }

        float CalculateWorldRadius(Dictionary<Mesh, float> radiusPerMesh, LODGroup lodGroup, Vector3 lodGroupCenter, LOD[] lods) {
            var radius = 0f;
            for (int i = 0; i < lods.Length; i++) {
                var renderers = lods[i].renderers;
                for (int j = 0; j < renderers.Length; j++) {
                    if (renderers[j] is not MeshRenderer meshRenderer) {
                        continue;
                    }
                    var meshFilter = meshRenderer.GetComponent<MeshFilter>();
                    var mesh = meshFilter.sharedMesh;
                    if (mesh == null) {
                        Log.Minor?.Error($"Mesh is null for {meshRenderer.name} in {lodGroup.name}", lodGroup);
                        continue;
                    }
                    var meshRadius = CalculateWorldRadius(radiusPerMesh, lodGroup, lodGroupCenter, meshFilter, mesh);
                    radius = math.max(radius, meshRadius);
                }
            }
            return radius;
        }

        float CalculateWorldRadius(Dictionary<Mesh, float> radiusPerMesh, LODGroup lodGroup, Vector3 lodGroupCenter,
            MeshFilter meshFilter, Mesh mesh) {
            if (!radiusPerMesh.TryGetValue(mesh, out var radius)) {
                var meshLocalToLodLocal = lodGroup.transform.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;
                var lodLocalToMeshLocal = meshLocalToLodLocal.inverse;
                var meshCenter = lodLocalToMeshLocal.MultiplyPoint(lodGroupCenter);

                using var meshDataArray = MeshUtility.AcquireReadOnlyMeshData(mesh);
                var meshData = meshDataArray[0];
                var vertices = new NativeArray<Vector3>(meshData.vertexCount, Allocator.Temp);
                meshData.GetVertices(vertices);

                var maxDistanceSq = 0f;
                foreach (var vertex in vertices) {
                    var distance = math.distancesq(vertex, meshCenter);
                    if (distance > maxDistanceSq) {
                        maxDistanceSq = distance;
                    }
                }
                vertices.Dispose();
                radius = math.sqrt(maxDistanceSq);

                radiusPerMesh[mesh] = radius;
            }

            return radius * meshFilter.transform.lossyScale.Max();
        }

        [MenuItem("TG/Assets/Medusa/Log invalid")]
        static void LogInvalid() {
            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();

            var toConvert = new List<MedusaRendererPrefab>();
            foreach (GameObject root in roots.Where(static r => r.activeSelf)) {
                toConvert.AddRange(root.GetComponentsInChildren<MedusaRendererPrefab>());
            }

            if (toConvert.Count == 0) {
                return;
            }

            var lodGroups = toConvert.Select(static p => p.GetComponent<LODGroup>()).ToArray();
            for (int i = 0; i < lodGroups.Length; i++) {
                var lodGroupTransform = lodGroups[i].transform;
                var position = lodGroupTransform.position;
                var rotation = lodGroupTransform.rotation;
                var euler = rotation.eulerAngles;
                var scale = lodGroupTransform.lossyScale;
                var lods = lodGroups[i].GetLODs();
                for (int j = 0; j < lods.Length; j++) {
                    LOD lod = lods[j];
                    for (int k = 0; k < lod.renderers.Length; k++) {
                        UnityEngine.Renderer lodRenderer = lod.renderers[k];
                        if (lodRenderer is not MeshRenderer meshRenderer) {
                            continue;
                        }
                        var meshTransform = meshRenderer.transform;
                        if (meshTransform.LocalTransformToMatrix() != Matrix4x4.identity) {
                            Log.Important?.Error($"Moved mesh for {meshRenderer.name} in {lodGroups[i].name}", lodGroupTransform);
                            continue;
                        }
                        var meshPosition = meshTransform.position;
                        var meshRotation = meshTransform.rotation;
                        var meshEuler = meshRotation.eulerAngles;
                        var meshScale = meshTransform.lossyScale;
                        if (Vector3.Distance(position, meshPosition) > 0.1f ||
                            Mathf.DeltaAngle(euler.x, meshEuler.x) > 0.1f ||
                            Mathf.DeltaAngle(euler.y, meshEuler.y) > 0.1f ||
                            Mathf.DeltaAngle(euler.z, meshEuler.z) > 0.1f ||
                            Vector3.Distance(scale, meshScale) > 0.1f) {
                            Log.Important?.Error($"Invalid transform for {meshRenderer.name} in {lodGroups[i].name}", lodGroupTransform);
                        }
                    }
                }
            }
        }

        [MenuItem("TG/Assets/Medusa/Fix invalid")]
        static void FixInvalid() {
            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();

            var toConvert = new List<MedusaRendererPrefab>();
            foreach (GameObject root in roots.Where(static r => r.activeSelf)) {
                toConvert.AddRange(root.GetComponentsInChildren<MedusaRendererPrefab>());
            }

            if (toConvert.Count == 0) {
                return;
            }

            var lodGroups = toConvert.Select(static p => p.GetComponent<LODGroup>()).ToArray();
            for (int i = 0; i < lodGroups.Length; i++) {
                var lodGroupTransform = lodGroups[i].transform;
                var position = lodGroupTransform.position;
                var rotation = lodGroupTransform.rotation;
                var euler = rotation.eulerAngles;
                var scale = lodGroupTransform.lossyScale;
                var lods = lodGroups[i].GetLODs();
                for (int j = 0; j < lods.Length; j++) {
                    LOD lod = lods[j];
                    for (int k = 0; k < lod.renderers.Length; k++) {
                        UnityEngine.Renderer lodRenderer = lod.renderers[k];
                        if (lodRenderer is not MeshRenderer meshRenderer) {
                            continue;
                        }
                        var meshTransform = meshRenderer.transform;
                        if (meshTransform.LocalTransformToMatrix() != Matrix4x4.identity) {
                            Log.Important?.Info($"Fixed {meshRenderer.name} in {lodGroups[i].name}", lodGroupTransform);
                            meshTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                            meshTransform.localScale = Vector3.one;
                        }
                        var meshPosition = meshTransform.position;
                        var meshRotation = meshTransform.rotation;
                        var meshEuler = meshRotation.eulerAngles;
                        var meshScale = meshTransform.lossyScale;
                        if (Vector3.Distance(position, meshPosition) > 0.1f ||
                            Mathf.DeltaAngle(euler.x, meshEuler.x) > 0.1f ||
                            Mathf.DeltaAngle(euler.y, meshEuler.y) > 0.1f ||
                            Mathf.DeltaAngle(euler.z, meshEuler.z) > 0.1f ||
                            Vector3.Distance(scale, meshScale) > 0.1f) {
                            Debug.LogError($"Fixed {lodGroups[i].name}", lodGroupTransform);
                            FixupTransform(meshTransform.parent, lodGroupTransform);
                        }
                    }
                }
            }
        }

        static void FixupTransform(Transform current, Transform upTo) {
            if (current == upTo) {
                return;
            }
            var parent = current.parent;
            current.GetPositionAndRotation(out var position, out var rotation);
            var localScale = current.localScale;
            var parentScale = parent.localScale;
            parentScale.x *= localScale.x;
            parentScale.y *= localScale.y;
            parentScale.z *= localScale.z;
            parent.localScale = parentScale;
            parent.rotation = rotation;
            parent.position = position;

            current.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            current.localScale = Vector3.one;
            FixupTransform(parent, upTo);
        }
    }
}
