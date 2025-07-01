using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.TG.Editor.Helpers;
using Awaken.TG.LeshyRenderer;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using Awaken.Utility.UI;
using Sirenix.OdinInspector.Editor;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Graphics.LeshyRenderer {
    [CustomEditor(typeof(LeshyPrefabs))]
    public class LeshyPrefabsEditor : OdinEditor {
        // PersistentVegetationStorage _persistentVegetationStorage;
        // VegetationSystemPro _vegetationSystemPro;

        protected override void OnEnable() {
            base.OnEnable();
            InitPreview();
        }

        protected override void OnDisable() {
            base.OnDisable();
            DisposePreview();
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            // var leshyPrefabs = (LeshyPrefabs)target;
            // _vegetationSystemPro = EditorGUILayout.ObjectField(_vegetationSystemPro, typeof(VegetationSystemPro), true) as VegetationSystemPro;
            // MapScene mapScene = null;
            // if (_vegetationSystemPro != null) {
            //     mapScene = GameObjects.FindComponentByTypeInScene<MapScene>(_vegetationSystemPro.gameObject.scene, false);
            // }
            //
            // if (_vegetationSystemPro && mapScene && GUILayout.Button("Bake Vegetation Prefabs")) {
            //     BakePrefabs(leshyPrefabs, _vegetationSystemPro, LeshyManagerEditor.GetHandPlacedInstances(mapScene));
            // }
            //
            // _persistentVegetationStorage = EditorGUILayout.ObjectField(_persistentVegetationStorage, typeof(PersistentVegetationStorage), true) as PersistentVegetationStorage;
            // if (_vegetationSystemPro && _persistentVegetationStorage && GUILayout.Button("Transform VSP bake")) {
            //     LeshyDataBaker.TransformBakedVegetation(leshyPrefabs, _vegetationSystemPro, _persistentVegetationStorage,
            //         LeshyManagerEditor.GetHandPlacedInstances(mapScene));
            // }
        }

        // public static void BakePrefabs(LeshyPrefabs leshyPrefabs, VegetationSystemPro vegetationSystemPro,
        //     List<LeshyObjectSettings> handPlacedInstances) {
        //     AssetDatabase.StartAssetEditing();
        //     try {
        //         foreach (var prefabAuthoring in leshyPrefabs.Prefabs) {
        //             if (prefabAuthoring.HasCollider) {
        //                 var path = AssetDatabase.GetAssetPath(prefabAuthoring.colliders);
        //                 AssetDatabase.DeleteAsset(path);
        //             }
        //         }
        //     } finally {
        //         AssetDatabase.StopAssetEditing();
        //     }
        //
        //     Bake(leshyPrefabs, vegetationSystemPro, handPlacedInstances);
        //     EditorUtility.SetDirty(leshyPrefabs);
        // }

        // static void Bake(LeshyPrefabs leshyPrefabs, VegetationSystemPro vsp, List<LeshyObjectSettings> handPlacedInstances) {
        //     var accessor = new LeshyPrefabs.Editor_Accessor(leshyPrefabs);
        //
        //     var allVspItems = vsp.VegetationPackageProList.SelectMany(static l => l.VegetationInfoList).ToArray();
        //     var vspSettings = vsp.VegetationSettings;
        //
        //     var vspRemaps = new List<LeshyPrefabs.VspRemap>();
        //     var handPlacedRemaps = new List<LeshyPrefabs.HandPlacedLeshyInstanceRemap>();
        //
        //     var inputPrefabs = new List<GameObject>();
        //
        //     var cellSizes = new Dictionary<LeshyPrefabs.PrefabType, float>();
        //     foreach (var cellSizeByVegetation in accessor.CellSizeByVegetationType) {
        //         cellSizes[cellSizeByVegetation.prefabType] = cellSizeByVegetation.cellSize;
        //     }
        //
        //     string leshyPrefabsDirectory = GetLeshyPrefabsDirectory(leshyPrefabs, vsp.gameObject.scene.name);
        //     if (!Directory.Exists(leshyPrefabsDirectory)) {
        //         Directory.CreateDirectory(leshyPrefabsDirectory);
        //     }
        //
        //     var vspItemsPrefabs = new List<LeshyPrefabs.PrefabAuthoring>();
        //     foreach (var vspItem in allVspItems) {
        //         BakeVspItem(vspItem, vspRemaps, leshyPrefabsDirectory, vspSettings, cellSizes, vspItemsPrefabs, inputPrefabs);
        //     }
        //
        //     var prefabToColliderPrefabMap = new Dictionary<GameObject, GameObject>(10);
        //     int handPlacedInstancesCount = handPlacedInstances.Count;
        //     var handPlacedInstancesPrefabs = new List<LeshyPrefabs.PrefabAuthoring>();
        //     for (int i = 0; i < handPlacedInstancesCount; i++) {
        //         BakeLeshyObject(handPlacedInstances[i], i, prefabToColliderPrefabMap, handPlacedRemaps, leshyPrefabsDirectory, vspSettings, cellSizes, 
        //             handPlacedInstancesPrefabs, vspItemsPrefabs.Count, inputPrefabs);
        //     }
        //     var allPrefabs = ArrayUtils.CreateArray(vspItemsPrefabs, handPlacedInstancesPrefabs);
        //     accessor.Set(allPrefabs, vspRemaps.ToArray(), handPlacedRemaps.ToArray(), inputPrefabs.ToList());
        // }

        // static void BakeVspItem(VegetationItemInfoPro vspItem, List<LeshyPrefabs.VspRemap> vspRemaps, string leshyPrefabsDirectory,
        //     VegetationSettings vspSettings, Dictionary<LeshyPrefabs.PrefabType, float> cellSizes,
        //     List<LeshyPrefabs.PrefabAuthoring> prefabs,
        //     List<GameObject> inputPrefabs) {
        //     var id = vspItem.VegetationItemID;
        //
        //     var vspPrefab = vspItem.PrefabType == VegetationPrefabType.Texture ? Resources.Load<GameObject>("DefaultGrassPatch") : vspItem.VegetationPrefab;
        //
        //     if (!vspPrefab) {
        //         Log.Important?.Error($"Empty prefab for item: {vspItem.Name} {vspItem.VegetationItemID}");
        //         return;
        //     }
        //
        //     DrakeLodGroup drakeLodGroup = null;
        //     if (vspPrefab.TryGetComponent(out LODGroup lodGroup) == false &&
        //         vspPrefab.TryGetComponent(out drakeLodGroup) == false) {
        //         Log.Important?.Error($"Vsp prefab {vspPrefab.name} does not have {nameof(LODGroup)} or {nameof(DrakeLodGroup)} component", vspPrefab);
        //         return;
        //     }
        //
        //     var (layer, prefabType) = ExtractDescriptorProperties(vspItem, vspSettings);
        //     var (colliderPrefab, colliderDistance) = GetVspColliderData(leshyPrefabsDirectory, vspItem, vspSettings);
        //
        //     var lodFactor = vspItem.LODFactor;
        //     var prefabCellSize = cellSizes[prefabType];
        //     var useBillboards = vspItem.UseBillboards;
        //
        //     LeshyPrefabs.PrefabAuthoring prefabAuthoring;
        //     if (lodGroup) {
        //         prefabAuthoring = LeshyPrefabs.PrefabAuthoring.FromLODGroup(lodGroup, lodFactor,
        //             prefabType, prefabCellSize, layer, colliderDistance, colliderPrefab, useBillboards);
        //     } else {
        //         prefabAuthoring = LeshyPrefabs.PrefabAuthoring.FromDrakeLODGroup(drakeLodGroup, lodFactor,
        //             prefabType, prefabCellSize, layer, colliderDistance, colliderPrefab, useBillboards);
        //     }
        //
        //
        //     var prefabIndex = prefabs.IndexOf(prefabAuthoring);
        //     if (prefabIndex == -1) {
        //         prefabIndex = prefabs.Count;
        //         Debug.Log($"Prefab index {prefabIndex}. prefab: {vspPrefab.name} collider: {colliderPrefab} [{vspItem.ColliderType}]", vspPrefab);
        //         prefabs.Add(prefabAuthoring);
        //         inputPrefabs.Add(vspPrefab);
        //     }
        //
        //     vspRemaps.Add(new LeshyPrefabs.VspRemap(id, prefabIndex));
        // }

        // static void BakeLeshyObject(LeshyObjectSettings leshyObjectInstanceSettings, int index, Dictionary<GameObject, GameObject> prefabToColliderPrefabMap,
        //     List<LeshyPrefabs.HandPlacedLeshyInstanceRemap> handPlacedRemaps,
        //     string leshyPrefabsDirectory, VegetationSettings vspSettings, Dictionary<LeshyPrefabs.PrefabType, float> cellSizes,
        //     List<LeshyPrefabs.PrefabAuthoring> prefabs, int prefabsStartIndex, List<GameObject> inputPrefabs) {
        //     if (leshyObjectInstanceSettings == null) {
        //         Log.Important?.Error($"Leshy object instance at index {index} is null");
        //         return;
        //     }
        //
        //     var leshyObjectGO = leshyObjectInstanceSettings.gameObject;
        //     DrakeLodGroup drakeLodGroup = null;
        //     if (leshyObjectGO.TryGetComponent(out LODGroup lodGroup) == false &&
        //         leshyObjectGO.TryGetComponent(out drakeLodGroup) == false) {
        //         Log.Important?.Error($"Leshy object instance {leshyObjectGO.name} does not have {nameof(LODGroup)} or {nameof(DrakeLodGroup)} component", leshyObjectGO);
        //         return;
        //     }
        //
        //     GameObject leshyObjectPrefab;
        //     var hasPrefab = PrefabUtility.GetPrefabInstanceStatus(leshyObjectGO) == PrefabInstanceStatus.Connected;
        //     if (hasPrefab) {
        //         leshyObjectPrefab = PrefabUtility.GetCorrespondingObjectFromSource(leshyObjectGO);
        //         if (leshyObjectPrefab == null) {
        //             Log.Important?.Warning($"Leshy prefab for {leshyObjectGO.name} is null. Using instance as prefab", leshyObjectGO);
        //             leshyObjectPrefab = leshyObjectGO;
        //         }
        //     } else {
        //         Log.Important?.Warning($"Leshy object instance {leshyObjectGO.name} is not a part of a prefab. Using instance as prefab", leshyObjectGO);
        //         leshyObjectPrefab = leshyObjectGO;
        //     }
        //
        //     var leshyObjectPrefabSettings = leshyObjectPrefab.GetComponent<LeshyObjectSettings>();
        //     if (!leshyObjectPrefabSettings) {
        //         Log.Important?.Warning($"Leshy object prefab {leshyObjectPrefab.name} does not have {nameof(LeshyObjectSettings)} component. Using instance as prefab", leshyObjectGO);
        //         leshyObjectPrefab = leshyObjectGO;
        //         leshyObjectPrefabSettings = leshyObjectInstanceSettings;
        //     }
        //
        //     var lodFactor = leshyObjectInstanceSettings.lodFactor;
        //     var prefabType = leshyObjectInstanceSettings.prefabType;
        //     var prefabCellSize = cellSizes[prefabType];
        //     var layerMask = PrefabTypeToLayerMask(prefabType, vspSettings);
        //     var layer = layerMask.value;
        //     var colliderDistance = vspSettings.GetVegetationDistance() * leshyObjectInstanceSettings.colliderDistanceFactor;
        //     var useBillboards = leshyObjectInstanceSettings.useBillboards;
        //
        //     if (prefabToColliderPrefabMap.TryGetValue(leshyObjectPrefab, out GameObject colliderPrefab) == false) {
        //         if (leshyObjectPrefabSettings.collidersGO == null) {
        //             prefabToColliderPrefabMap.Add(leshyObjectPrefab, null);
        //         } else {
        //             var colliderGO = new GameObject($"{leshyObjectGO.name}_Collider");
        //             colliderGO.layer = layerMask;
        //             AddCollidersFromPrefab(leshyObjectPrefabSettings.collidersGO, layerMask, colliderGO);
        //             colliderPrefab = SaveAsLeshyColliderPrefabAsset(leshyPrefabsDirectory, colliderGO);
        //             prefabToColliderPrefabMap.Add(leshyObjectPrefab, colliderPrefab);
        //         }
        //     }
        //
        //     LeshyPrefabs.PrefabAuthoring prefabAuthoring;
        //     if (lodGroup) {
        //         prefabAuthoring = LeshyPrefabs.PrefabAuthoring.FromLODGroup(lodGroup, lodFactor, prefabType,
        //             prefabCellSize, layer, colliderDistance, colliderPrefab, useBillboards);
        //     } else {
        //         prefabAuthoring = LeshyPrefabs.PrefabAuthoring.FromDrakeLODGroup(drakeLodGroup, lodFactor, prefabType,
        //             prefabCellSize, layer, colliderDistance, colliderPrefab, useBillboards);
        //     }
        //
        //
        //     var prefabIndex = prefabs.IndexOf(prefabAuthoring);
        //     if (prefabIndex == -1) {
        //         prefabIndex = prefabs.Count + prefabsStartIndex;
        //         Log.Debug?.Info($"Prefab index {prefabIndex}. prefab: {leshyObjectPrefab.name}. GameObject: {leshyObjectGO.name}", leshyObjectGO);
        //         prefabs.Add(prefabAuthoring);
        //         var prefab = hasPrefab ? PrefabUtility.GetCorrespondingObjectFromOriginalSource(leshyObjectGO) : null;
        //         inputPrefabs.Add(prefab);
        //     } else {
        //         prefabIndex += prefabsStartIndex;
        //     }
        //
        //     handPlacedRemaps.Add(new(index, prefabIndex));
        // }

        // static (int, LeshyPrefabs.PrefabType) ExtractDescriptorProperties(VegetationItemInfoPro vspItem, VegetationSettings vspSettings) {
        //     var vegetationType = vspItem.VegetationType;
        //     return vegetationType switch {
        //         VegetationType.Grass => (vspSettings.GrassLayer.value, LeshyPrefabs.PrefabType.Grass),
        //         VegetationType.Plant => (vspSettings.PlantLayer.value, LeshyPrefabs.PrefabType.Plant),
        //         VegetationType.Objects => (vspSettings.ObjectLayer.value, LeshyPrefabs.PrefabType.Object),
        //         VegetationType.LargeObjects => (vspSettings.LargeObjectLayer.value, LeshyPrefabs.PrefabType.LargeObject),
        //         VegetationType.Tree => (vspSettings.TreeLayer.value, LeshyPrefabs.PrefabType.Tree),
        //         _ => throw new NotImplementedException($"Case not implemented for type {vegetationType}")
        //     };
        // }

        // static LayerMask PrefabTypeToLayerMask(LeshyPrefabs.PrefabType prefabType, VegetationSettings vspSettings) {
        //     return prefabType switch {
        //         LeshyPrefabs.PrefabType.Grass => vspSettings.GrassLayer,
        //         LeshyPrefabs.PrefabType.Plant => vspSettings.PlantLayer,
        //         LeshyPrefabs.PrefabType.Object => vspSettings.ObjectLayer,
        //         LeshyPrefabs.PrefabType.LargeObject => vspSettings.LargeObjectLayer,
        //         LeshyPrefabs.PrefabType.Tree => vspSettings.TreeLayer,
        //         LeshyPrefabs.PrefabType.Ivy => vspSettings.LargeObjectLayer,
        //         _ => throw new NotImplementedException($"Case not implemented for type {prefabType}")
        //     };
        // }

        // static (GameObject colliderPrefab, float colliderDistance) GetVspColliderData(string leshyPrefabsDirectory,
        //     VegetationItemInfoPro vspItem, VegetationSettings vspSettings) {
        //     var colliderType = vspItem.ColliderType;
        //     if (colliderType == ColliderType.Disabled) {
        //         return (null, 0f);
        //     }
        //
        //     var layer = vspSettings.GetLayer(vspItem.VegetationType);
        //     var distance = vspSettings.GetVegetationDistance() * vspItem.ColliderDistanceFactor;
        //     var colliderRootGO = new GameObject($"{vspItem.VegetationPrefab.name}_Collider");
        //     colliderRootGO.layer = layer;
        //     if (colliderType == ColliderType.FromPrefab) {
        //         var prefab = vspItem.VegetationPrefab;
        //         AddCollidersFromPrefab(prefab, layer, colliderRootGO);
        //     } else if (colliderType == ColliderType.Capsule) {
        //         var collider = colliderRootGO.AddComponent<CapsuleCollider>();
        //         collider.center = vspItem.ColliderOffset;
        //         collider.height = vspItem.ColliderHeight;
        //         collider.radius = vspItem.ColliderRadius;
        //     } else if (colliderType == ColliderType.Sphere) {
        //         var collider = colliderRootGO.AddComponent<SphereCollider>();
        //         collider.radius = vspItem.ColliderRadius;
        //     } else if (colliderType == ColliderType.Box) {
        //         var collider = colliderRootGO.AddComponent<BoxCollider>();
        //         collider.center = vspItem.ColliderOffset;
        //         collider.size = vspItem.ColliderSize;
        //     } else if (colliderType == ColliderType.CustomMesh) {
        //         var collider = colliderRootGO.AddComponent<MeshCollider>();
        //         collider.sharedMesh = vspItem.ColliderMesh;
        //         collider.convex = vspItem.ColliderConvex;
        //     } else if (colliderType == ColliderType.Mesh) {
        //         var collider = colliderRootGO.AddComponent<MeshCollider>();
        //         collider.sharedMesh = vspItem.VegetationPrefab.GetComponentInChildren<MeshFilter>().sharedMesh;
        //         collider.convex = vspItem.ColliderConvex;
        //     }
        //
        //     if (colliderRootGO.transform.childCount == 0 && colliderRootGO.GetComponents<Component>().Length == 1) {
        //         DestroyImmediate(colliderRootGO);
        //         return (null, 0f);
        //     }
        //
        //     colliderRootGO = SaveAsLeshyColliderPrefabAsset(leshyPrefabsDirectory, colliderRootGO);
        //
        //     return (colliderRootGO, distance);
        // }

        static void AddCollidersFromPrefab(GameObject prefab, LayerMask layer, GameObject colliderRootGO) {
            var prefabInstance = Instantiate(prefab);
            var instanceTransform = prefabInstance.transform;

            instanceTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            instanceTransform.localScale = Vector3.one;

            var colliders = prefabInstance.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders) {
                collider.gameObject.layer = layer;
                collider.transform.SetParent(colliderRootGO.transform, true);

                var components = collider.GetComponents<Component>();
                foreach (var component in components) {
                    if (component is Transform or Collider) {
                        continue;
                    }

                    DestroyImmediate(component);
                }
            }

            foreach (var vegetationCollider in colliders) {
                for (int i = vegetationCollider.transform.childCount - 1; i >= 0; i--) {
                    DestroyImmediate(vegetationCollider.transform.GetChild(i).gameObject);
                }
            }

            if (instanceTransform.parent == null) {
                DestroyImmediate(prefabInstance);
            }

            foreach (var collider in colliders) {
                if (collider == null) {
                    continue;
                }

                if (collider.transform.localToWorldMatrix == Matrix4x4.identity) {
                    var componentCopy = colliderRootGO.AddComponent(collider.GetType());
                    var preset = new UnityEditor.Presets.Preset(collider);
                    preset.ApplyTo(componentCopy);
                    DestroyImmediate(collider.gameObject);
                }
            }
        }

        static string GetLeshyPrefabsDirectory(LeshyPrefabs leshyPrefabs, string sceneName) {
            var leshyPrefabsAssetPath = AssetDatabase.GetAssetPath(leshyPrefabs);
            leshyPrefabsAssetPath = leshyPrefabsAssetPath.Substring(0, leshyPrefabsAssetPath.LastIndexOf('/') + 1);
            var leshyPrefabsDirectory = $"{leshyPrefabsAssetPath}{sceneName}";

            return leshyPrefabsDirectory;
        }

        static GameObject SaveAsLeshyColliderPrefabAsset(string leshyPrefabsDirectory, GameObject colliderRootGO) {
            var path = $"{leshyPrefabsDirectory}/{colliderRootGO.name}.prefab";
            var newColliderParent = PrefabUtility.SaveAsPrefabAsset(colliderRootGO, path);
            DestroyImmediate(colliderRootGO);
            return newColliderParent;
        }

        // === Preview
        static readonly int SliderHash = "Slider".GetHashCode();
        PreviewRenderUtility _previewUtility;
        Vector2 _previewDir = new Vector2(0, -20);
        int _selectedPreviewIndex = -1;
        int _selectedLodIndex;
        float _distanceModifier = 12f;
        GUIStyle _statsStyle;
        string _rendererInfo;
        float _rendererInfoHeight;

        void InitPreview() {
            _previewUtility = new PreviewRenderUtility();
        }

        void DisposePreview() {
            _previewUtility.Cleanup();
        }

        public override bool HasPreviewGUI() {
            var vegetationPrefabs = (LeshyPrefabs)target;
            return vegetationPrefabs.Prefabs.IsNotNullOrEmpty();
        }

        public override void OnPreviewSettings() {
            base.OnPreviewSettings();

            var vegetationPrefabs = (LeshyPrefabs)target;
            var maxPrefab = vegetationPrefabs.Prefabs.Length - 1;

            EditorGUI.BeginChangeCheck();

            if (_selectedPreviewIndex == -1) {
                _selectedPreviewIndex = 0;
                GUI.changed = true;
            }

            GUILayout.Label("Prefab:");
            using (new EditorGUI.DisabledScope(_selectedPreviewIndex == 0)) {
                if (GUILayout.Button("<", GUILayout.Width(20))) {
                    _selectedPreviewIndex--;
                    _selectedLodIndex = 0;
                }
            }

            EditorGUI.BeginChangeCheck();
            _selectedPreviewIndex = EditorGUILayout.IntField(_selectedPreviewIndex, GUILayout.Width(30));
            if (EditorGUI.EndChangeCheck()) {
                _selectedPreviewIndex = math.clamp(_selectedPreviewIndex, 0, maxPrefab);
                _selectedLodIndex = 0;
            }

            using (new EditorGUI.DisabledScope(_selectedPreviewIndex == maxPrefab)) {
                if (GUILayout.Button(">", GUILayout.Width(20))) {
                    _selectedPreviewIndex++;
                    _selectedLodIndex = 0;
                }
            }

            GUILayout.Space(24);
            var prefab = vegetationPrefabs.Prefabs[_selectedPreviewIndex];
            var maxLod = prefab.renderers.Length - 1;

            GUILayout.Label("Lod:");
            using (new EditorGUI.DisabledScope(_selectedLodIndex == 0)) {
                if (GUILayout.Button("<", GUILayout.Width(20))) {
                    _selectedLodIndex--;
                }
            }

            EditorGUI.BeginChangeCheck();
            _selectedLodIndex = EditorGUILayout.IntField(_selectedLodIndex, GUILayout.Width(25));
            if (EditorGUI.EndChangeCheck()) {
                _selectedLodIndex = math.clamp(_selectedLodIndex, 0, maxLod);
            }

            using (new EditorGUI.DisabledScope(_selectedLodIndex == maxLod)) {
                if (GUILayout.Button(">", GUILayout.Width(20))) {
                    _selectedLodIndex++;
                }
            }

            using (new EditorGUI.DisabledScope(!prefab.HasBillboard)) {
                var buttonContent = new GUIContent("B", "Billboard");
                if (GUILayout.Button(buttonContent, GUILayout.Width(20))) {
                    _selectedLodIndex = maxLod;
                }
            }

            if (EditorGUI.EndChangeCheck()) {
                StringBuilder infoBuilder = new StringBuilder();
                infoBuilder.Append(prefab.prefabType);
                if (prefab.HasCollider) {
                    infoBuilder.Append(" with collider");
                } else {
                    infoBuilder.Append(" no collider");
                }

                infoBuilder.AppendLine();
                infoBuilder.Append("Vertices: ");
                infoBuilder.Append(prefab.renderers[_selectedLodIndex].mesh.vertexCount);
                infoBuilder.AppendLine();
                infoBuilder.Append("Submeshes: ");
                infoBuilder.Append(prefab.renderers[_selectedLodIndex].mesh.subMeshCount);
                infoBuilder.AppendLine();
                infoBuilder.Append("Materials: ");
                infoBuilder.Append(prefab.renderers[_selectedLodIndex].materials.Length);
                _rendererInfo = infoBuilder.ToString();

                _statsStyle ??= CreateStatsStyle();
                var content = new GUIContent(_rendererInfo);
                _rendererInfoHeight = _statsStyle.CalcSize(content).y;

                content.text = prefab.renderers[_selectedLodIndex].mesh.name;
                var buttonsHeight = _statsStyle.CalcSize(content).y;
                foreach (var material in prefab.renderers[_selectedLodIndex].materials) {
                    content.text = material.name;
                    buttonsHeight += _statsStyle.CalcSize(content).y;
                }

                _rendererInfoHeight = math.max(_rendererInfoHeight, buttonsHeight);
            }
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background) {
            var bottomRow = new Rect(r.x, r.yMax - _rendererInfoHeight, r.width, _rendererInfoHeight);
            _previewDir = Drag2D(_previewDir, r, bottomRow);
            _previewDir.y = Mathf.Clamp(_previewDir.y, -89.0f, 89.0f);

            var vegetationPrefabs = (LeshyPrefabs)target;
            var prefab = vegetationPrefabs.Prefabs[_selectedPreviewIndex];

            if (Event.current.type == EventType.Repaint) {
                _previewUtility.BeginPreview(r, background);

                DoRenderPreview(prefab);

                _previewUtility.EndAndDrawPreview(r);
            }

            var lod = prefab.renderers[_selectedLodIndex];
            DrawInfo(lod, bottomRow);
        }

        void DoRenderPreview(LeshyPrefabs.PrefabAuthoring prefab) {
            var bounds = prefab.localBounds.ToBounds();

            var halfSize = bounds.extents.magnitude;
            var distance = halfSize * _distanceModifier;

            var viewDir = -(_previewDir / 100.0f);

            _previewUtility.camera.transform.position = bounds.center +
                                                        (new Vector3(Mathf.Sin(viewDir.x) * Mathf.Cos(viewDir.y),
                                                             Mathf.Sin(viewDir.y),
                                                             Mathf.Cos(viewDir.x) * Mathf.Cos(viewDir.y)) *
                                                         distance);

            _previewUtility.camera.transform.LookAt(bounds.center);
            _previewUtility.camera.nearClipPlane = 0.05f;
            _previewUtility.camera.farClipPlane = 1000.0f;

            _previewUtility.lights[0].intensity = 1.0f;
            _previewUtility.lights[0].transform.rotation = Quaternion.Euler(50f, 50f, 0);
            _previewUtility.lights[1].intensity = 1.0f;
            _previewUtility.ambientColor = new Color(.2f, .2f, .2f, 0);

            var renderer = prefab.renderers[_selectedLodIndex];

            var matrix = Matrix4x4.TRS(Vector3.up * (bounds.extents.y * 0.25f), quaternion.identity, Vector3.one);
            for (int i = 0; i < renderer.materials.Length; i++) {
                _previewUtility.DrawMesh(renderer.mesh, matrix, renderer.materials[i], i);
            }

            _previewUtility.Render(Unsupported.useScriptableRenderPipeline);
        }

        void DrawInfo(LeshyPrefabs.RendererAuthoring rendererAuthoring, Rect bottomRow) {
            var wholeRect = new PropertyDrawerRects(bottomRow);

            var leftSide = wholeRect.AllocateLeft(bottomRow.width * 0.3f);
            var rightSide = wholeRect;

            GUI.Label(leftSide, _rendererInfo, _statsStyle);

            var meshButtonRect = rightSide.AllocateLine();
            if (GUI.Button(meshButtonRect, rendererAuthoring.mesh.name)) {
                Selection.activeObject = rendererAuthoring.mesh;
            }

            foreach (var material in rendererAuthoring.materials) {
                var materialRect = rightSide.AllocateLine();
                var info = $"{material.name} - {material.shader.name.Split('/')[^1]}";
                if (GUI.Button(materialRect, info)) {
                    Selection.activeObject = material;
                }
            }
        }

        Vector2 Drag2D(Vector2 scrollPosition, Rect position, Rect bottomRow) {
            int controlId = GUIUtility.GetControlID(SliderHash, FocusType.Passive);
            Event current = Event.current;
            switch (current.GetTypeForControl(controlId)) {
                case EventType.MouseDown:
                    if (position.Contains(current.mousePosition) && position.width > 50.0 && !bottomRow.Contains(current.mousePosition)) {
                        GUIUtility.hotControl = controlId;
                        current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }

                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId)
                        GUIUtility.hotControl = 0;
                    EditorGUIUtility.SetWantsMouseJumping(0);
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId) {
                        scrollPosition -= current.delta *
                                          (current.shift ? 3f : 1f) /
                                          Mathf.Min(position.width, position.height) *
                                          140f;
                        current.Use();
                        GUI.changed = true;
                    }

                    break;

                case EventType.ScrollWheel:
                    if (position.Contains(current.mousePosition) && position.width > 50.0) {
                        var speed = 0.1f;
                        if (current.shift) {
                            speed *= 2f;
                        } else if (current.control) {
                            speed *= 0.3f;
                        }

                        _distanceModifier += current.delta.y * speed;
                        current.Use();
                        GUI.changed = true;
                    }

                    break;
            }

            return scrollPosition;
        }

        GUIStyle CreateStatsStyle() {
            var statsStyle = new GUIStyle(EditorStyles.label);
            statsStyle.normal.textColor = Color.black;
            statsStyle.hover.textColor = Color.black;
            statsStyle.focused.textColor = Color.black;
            statsStyle.alignment = TextAnchor.MiddleCenter;
            return statsStyle;
        }
    }
}