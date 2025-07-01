#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.ECS.Authoring;
using Awaken.Utility;
using Awaken.Utility.GameObjects;
using Awaken.Utility.Graphics;
using Awaken.Utility.LowLevel;
using Awaken.Utility.Maths;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Awaken.ECS.DrakeRenderer.Authoring {
    public class DrakeMergedRenderersRoot : MonoBehaviour {
        // Bake & Save
        struct SaveData {
            public DrakeMergedRenderers.LodGroup[] lodGroupsData;
            public DrakeMergedRenderers.MeshData[] serializedMeshData;
            public SerializableGuid[] materialKeys;
            public DrakeMergedRenderers.SerializableRendererDefinition[] rendererDefinitions;
            public DrakeMergedRenderers.RendererInstance[] rendererInstances;
        }

        public void Bake() {
            var targetGo = new GameObject("DM");
            targetGo.transform.SetParent(transform, false);
            targetGo.SetActive(false);
            var target = targetGo.AddComponent<DrakeMergedRenderers>();

            var dataToSave = Merge(this);
            var dataGuid = SaveToFile(dataToSave);
            DrakeMergedRenderers.EditorAccess.DataGuid(target) = dataGuid;

            EditorUtility.SetDirty(target);
            DestroyImmediate(this);
            targetGo.SetActive(true);
        }

        static SaveData Merge(DrakeMergedRenderersRoot sourceRoot) {
            var drakeLodGroups = sourceRoot.GetComponentsInChildren<DrakeLodGroup>();
            var drakeRenderers = new HashSet<DrakeMeshRenderer>(sourceRoot.GetComponentsInChildren<DrakeMeshRenderer>());

            var lodGroups = new List<DrakeMergedRenderers.LodGroup>(drakeLodGroups.Length + 1);
            lodGroups.Add(default);

            var meshData = new List<DrakeMergedRenderers.MeshData>(128);
            var meshDataMap = new Dictionary<FixedString128Bytes, uint>(128);

            var definitions = new List<DrakeMergedRenderers.AuthoringRendererDefinition>(drakeRenderers.Count);
            var definitionsMap = new Dictionary<DrakeMergedRenderers.AuthoringRendererDefinition, uint>(drakeRenderers.Count);

            var instances = new List<DrakeMergedRenderers.RendererInstance>(drakeRenderers.Count);

            // Drake LOD group
            foreach (var drakeLodGroup in drakeLodGroups) {
                if (!CanBeBakedLodGroup(drakeLodGroup)) {
                    continue;
                }

                var lodGroupModificationSteps = drakeLodGroup.GetComponents<IDrakeLODBakingModificationStep>();
                for (int i = 0; i < lodGroupModificationSteps.Length; i++) {
                    try {
                        lodGroupModificationSteps[i].ModifyDrakeLODGroup(drakeLodGroup);
                    } catch (Exception e) {
                        Debug.LogException(e);
                    }
                }

                var lodGroupIndex = lodGroups.Count;
                var lodGroupData = drakeLodGroup.LodGroupSerializableData;
                lodGroups.Add(new DrakeMergedRenderers.LodGroup {
                    distances0 = lodGroupData.lodDistances0,
                    distances1 = lodGroupData.lodDistances1,
                    worldReferencePoint = lodGroupData.ToWorldReferencePoint().Value,
                });

                foreach (var meshRenderer in drakeLodGroup.Renderers) {
                    AddDrakeRenderer(meshRenderer, (uint)lodGroupIndex);
                    drakeRenderers.Remove(meshRenderer);
                    DestroyRecursive(meshRenderer);
                }
                DestroyRecursive(drakeLodGroup);
            }

            // Drake renderer
            foreach (var drakeMeshRenderer in drakeRenderers) {
                if (!CanBeBakedRenderer(drakeMeshRenderer)) {
                    continue;
                }

                AddDrakeRenderer(drakeMeshRenderer, 0);
                DestroyRecursive(drakeMeshRenderer);
            }

            var materials = new List<SerializableGuid>(256);
            var serializableDefinitions = new DrakeMergedRenderers.SerializableRendererDefinition[definitions.Count];
            for (var i = 0; i < definitions.Count; i++) {
                var definition = definitions[i];
                var startMaterials = materials.Count;
                for (var j = 0; j < definition.materialKeys.Length; j++) {
                    materials.Add(definition.materialKeys[j]);
                }

                serializableDefinitions[i] = new DrakeMergedRenderers.SerializableRendererDefinition {
                    renderFilterSettings = definition.renderFilterSettings,
                    lodMask = definition.lodMask,
                    meshIndex = definition.meshIndex,
                    lightProbeUsage = definition.lightProbeUsage,
                    transparentMask = definition.transparentMask,
                    materialsRange = IndexRange.FromStartEnd((uint)startMaterials, (uint)materials.Count),
                };
            }

            return new SaveData {
                lodGroupsData = lodGroups.ToArray(),
                serializedMeshData = meshData.ToArray(),
                materialKeys = materials.ToArray(),
                rendererDefinitions = serializableDefinitions,
                rendererInstances = instances.ToArray(),
            };

            bool CanBeBakedLodGroup(DrakeLodGroup lodGroup) {
                if (!lodGroup.IsStatic) {
                    return false;
                }
                // TODO: investigate if can be supported
                if (lodGroup.HasEntitiesAccess) {
                    return false;
                }
                if (lodGroup.GetComponent<IDrakeLODBakingStep>() != null) {
                    return false;
                }
                if (lodGroup.GetComponentInParent<DrakeMergedRenderersRoot>() != sourceRoot) {
                    return false;
                }
                return true;
            }

            bool CanBeBakedRenderer(DrakeMeshRenderer meshRenderer) {
                if (!meshRenderer.IsStatic) {
                    return false;
                }
                // TODO: investigate if can be supported
                if (meshRenderer.HasEntitiesAccess) {
                    return false;
                }
                if (meshRenderer.GetComponent<IDrakeMeshRendererBakingStep>() != null) {
                    return false;
                }
                if (meshRenderer.GetComponentInParent<DrakeMergedRenderersRoot>() != sourceRoot) {
                    return false;
                }
                return true;
            }

            uint AddMesh(string meshGuid, string meshSubAsset) {
                var meshFullKey = string.IsNullOrWhiteSpace(meshSubAsset) ? meshGuid : $"{meshGuid}[{meshSubAsset}]";
                var meshKey = new FixedString128Bytes(meshFullKey);
                if (meshDataMap.TryGetValue(meshKey, out var existingIndex)) {
                    return existingIndex;
                }

                var index = (uint)meshData.Count;
                Mesh mesh;
                if (string.IsNullOrWhiteSpace(meshSubAsset)) {
                    mesh = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(meshGuid));
                } else {
                    var allObjects = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(meshGuid));
                    mesh = allObjects.OfType<Mesh>().First(m => m.name == meshSubAsset);
                }

                var aabb = mesh.bounds.ToAABB();
                var uvDistribution = mesh.GetUVDistributionMetric(0);
                var currentMeshData = new DrakeMergedRenderers.MeshData {
                    meshKey = meshKey,
                    aabb = aabb,
                    uvDistribution = uvDistribution,
                };
                meshData.Add(currentMeshData);
                meshDataMap.Add(meshKey, index);

                return index;
            }

            uint AddDefinition(DrakeMergedRenderers.AuthoringRendererDefinition definition) {
                if (definitionsMap.TryGetValue(definition, out var existingIndex)) {
                    return existingIndex;
                }

                var index = (uint)definitions.Count;
                definitions.Add(definition);
                definitionsMap.Add(definition, index);

                return index;
            }

            void AddDrakeRenderer(DrakeMeshRenderer drakeMeshRenderer, uint lodGroupIndex) {
                var drakeMeshRendererModificationSteps = drakeMeshRenderer.GetComponents<IDrakeMeshRendererBakingModificationStep>();
                for (int i = 0; i < drakeMeshRendererModificationSteps.Length; i++) {
                    drakeMeshRendererModificationSteps[i].ModifyDrakeMeshRenderer(drakeMeshRenderer);
                }

                var description = drakeMeshRenderer.RenderMeshDescription(true);
                var filterSettings = description.FilterSettings;

                var renderFilterSettings = new SerializableFilterSettings {
                    Layer = filterSettings.Layer,
                    RenderingLayerMask = filterSettings.RenderingLayerMask,
                    MotionMode = (byte)(int)filterSettings.MotionMode,
                    ShadowCastingMode = (byte)(int)filterSettings.ShadowCastingMode,
                    ReceiveShadows = filterSettings.ReceiveShadows,
                    StaticShadowCaster = filterSettings.StaticShadowCaster,
                };

                var lodMask = drakeMeshRenderer.LodMask;

                var (meshGuid, meshSubAsset) = drakeMeshRenderer.MeshReferenceData;
                var meshIndex = AddMesh(meshGuid, meshSubAsset);

                var lightProbeUsage = (byte)(int)description.LightProbeUsage;
                byte transparentMask = 0;
                var materialKeys = new SerializableGuid[drakeMeshRenderer.MaterialReferences.Length];

                for (var i = 0; i < materialKeys.Length; i++) {
                    var materialReference = drakeMeshRenderer.MaterialReferences[i];
                    var materialGuid = materialReference.AssetGUID;
                    materialKeys[i] = new SerializableGuid(materialGuid);
                    var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(materialGuid));
                    if (ShadersUtils.IsMaterialTransparent(material)) {
                        transparentMask |= (byte)(1 << i);
                    }
                }

                var rendererDefinition = new DrakeMergedRenderers.AuthoringRendererDefinition {
                    renderFilterSettings = renderFilterSettings,
                    lodMask = lodMask,
                    meshIndex = meshIndex,
                    lightProbeUsage = lightProbeUsage,
                    transparentMask = transparentMask,
                    materialKeys = materialKeys,
                };

                var definitionIndex = AddDefinition(rendererDefinition);

                var rendererInstance = new DrakeMergedRenderers.RendererInstance {
                    localToWorld = drakeMeshRenderer.LocalToWorld.orthonormal(),
                    lodGroupIndex = lodGroupIndex,
                    definitionIndex = definitionIndex,
                };
                instances.Add(rendererInstance);
            }

            void DestroyRecursive(MonoBehaviour monoBehaviour) {
                if (monoBehaviour.transform.IsLeafSingleComponent()) {
                    DestroyImmediate(monoBehaviour.gameObject);
                } else {
                    DestroyImmediate(monoBehaviour);
                }
            }
        }

        static SerializableGuid SaveToFile(SaveData saveData) {
            var guid = Guid.NewGuid();
            var parentDirectory = DrakeMergedRenderersLoading.BakingDirectoryPath;
            if (!Directory.Exists(parentDirectory)) {
                Directory.CreateDirectory(parentDirectory);
            }
            var filePath = Path.Combine(parentDirectory, $"{guid:N}.data");

            using var fileWriter = new FileWriter(filePath);

            // -- Write lod groups
            fileWriter.Write(saveData.lodGroupsData.Length);
            fileWriter.Write(saveData.lodGroupsData);

            // -- Write mesh data
            fileWriter.Write(saveData.serializedMeshData.Length);
            fileWriter.Write(saveData.serializedMeshData);

            // -- Write material keys
            fileWriter.Write(saveData.materialKeys.Length);
            fileWriter.Write(saveData.materialKeys);

            // -- Write renderer definitions
            fileWriter.Write(saveData.rendererDefinitions.Length);
            fileWriter.Write(saveData.rendererDefinitions);

            // -- Write renderer instances
            fileWriter.Write(saveData.rendererInstances.Length);
            fileWriter.Write(saveData.rendererInstances);

            return new SerializableGuid(guid);
        }
    }
}
#endif
