using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.Utility.Debugging;
using Awaken.Utility.Maths;
using Unity.Mathematics;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.ECS.Editor.DrakeRenderer {
    public static class DrakeEditorHelpers {
        const float CullingPercentage = 0.025f;
        const float NoShadowsPercentage = 0.04f;
        // === Drake lod group
        public static bool Bake(DrakeToBake drakeToBake) {
            var lodGroup = drakeToBake.GetComponent<LODGroup>();
            var meshRenderer = drakeToBake.GetComponent<MeshRenderer>();
            if (!lodGroup && !meshRenderer) {
                return false;
            }
            if (lodGroup) {
                var drakeLodGroup = drakeToBake.gameObject.AddComponent<DrakeLodGroup>();
                if (DrakeEditorHelpers.Bake(drakeLodGroup, lodGroup)) {
                    Object.DestroyImmediate(drakeToBake);
                    DrakeLodGroup.OnAddedDrakeLodGroup(drakeLodGroup);
                    return true;
                } else {
                    Object.DestroyImmediate(drakeLodGroup);
                    return false;
                }
            }
            if (meshRenderer) {
                var drakeLodGroup = drakeToBake.gameObject.AddComponent<DrakeLodGroup>();
                if (DrakeEditorHelpers.Bake(drakeLodGroup, meshRenderer)) {
                    Object.DestroyImmediate(drakeToBake);
                    DrakeLodGroup.OnAddedDrakeLodGroup(drakeLodGroup);
                    return true;
                } else {
                    Object.DestroyImmediate(drakeLodGroup);
                    return false;
                }
            }
            return false;
        }

        public static bool Bake(DrakeLodGroup drakeLodGroup, MeshRenderer meshRenderer) {
            var lodGroup = drakeLodGroup.gameObject.AddComponent<LODGroup>();

            var lods = new List<LOD>(2);

            lods.Add(new LOD(CullingPercentage, new Renderer[] { meshRenderer }));

            GameObject noShadowRendererGo = null;
            if (meshRenderer.shadowCastingMode == ShadowCastingMode.On) {
                var noShadowRenderer = CreateNoShadowRenderers(lodGroup, new MeshRenderer[] { meshRenderer })[0];
                noShadowRendererGo = noShadowRenderer.gameObject;
                var previousLod = lods[0];
                previousLod.screenRelativeTransitionHeight = NoShadowsPercentage;
                lods[0] = previousLod;
                lods.Add(new LOD(CullingPercentage, new Renderer[] { noShadowRenderer }));
            }

            lodGroup.SetLODs(lods.ToArray());
            lodGroup.RecalculateBounds();

            if (!Bake(drakeLodGroup, lodGroup)) {
                Object.DestroyImmediate(lodGroup);
                if (noShadowRendererGo) {
                    Object.DestroyImmediate(noShadowRendererGo);
                }
                return false;
            }
            return true;
        }

        public static bool Bake(DrakeLodGroup drakeLodGroup, LODGroup lodGroup) {
            var children = new HashSet<DrakeMeshRenderer>(8);
            var lods = lodGroup.GetLODs();

            if (lods.Length == 1) {
                ref var lod0 = ref lods[0];
                if (lod0.renderers.OfType<MeshRenderer>().Any(r => r.shadowCastingMode != ShadowCastingMode.Off)) {
                    var originalTransition = lod0.screenRelativeTransitionHeight;
                    var lod0Transition = math.max(NoShadowsPercentage, originalTransition + 0.01f);
                    lod0.screenRelativeTransitionHeight = lod0Transition;

                    var lod1Renderers = CreateNoShadowRenderers(lodGroup, lod0.renderers.OfType<MeshRenderer>().ToArray());
                    var lod1 = new LOD(originalTransition, lod1Renderers);

                    Array.Resize(ref lods, 2);
                    lods[0] = lod0;
                    lods[1] = lod1;
                    lodGroup.SetLODs(lods);
                }
            }

            foreach (var lod in lods) {
                foreach (var renderer in lod.renderers) {
                    if (renderer is not MeshRenderer meshRenderer || !meshRenderer) {
                        continue;
                    }
                    var meshRendererGameObject = meshRenderer.gameObject;
                    var drakeMeshRenderer = lodGroup.gameObject.AddComponent<DrakeMeshRenderer>();
                    if (DrakeMeshRendererEditor.Bake(drakeMeshRenderer, meshRenderer)) {
                        children.Add(drakeMeshRenderer);
                        DestroyEmpty(meshRendererGameObject);
                    } else {
                        Log.Important?.Error($"Cannot bake renderer {meshRendererGameObject.name}.");
                        Object.DestroyImmediate(drakeMeshRenderer);
                    }
                }
            }

            if (children.Count > 0) {
                drakeLodGroup.Setup(lodGroup, children.ToArray());
                drakeLodGroup.BakeStatic();
                EditorUtility.SetDirty(drakeLodGroup);
                return true;
            } else {
                Log.Important?.Error($"Baking was not successful, no renderers were found.");
                Object.DestroyImmediate(drakeLodGroup);
                return false;
            }
        }

        public static void Unbake(DrakeLodGroup drakeLodGroup) {
            var gameObject = drakeLodGroup.gameObject;
            SpawnAuthoring(drakeLodGroup);
            foreach (var renderer in drakeLodGroup.Renderers) {
                Object.DestroyImmediate(renderer);
            }
            drakeLodGroup.ClearData();
            DrakeLodGroup.OnRemovedDrakeLodGroup?.Invoke(drakeLodGroup);
            Object.DestroyImmediate(drakeLodGroup);
            gameObject.AddComponent<DrakeToBake>();
            EditorUtility.SetDirty(gameObject);
        }

        public static void SpawnAuthoring(DrakeLodGroup drakeLodGroup, GameObject unbakeTarget = null) {
            var lodsSerializedData = drakeLodGroup.LodGroupSerializableData;
            var lodSize = drakeLodGroup.LodGroupSize;
            var scale = mathExt.Scale(lodsSerializedData.localToWorldMatrix);
            var worldSize = LodUtils.GetWorldSpaceScale(scale) * lodSize;

            var maxLod = 0;
            LodUnbakeData[] lodUnbakeData = new LodUnbakeData[drakeLodGroup.Renderers.Length];
            for (int i = 0; i < drakeLodGroup.Renderers.Length; i++) {
                var drakeRenderer = drakeLodGroup.Renderers[i];
                var lodMask = drakeRenderer.LodMask;
                var renderer = unbakeTarget ?
                    DrakeMeshRendererEditor.SpawnAuthoring(drakeRenderer, unbakeTarget, drakeLodGroup.IsStatic) :
                    DrakeMeshRendererEditor.Unbake(drakeRenderer, drakeLodGroup.IsStatic);
                lodUnbakeData[i] = new LodUnbakeData(lodMask, renderer);
                var lastUsedLod = 32 - math.lzcnt(lodMask);
                maxLod = math.max(maxLod, lastUsedLod);
            }
            if (maxLod > 8) {
                Log.Important?.Error("Cannot correctly spawn authoring, too many LODs.");
                maxLod = 8;
            }

            var lods = new LOD[maxLod];
            for (int i = 0; i < maxLod; i++) {
                var mask = 1 << i;
                var renderers = lodUnbakeData.Where(d => (d.lodMask & mask) == mask).Select(static d => d.renderer).ToArray();
                var distance = i < 4 ? lodsSerializedData.lodDistances0[i] : lodsSerializedData.lodDistances1[i - 4];
                lods[i] = new LOD(worldSize / distance, renderers);
            }

            var targetGameObject = unbakeTarget ? unbakeTarget : drakeLodGroup.gameObject;
            var lodGroup = targetGameObject.AddComponent<LODGroup>();
            lodGroup.SetLODs(lods);
            lodGroup.size = lodSize;
        }

        static void DestroyEmpty(GameObject gameObject) {
            var nextParent = gameObject.transform.parent?.gameObject;
            if (gameObject && gameObject.transform.childCount == 0 && gameObject.GetComponents<Component>().Length == 1) {
                Object.DestroyImmediate(gameObject);
                DestroyEmpty(nextParent);
            }
        }

        // === Helpers
        public static T LoadAsset<T>(AssetReference assetReference) where T : Object {
            if (string.IsNullOrEmpty(assetReference.SubObjectName)) {
                return assetReference.editorAsset as T;
            }
            var path = AssetDatabase.GUIDToAssetPath(assetReference.AssetGUID);
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in allAssets) {
                if (asset is T castedAsset && castedAsset.name == assetReference.SubObjectName) {
                    return castedAsset;
                }
            }
            return assetReference.editorAsset as T;
        }

        public static void MeshStats(Mesh mesh, out int vertexCount, out uint triangleCount, out int subMeshCount) {
            if (!mesh) {
                vertexCount = 0;
                triangleCount = 0;
                subMeshCount = 0;
                return;
            }
            vertexCount = mesh.vertexCount;
            triangleCount = 0;
            subMeshCount = mesh.subMeshCount;
            for (var i = 0; i < subMeshCount; i++) {
                triangleCount += mesh.GetIndexCount(i) / 3;
            }
        }

        static MeshRenderer[] CreateNoShadowRenderers(LODGroup lodGroup, MeshRenderer[] originalRenderers) {
            var results = new MeshRenderer[originalRenderers.Length];
            for (var i = 0; i < originalRenderers.Length; i++) {
                var originalRenderer = originalRenderers[i];
                var originalTransform = originalRenderer.transform;

                var noShadowRendererGo = new GameObject(originalRenderer.name + "_NoShadow");
                var noShadowTransform = noShadowRendererGo.transform;

                Transform parent = lodGroup.transform;

                noShadowTransform.SetParent(parent);
                originalTransform.GetLocalPositionAndRotation(out var originalPosition, out var originalRotation);
                noShadowTransform.SetLocalPositionAndRotation(originalPosition, originalRotation);
                noShadowTransform.localScale = originalTransform.localScale;

                ComponentUtility.CopyComponent(originalRenderer);
                ComponentUtility.PasteComponentAsNew(noShadowRendererGo);
                ComponentUtility.CopyComponent(originalRenderer.GetComponent<MeshFilter>());
                ComponentUtility.PasteComponentAsNew(noShadowRendererGo);

                var noShadowRenderer = noShadowRendererGo.GetComponent<MeshRenderer>();
                noShadowRenderer.shadowCastingMode = ShadowCastingMode.Off;
                results[i] = noShadowRenderer;
            }
            return results;
        }

        readonly struct LodUnbakeData {
            public readonly Renderer renderer;
            public readonly int lodMask;

            public LodUnbakeData(int lodMask, Renderer renderer) {
                this.lodMask = lodMask;
                this.renderer = renderer;
            }
        }
    }
}
