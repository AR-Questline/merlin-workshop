using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.Utility.Collections;
using Awaken.Utility.Maths;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Awaken.TG.LeshyRenderer {
    [CreateAssetMenu(fileName = "LeshyPrefabs", menuName = "TG/Leshy/Prefabs")]
    public class LeshyPrefabs : ScriptableObject {
        static readonly CellSizeByType[] DefaultCellSizeByVegetationType = new CellSizeByType[]{
            new CellSizeByType { prefabType = PrefabType.Grass, cellSize = 50f },
            new CellSizeByType { prefabType = PrefabType.Plant, cellSize = 100f },
            new CellSizeByType { prefabType = PrefabType.Object, cellSize = 150f },
            new CellSizeByType { prefabType = PrefabType.Ivy, cellSize = 150f },
            new CellSizeByType { prefabType = PrefabType.LargeObject, cellSize = 500f },
            new CellSizeByType { prefabType = PrefabType.Tree, cellSize = 500f },
        };

        [SerializeField, ListDrawerSettings(ShowIndexLabels = true)]
        PrefabAuthoring[] prefabs = Array.Empty<PrefabAuthoring>();

        [SerializeField] VspRemap[] vspRemaps = Array.Empty<VspRemap>();
        [SerializeField] HandPlacedLeshyInstanceRemap[] handPlacedRemaps = Array.Empty<HandPlacedLeshyInstanceRemap>();

        // -- TMP
        [SerializeField, ListDrawerSettings(IsReadOnly = true)]
        CellSizeByType[] cellSizeByVegetationType = Array.Empty<CellSizeByType>();
#if UNITY_EDITOR && !ADDRESSABLES_BUILD
        [SerializeField, ListDrawerSettings(ShowIndexLabels = true), UsedImplicitly, UnityEngine.Scripting.Preserve]
        List<GameObject> inputPrefabs = new();
#endif

        public VspRemap[] VspRemaps => vspRemaps;
        public HandPlacedLeshyInstanceRemap[] HandPlacedRemaps => handPlacedRemaps;
        public PrefabAuthoring[] Prefabs => prefabs;
        
        public PrefabRuntime[] RuntimePrefabs(in LeshyQualityManager qualityManager) {
            var runtimePrefabs = new PrefabRuntime[prefabs.Length];
            for (var i = 0; i < prefabs.Length; i++) {
                runtimePrefabs[i] = new PrefabRuntime(prefabs[i], qualityManager);
            }

            return runtimePrefabs;
        }

        void Reset() {
            cellSizeByVegetationType = DefaultCellSizeByVegetationType.CreateCopy();
        }

        void OnValidate() {
            if (cellSizeByVegetationType.Length != Enum.GetValues(typeof(PrefabType)).Length) {
                cellSizeByVegetationType = new[] {
                    GetCellSizeByTypeOrDefault(PrefabType.Grass),
                    GetCellSizeByTypeOrDefault(PrefabType.Plant),
                    GetCellSizeByTypeOrDefault(PrefabType.Object),
                    GetCellSizeByTypeOrDefault(PrefabType.Ivy),
                    GetCellSizeByTypeOrDefault(PrefabType.LargeObject),
                    GetCellSizeByTypeOrDefault(PrefabType.Tree),
                };
            }

            CellSizeByType GetCellSizeByTypeOrDefault(PrefabType prefabType) {
                return cellSizeByVegetationType.TryGetFirst(item => item.prefabType == prefabType, out var grassItem) ? grassItem : DefaultCellSizeByVegetationType.First(item => item.prefabType == prefabType);
            }
        }

        [Serializable]
        public struct HandPlacedLeshyInstanceRemap : IEquatable<HandPlacedLeshyInstanceRemap> {
            public int instanceIndex;
            public int prefabIndex;

            public HandPlacedLeshyInstanceRemap(int instanceIndex, int prefabIndex) {
                this.instanceIndex = instanceIndex;
                this.prefabIndex = prefabIndex;
            }

            public bool Equals(HandPlacedLeshyInstanceRemap other) {
                return instanceIndex == other.instanceIndex && prefabIndex == other.prefabIndex;
            }

            public override bool Equals(object obj) {
                return obj is HandPlacedLeshyInstanceRemap other && Equals(other);
            }

            public override int GetHashCode() {
                return HashCode.Combine(instanceIndex, prefabIndex);
            }
        }

        [Serializable]
        public struct VspRemap : IEquatable<VspRemap> {
            public string vspId;
            public int prefabIndex;

            public VspRemap(string vspId, int prefabIndex) {
                this.vspId = vspId;
                this.prefabIndex = prefabIndex;
            }

            public bool Equals(VspRemap other) {
                return vspId == other.vspId && prefabIndex == other.prefabIndex;
            }

            public override bool Equals(object obj) {
                return obj is VspRemap other && Equals(other);
            }

            public override int GetHashCode() {
                unchecked {
                    return ((vspId != null ? vspId.GetHashCode() : 0) * 397) ^ prefabIndex;
                }
            }

            public static bool operator ==(VspRemap left, VspRemap right) {
                return left.Equals(right);
            }

            public static bool operator !=(VspRemap left, VspRemap right) {
                return !left.Equals(right);
            }
        }

        [Serializable]
        public struct RendererAuthoring : IEquatable<RendererAuthoring> {
            public Mesh mesh;

            [InlineEditor(InlineEditorModes.GUIAndHeader, Expanded = true)]
            public Material[] materials;

            public FilterSettings filterSettings;
            public byte lodMask;

            public static RendererAuthoring FromRenderer(Renderer renderer) {
                var filterSettings = new FilterSettings {
                    renderingLayerMask = renderer.renderingLayerMask,
                    layer = (byte)renderer.gameObject.layer,
                    shadowCastingMode = renderer.shadowCastingMode,
                };
                return new RendererAuthoring {
                    mesh = renderer.GetComponent<MeshFilter>().sharedMesh,
                    materials = renderer.sharedMaterials,
                    filterSettings = filterSettings,
                };
            }

            public static RendererAuthoring FromDrakeRenderer(DrakeMeshRenderer drakeMeshRenderer) {
                var filterSettings = drakeMeshRenderer.RenderMeshDescription(true).FilterSettings;
                GetMeshAndMaterials(drakeMeshRenderer, out var mesh, out var materials);
                return new RendererAuthoring {
                    mesh = mesh,
                    materials = materials,
                    filterSettings = new FilterSettings() {
                        layer = (byte)filterSettings.Layer,
                        renderingLayerMask = filterSettings.RenderingLayerMask,
                        shadowCastingMode = filterSettings.ShadowCastingMode
                    }
                };

                static void GetMeshAndMaterials(DrakeMeshRenderer drakeMeshRenderer, out Mesh mesh, out Material[] materials) {
#if UNITY_EDITOR
                    if (Application.isPlaying == false) {
                        materials = drakeMeshRenderer.EDITOR_GetMaterials();
                        mesh = drakeMeshRenderer.EDITOR_GetMesh();
                        return;
                    }
#endif
                    drakeMeshRenderer.StartLoadingMesh();
                    drakeMeshRenderer.StartLoadingMaterials();
                    mesh = drakeMeshRenderer.WaitForCompletionMesh();
                    materials = drakeMeshRenderer.WaitForCompletionMaterials();
                }
            }

            public bool Equals(RendererAuthoring other) {
                var simpleEquals = mesh == other.mesh &&
                                   filterSettings.Equals(other.filterSettings) &&
                                   materials.Length == other.materials.Length &&
                                   lodMask == other.lodMask;
                if (!simpleEquals) {
                    return false;
                }

                for (int i = 0; i < materials.Length; i++) {
                    if (materials[i] != other.materials[i]) {
                        return false;
                    }
                }

                return true;
            }
        }

        [Serializable]
        public struct PrefabAuthoring : IEquatable<PrefabAuthoring> {
            const float LODDistancesComparisonError = 1.01f;

            public RendererAuthoring[] renderers;
            public float4x2 lodDistances;
            public AABB localBounds;
            public float cellSize;
            public float hidePercent;

            public PrefabType prefabType;
            public float colliderDistance;
            public GameObject colliders;

            // Rider is complaining about ^1 here
            // ReSharper disable once UseIndexFromEndExpression
            public ref RendererAuthoring Billboard => ref renderers[renderers.Length - 1];
            public bool hasBillboard;
            public bool HasCollider => colliders != null;
            public bool HasBillboard => hasBillboard;

            public static PrefabAuthoring FromLODGroup(LODGroup lodGroup, float lodFactor, PrefabType prefabType, float prefabCellSize,
                int prefabTypeLayer, float colliderDistance, GameObject collidersPrefab, bool useBillboards) {
                
                var lods = lodGroup.GetLODs();
                var meshRenderers =
                    lods.SelectMany(static l => l.renderers).OfType<MeshRenderer>().Distinct().ToArray();
                var lodDistances = GetLODDistancesFromLODGroup(lodGroup, lods, lodFactor);
                return FromLODGroupRenderers(meshRenderers, (renderer) => (byte)LodUtils.LodMask(lods, renderer), 
                    RendererAuthoring.FromRenderer, lodDistances, prefabType, prefabCellSize, prefabTypeLayer, colliderDistance, 
                    collidersPrefab, useBillboards);
            }
            
            public static PrefabAuthoring FromDrakeLODGroup(DrakeLodGroup drakeLodGroup, float lodFactor, PrefabType prefabType, float prefabCellSize,
                int prefabTypeLayer, float colliderDistance, GameObject collidersPrefab, bool useBillboards) {
                
                var lodData = drakeLodGroup.LodGroupSerializableDataRaw;
                var lodDistances = new float4x2(lodData.lodDistances0, lodData.lodDistances1) * lodFactor;
                return FromLODGroupRenderers(drakeLodGroup.Renderers, GetLodMask, RendererAuthoring.FromDrakeRenderer,
                    lodDistances, prefabType, prefabCellSize, prefabTypeLayer, colliderDistance,
                    collidersPrefab, useBillboards);

                static byte GetLodMask(DrakeMeshRenderer renderer) => (byte)renderer.LodMask; 
            }

            public static PrefabAuthoring FromLODGroupRenderers<TRenderer>(TRenderer[] lodRenderers, 
                Func<TRenderer, byte> getRendererLodMask, Func<TRenderer, RendererAuthoring> createRendererAuthoring,
                float4x2 lodDistances, PrefabType prefabType, float prefabCellSize, int prefabTypeLayer, float colliderDistance, 
                GameObject collidersPrefab, bool useBillboards) {
                
                var prefab = new PrefabAuthoring {
                    lodDistances = lodDistances,
                    cellSize = prefabCellSize,
                    prefabType = prefabType,
                    colliderDistance = colliderDistance,
                    colliders = collidersPrefab,
                };
                (RendererAuthoring[] renderers, AABB bounds) = GetRendererAuthoringArray(lodRenderers, getRendererLodMask, createRendererAuthoring, prefabTypeLayer);
                prefab.renderers = renderers;
                prefab.localBounds = bounds;

                prefab.hasBillboard = prefabType == PrefabType.Tree && useBillboards;
                return prefab;
            }

            static (RendererAuthoring[] renderers, AABB bounds) GetRendererAuthoringArray<TRenderer>(TRenderer[] lodRenderers, 
                Func<TRenderer, byte> getRendererLodMask, Func<TRenderer, RendererAuthoring> createRendererAuthoring, 
                int prefabTypeLayer) {
                Bounds bounds = default;
                var renderers = new RendererAuthoring[lodRenderers.Length];
                for (int i = 0; i < lodRenderers.Length; i++) {
                    var renderer = lodRenderers[i];
                    var lodMask = getRendererLodMask(renderer);
                    var rendererAuthoring = createRendererAuthoring(renderer);
                    var filterSettings = rendererAuthoring.filterSettings;

                    filterSettings.layer = (byte)prefabTypeLayer;

                    rendererAuthoring.filterSettings = filterSettings;
                    rendererAuthoring.lodMask = lodMask;

                    renderers[i] = rendererAuthoring;

                    if (i == 0) {
                        bounds = rendererAuthoring.mesh.bounds;
                    } else {
                        bounds.Encapsulate(rendererAuthoring.mesh.bounds);
                    }
                }

                return (renderers, bounds.ToAABB());
            }

            public bool Equals(PrefabAuthoring other) {
                return LodDistancesApproxEquals(lodDistances, other.lodDistances) &&
                       RenderersEquals(renderers, other.renderers);
            }

            public static bool LodDistancesApproxEquals(float4x2 lodDistances, float4x2 otherLodDistance) {
                if (lodDistances.Equals(otherLodDistance)) {
                    return true;
                }

                return (math.isinf(lodDistances.c0.x) ? lodDistances.c0.x == otherLodDistance.c0.x : math.abs(lodDistances.c0.x - otherLodDistance.c0.x) < LODDistancesComparisonError) &
                       (math.isinf(lodDistances.c0.y) ? lodDistances.c0.y == otherLodDistance.c0.y : math.abs(lodDistances.c0.y - otherLodDistance.c0.y) < LODDistancesComparisonError) &
                       (math.isinf(lodDistances.c0.z) ? lodDistances.c0.z == otherLodDistance.c0.z : math.abs(lodDistances.c0.z - otherLodDistance.c0.z) < LODDistancesComparisonError) &
                       (math.isinf(lodDistances.c0.w) ? lodDistances.c0.w == otherLodDistance.c0.w : math.abs(lodDistances.c0.w - otherLodDistance.c0.w) < LODDistancesComparisonError) &
                       (math.isinf(lodDistances.c1.x) ? lodDistances.c1.x == otherLodDistance.c1.x : math.abs(lodDistances.c1.x - otherLodDistance.c1.x) < LODDistancesComparisonError) &
                       (math.isinf(lodDistances.c1.y) ? lodDistances.c1.y == otherLodDistance.c1.y : math.abs(lodDistances.c1.y - otherLodDistance.c1.y) < LODDistancesComparisonError) &
                       (math.isinf(lodDistances.c1.z) ? lodDistances.c1.z == otherLodDistance.c1.z : math.abs(lodDistances.c1.z - otherLodDistance.c1.z) < LODDistancesComparisonError) &
                       (math.isinf(lodDistances.c1.w) ? lodDistances.c1.w == otherLodDistance.c1.w : math.abs(lodDistances.c1.w - otherLodDistance.c1.w) < LODDistancesComparisonError);
            }
            
            static bool RenderersEquals(RendererAuthoring[] left, RendererAuthoring[] right) {
                if (left.Length != right.Length) {
                    return false;
                }

                for (int i = 0; i < left.Length; i++) {
                    if (!left[i].Equals(right[i])) {
                        return false;
                    }
                }

                return true;
            }

            public static float4x2 GetLODDistancesFromLODGroup(LODGroup lodGroup, LOD[] lods, float lodFactor) {
                var lodDistances = new float4x2(float.PositiveInfinity);
                for (var i = 0; i < lods.Length; ++i) {
                    var lodDistance = (lodGroup.size / lods[i].screenRelativeTransitionHeight);
                    lodDistances.Set(i, lodDistance);
                }

                lodDistances *= lodFactor;
                return lodDistances;
            }
        }

        public struct PrefabRuntime {
            public RendererAuthoring[] renderers;
            public float4x2 lodDistances;
            public AABB localBounds;

            public PrefabRuntime(in PrefabAuthoring prefabAuthoring, in LeshyQualityManager qualityManager) {
                var hasBillboard = prefabAuthoring.HasBillboard;
                var spawnDistance = qualityManager.SpawnDistance(prefabAuthoring.prefabType);
                var shadows = qualityManager.Shadows(prefabAuthoring.prefabType);
                var motionMode = MotionMode(prefabAuthoring.prefabType);

                lodDistances = CutLodDistances(prefabAuthoring.lodDistances, spawnDistance);
                localBounds = prefabAuthoring.localBounds;

                byte validLods = 0;
                var billboardLod = -1;
                for (var i = 0; i < 8; i++) {
                    if (float.IsFinite(lodDistances.Get(i))) {
                        validLods |= (byte)(1 << i);
                    } else if (hasBillboard && billboardLod == -1) {
                        var billboardDistance = qualityManager.BillboardDistance(prefabAuthoring.prefabType);
                        if (lodDistances.Get(i - 1) < billboardDistance) {
                            lodDistances.Set(i, billboardDistance);
                        }

                        billboardLod = i;
                    }
                }

                var renderersCount = 0;
                for (var i = 0; i < prefabAuthoring.renderers.Length; i++) {
                    if ((prefabAuthoring.renderers[i].lodMask & validLods) != 0) {
                        ++renderersCount;
                    }
                }

                if (hasBillboard) {
                    renderersCount++;
                }

                renderers = new RendererAuthoring[renderersCount];
                var renderersIndex = 0;
                for (var i = 0; i < prefabAuthoring.renderers.Length; i++) {
                    if ((prefabAuthoring.renderers[i].lodMask & validLods) != 0) {
                        var renderer = prefabAuthoring.renderers[i];
                        var filterSettings = renderer.filterSettings;
                        filterSettings.shadowCastingMode = shadows;
                        filterSettings.motionVectorGenerationMode = motionMode;
                        renderer.filterSettings = filterSettings;
                        renderers[renderersIndex++] = renderer;
                    }
                }

                if (hasBillboard) {
                    var renderer = prefabAuthoring.Billboard;
                    var filterSettings = renderer.filterSettings;
                    filterSettings.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.filterSettings = filterSettings;
                    renderer.lodMask = (byte)(1 << billboardLod);

                    renderers[renderersIndex] = renderer;
                }
            }

            static float4x2 CutLodDistances(float4x2 prefabLODDistances, float spawnDistance) {
                for (var i = 8 - 1; i >= 1; i--) {
                    var lodDistance = prefabLODDistances.Get(i);
                    if (lodDistance > spawnDistance) {
                        var prevLodDistance = prefabLODDistances.Get(i - 1);
                        if (prevLodDistance > spawnDistance) {
                            prefabLODDistances.Set(i, float.PositiveInfinity);
                        } else {
                            prefabLODDistances.Set(i, spawnDistance);
                        }
                    }
                }

                prefabLODDistances.Set(0, math.min(spawnDistance, prefabLODDistances.Get(0)));
                return prefabLODDistances;
            }

            static MotionVectorGenerationMode MotionMode(PrefabType prefabType) {
                return prefabType switch {
                    PrefabType.Grass => MotionVectorGenerationMode.Object,
                    PrefabType.Plant => MotionVectorGenerationMode.Object,
                    PrefabType.Ivy => MotionVectorGenerationMode.Object,
                    PrefabType.Object => MotionVectorGenerationMode.Camera,
                    PrefabType.Tree => MotionVectorGenerationMode.Object,
                    PrefabType.LargeObject => MotionVectorGenerationMode.Camera,
                    _ => throw new ArgumentOutOfRangeException(nameof(prefabType), prefabType, null),
                };
            }
        }

        [Serializable]
        public struct CellSizeByType {
            public PrefabType prefabType;
            public float cellSize;
        }

        public enum PrefabType : byte {
            Grass = 0,
            Plant = 1,
            Object = 2,
            LargeObject = 3,
            Tree = 4,
            Ivy = 5,
        }

#if UNITY_EDITOR
        public struct Editor_Accessor {
            readonly LeshyPrefabs _leshyPrefabs;

            public Editor_Accessor(LeshyPrefabs leshyPrefabs) {
                _leshyPrefabs = leshyPrefabs;
            }

            public CellSizeByType[] CellSizeByVegetationType => _leshyPrefabs.cellSizeByVegetationType;

            public void Set(PrefabAuthoring[] prefabs, VspRemap[] vspRemaps, HandPlacedLeshyInstanceRemap[] handPlacedRemaps,
                List<GameObject> inputPrefabs) {
                _leshyPrefabs.prefabs = prefabs;
                _leshyPrefabs.vspRemaps = vspRemaps;
                _leshyPrefabs.handPlacedRemaps = handPlacedRemaps;
#if !ADDRESSABLES_BUILD
                _leshyPrefabs.inputPrefabs = inputPrefabs;
#endif
            }
        }
#endif
    }
}