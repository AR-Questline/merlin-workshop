using System;
using System.Collections.Generic;
using System.Globalization;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.Editor.DrakeRenderer;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Debugging.RenderingValidations {
    public static class RenderingValidatorStaticAnalysis {
        const int PreallocateCount = 128;

        public static void InitializeMedusaRenderersWithZeroCount(ECS.MedusaRenderer.Renderer[] renderers,
            Dictionary<MaterialMeshNameWithoutLOD, MaterialMeshVisibilityStats> medusaMaterialMeshNameWithoutLODToVisibleUpdatesCountMap,
            HashSet<MaterialMeshNameWithoutLOD> processedInCurrentFrameLODMaterialMeshesCache) {
            medusaMaterialMeshNameWithoutLODToVisibleUpdatesCountMap.Clear();
            processedInCurrentFrameLODMaterialMeshesCache.Clear();
            int renderersCount = renderers.Length;
            for (int rendererIndex = 0; rendererIndex < renderersCount; rendererIndex++) {
                var renderer = renderers[rendererIndex];
                var rendererDatas = renderer.renderData;
                int rendererDataCount = rendererDatas.Count;
                for (int dataIndex = 0; dataIndex < rendererDataCount; dataIndex++) {
                    var data = rendererDatas[dataIndex];
                    var meshName = data.mesh.name;
                    bool isLODMesh = IsMeshNameWithLOD(meshName);
                    var meshNameWithoutLOD = isLODMesh ? GetMeshNameWithoutLOD(meshName) : meshName;
                    var materialMeshNameWithoutLOD = new MaterialMeshNameWithoutLOD(data.material, meshNameWithoutLOD);
                    if (isLODMesh && processedInCurrentFrameLODMaterialMeshesCache.Add(materialMeshNameWithoutLOD) == false) {
                        continue;
                    }

                    medusaMaterialMeshNameWithoutLODToVisibleUpdatesCountMap.TryAdd(materialMeshNameWithoutLOD, new MaterialMeshVisibilityStats(0));
                }
            }
        }

        public static unsafe void IncreaseVisibleInstancesCount(Dictionary<MaterialMeshRef, long> materialMeshRefToVisibleCountMap,
            Dictionary<MaterialMeshNameWithoutLOD, MaterialMeshVisibilityStats> materialMeshToVisibleUpdatesCountMap,
            Dictionary<MaterialMeshNameWithoutLOD, LODLevelsVisibilityStats> materialMeshNameWithoutLODToLODVisibilityStats,
            HashSet<MaterialMeshNameWithoutLOD> processedInCurrentFrameLODMaterialMeshesCache) {
            if (materialMeshRefToVisibleCountMap == null || materialMeshRefToVisibleCountMap.Count == 0) {
                return;
            }

            processedInCurrentFrameLODMaterialMeshesCache.Clear();
            foreach (var materialMeshRef in materialMeshRefToVisibleCountMap.Keys) {
                var meshName = materialMeshRef.mesh.name;
                bool isMeshLod = IsMeshNameWithLOD(meshName);
                var meshNameWithoutLOD = isMeshLod ? GetMeshNameWithoutLOD(meshName) : meshName;
                var materialMeshNameWithoutLOD = new MaterialMeshNameWithoutLOD(materialMeshRef.material, meshNameWithoutLOD);
                if (processedInCurrentFrameLODMaterialMeshesCache.Add(materialMeshNameWithoutLOD)) {
                    var visibilityStats = materialMeshToVisibleUpdatesCountMap.GetValueOrDefault(materialMeshNameWithoutLOD);
                    visibilityStats.anyInstanceVisibleFramesCount += 1;
                    materialMeshToVisibleUpdatesCountMap[materialMeshNameWithoutLOD] = visibilityStats;
                }

                if (isMeshLod) {
                    var lodLevelsVisibilityStats = materialMeshNameWithoutLODToLODVisibilityStats.GetValueOrDefault(materialMeshNameWithoutLOD);
                    var meshLODLevel = GetMeshLODLevel(meshName);
                    if (meshLODLevel < 0 || meshLODLevel >= 8) {
                        Log.Minor?.Error($"Lod level {meshLODLevel} not valid");
                        return;
                    }
                    lodLevelsVisibilityStats.lodAnyInstanceVisibleFramesCount[meshLODLevel] += 1;
                    materialMeshNameWithoutLODToLODVisibilityStats[materialMeshNameWithoutLOD] = lodLevelsVisibilityStats;
                }
            }
        }

        public static unsafe RendererWithVisibilityStats[] GetRenderersWithVisibilityStats(
            Dictionary<MaterialMeshNameWithoutLOD, List<MeshRendererOrLODGroupHolder>> materialMeshNameWithoutLODToRenderersMap,
            Dictionary<MaterialMeshNameWithoutLOD, MaterialMeshVisibilityStats> materialMeshNameWithoutLODToVisibilityStatsMap,
            Dictionary<MaterialMeshNameWithoutLOD, LODLevelsVisibilityStats> materialMeshNameWithoutLODToLODVisibilityStats,
            int recordedFramesCount) {
            if (recordedFramesCount == 0) {
                return Array.Empty<RendererWithVisibilityStats>();
            }

            float invRecordedFramesCount = 1f / recordedFramesCount;
            var renderersWithVisibilityStats = new RendererWithVisibilityStats[materialMeshNameWithoutLODToRenderersMap.Count];
            int addIndex = 0;
            foreach (var (materialMeshNameWithoutLOD, renderers) in materialMeshNameWithoutLODToRenderersMap) {
                var visibilityStats = materialMeshNameWithoutLODToVisibilityStatsMap[materialMeshNameWithoutLOD];
                float visibilityPercent = visibilityStats.anyInstanceVisibleFramesCount * invRecordedFramesCount;
                if (materialMeshNameWithoutLODToLODVisibilityStats.TryGetValue(materialMeshNameWithoutLOD, out var lodVisibilityStats)) {
                    string[] lodVisibilityPercents = new string[LODLevelsVisibilityStats.LODLevelsCount];
                    for (int i = 0; i < LODLevelsVisibilityStats.LODLevelsCount; i++) {
                        var lodVisibilityPercent = math.clamp(lodVisibilityStats.lodAnyInstanceVisibleFramesCount[i] * invRecordedFramesCount, 0, 1);
                        lodVisibilityPercents[i] = math.ceil(lodVisibilityPercent * 100).ToString(CultureInfo.InvariantCulture) + " %";
                    }
                    renderersWithVisibilityStats[addIndex++] = new RendererWithVisibilityStats(materialMeshNameWithoutLOD, renderers, visibilityPercent, true, lodVisibilityPercents);
                } 
                else {
                    renderersWithVisibilityStats[addIndex++] = new RendererWithVisibilityStats(materialMeshNameWithoutLOD, renderers, visibilityPercent, false);
                }
            }

            return renderersWithVisibilityStats;
        }

        public static void GetDrakeData(
            Dictionary<MaterialMeshNameWithoutLOD, MaterialMeshVisibilityStats> materialMeshNameWithoutLODToVisibilityStatsMap,
            out Dictionary<MaterialMeshNameWithoutLOD, List<MeshRendererOrLODGroupHolder>> materialMeshNameWithoutLODToRenderersMap) {
            var allMeshRenderers = Object.FindObjectsByType<DrakeMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var allMeshRenderersCount = allMeshRenderers.Length;
            materialMeshNameWithoutLODToRenderersMap = new Dictionary<MaterialMeshNameWithoutLOD, List<MeshRendererOrLODGroupHolder>>(PreallocateCount);
            var addedLodGroupsInstanceIndices = new Unity.Collections.NativeHashSet<int>(PreallocateCount, ARAlloc.Temp);
            for (int i = 0; i < allMeshRenderersCount; i++) {
                try {
                    var meshRenderer = allMeshRenderers[i];
                    if (meshRenderer.MaterialReferences.Length == 0 || meshRenderer == null) {
                        continue;
                    }

                    var material = DrakeEditorHelpers.LoadAsset<Material>(meshRenderer.MaterialReferences[0]);
                    var mesh = DrakeEditorHelpers.LoadAsset<Mesh>(meshRenderer.MeshReference);
                    if (material == null || mesh == null) {
                        continue;
                    }

                    var meshName = mesh.name;
                    bool isLODMesh = IsMeshNameWithLOD(meshName);
                    var meshNameWithoutLOD = isLODMesh ? GetMeshNameWithoutLOD(meshName) : meshName;
                    var materialMeshNameWithoutLOD = new MaterialMeshNameWithoutLOD(material, meshNameWithoutLOD);
                    if (materialMeshNameWithoutLODToVisibilityStatsMap.TryGetValue(materialMeshNameWithoutLOD, out var visibilityStats) == false) {
                        continue;
                    }

                    if (materialMeshNameWithoutLODToRenderersMap.TryGetValue(materialMeshNameWithoutLOD, out var renderersList) == false) {
                        renderersList = new List<MeshRendererOrLODGroupHolder>(10);
                        materialMeshNameWithoutLODToRenderersMap.Add(materialMeshNameWithoutLOD, renderersList);
                    }

                    if (meshRenderer.transform.TryGetComponent(out DrakeLodGroup lodGroup) ||
                        (meshRenderer.transform.parent != null && meshRenderer.transform.parent.TryGetComponent(out lodGroup))) {
                        if (addedLodGroupsInstanceIndices.Add(lodGroup.GetHashCode()) == false) {
                            continue;
                        }

                        renderersList.Add(new DrakeLODGroupHolder(lodGroup));
                    } else {
                        renderersList.Add(new DrakeMeshRendererHolder(meshRenderer));
                    }
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }

            addedLodGroupsInstanceIndices.Dispose();
        }

        public static void GetMedusaData(
            Dictionary<MaterialMeshNameWithoutLOD, MaterialMeshVisibilityStats> materialMeshNameWithoutLODToVisibilityStatsMap,
            out Dictionary<MaterialMeshNameWithoutLOD, List<MeshRendererOrLODGroupHolder>> materialMeshNameWithoutLODToRenderersMap) {
            var allMeshRenderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var allMeshRenderersCount = allMeshRenderers.Length;
            materialMeshNameWithoutLODToRenderersMap = new Dictionary<MaterialMeshNameWithoutLOD, List<MeshRendererOrLODGroupHolder>>(PreallocateCount);
            var addedLodGroupsInstanceIndices = new Unity.Collections.NativeHashSet<int>(PreallocateCount, ARAlloc.Temp);
            for (int i = 0; i < allMeshRenderersCount; i++) {
                var meshRenderer = allMeshRenderers[i];
                try {
                    if (meshRenderer.TryGetComponent(out MeshFilter meshFilter) == false) {
                        continue;
                    }

                    var material = meshRenderer.sharedMaterial;
                    var mesh = meshFilter.sharedMesh;
                    if (material == null || mesh == null) {
                        continue;
                    }

                    var meshName = mesh.name;
                    bool isLODMesh = IsMeshNameWithLOD(meshName);
                    var meshNameWithoutLOD = isLODMesh ? GetMeshNameWithoutLOD(meshName) : meshName;
                    var materialMeshNameWithoutLOD = new MaterialMeshNameWithoutLOD(material, meshNameWithoutLOD);
                    if (materialMeshNameWithoutLODToVisibilityStatsMap.TryGetValue(materialMeshNameWithoutLOD, out var visibilityStats) == false) {
                        continue;
                    }

                    if (materialMeshNameWithoutLODToRenderersMap.TryGetValue(materialMeshNameWithoutLOD, out var renderersList) == false) {
                        renderersList = new List<MeshRendererOrLODGroupHolder>(10);
                        materialMeshNameWithoutLODToRenderersMap.Add(materialMeshNameWithoutLOD, renderersList);
                    }

                    if (meshRenderer.transform.TryGetComponent(out LODGroup lodGroup) ||
                        (meshRenderer.transform.parent != null && meshRenderer.transform.parent.TryGetComponent(out lodGroup))) {
                        if (addedLodGroupsInstanceIndices.Add(lodGroup.GetHashCode()) == false) {
                            continue;
                        }

                        renderersList.Add(new MedusaLODGroupHolder(lodGroup));
                    } else {
                        renderersList.Add(new MedusaMeshRendererHolder(meshRenderer));
                    }
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }

            addedLodGroupsInstanceIndices.Dispose();
        }

        static bool IsMeshNameWithLOD(string meshName) {
            var length = meshName.Length;
            if (length <= 4) {
                return false;
            }

            return meshName[length - 4] == 'L' & meshName[length - 3] == 'O' & meshName[length - 2] == 'D';
        }

        static string GetMeshNameWithoutLOD(string meshName) {
            return meshName.Substring(0, meshName.Length - 4);
        }

        static int GetMeshLODLevel(string meshName) {
            if (int.TryParse(meshName[^1].ToString(), out var lodLevel)) {
                return lodLevel;
            }

            Log.Minor?.Error($"Mesh {meshName} has invalid name with LOD format");
            return 0;
        }
    }
}