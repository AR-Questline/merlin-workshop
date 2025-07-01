using System.Collections.Generic;
using System.Text;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Editor.Utility.Searching;
using Awaken.TG.LeshyRenderer;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Scenes.SceneConstructors.SubdividedScenes;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging.MemorySnapshots;
using Awaken.Utility.Editor;
using Awaken.Utility.GameObjects;
using Awaken.Utility.LowLevel;
using Awaken.Utility.LowLevel.Collections;
using Awaken.Utility.Maths;
using Sirenix.OdinInspector.Editor;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Editor.Graphics.LeshyRenderer {
    [CustomEditor(typeof(LeshyManager))]
    public class LeshyManagerEditor : OdinEditor {
        Color _cullInvisibleColor = new Color(0.4f, 0.5f, 1, 1);
        Color _cullVisibleColor = Color.green;
        int _cellWithPrefabIdFilter;
        int _maxCellId;
        int _cellInstancesDebug = -1;

        bool _verbose = true;
        bool _showBounds = true;
        bool _showSpheres;

        bool _showMemoryInfo;

        StringBuilder _cellDataBuilder = new StringBuilder();
        GUIStyle _cellLabelStyle;

        protected override void OnEnable() {
            base.OnEnable();
            EditorApplication.update -= Repaint;
            EditorApplication.update += Repaint;

            _cellLabelStyle = new GUIStyle(EditorStyles.label);

            _maxCellId = -1;
            var leshyManager = (LeshyManager)target;
            if (leshyManager.EDITOR_Cells.cellsCatalog.IsCreated) {
                foreach (var cell in leshyManager.EDITOR_Cells.cellsCatalog) {
                    if (cell.prefabId > _maxCellId) {
                        _maxCellId = cell.prefabId;
                    }
                }
            }
        }

        protected override void OnDisable() {
            base.OnDisable();
            EditorApplication.update -= Repaint;
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            var leshyManager = (LeshyManager)target;

            DrawRuntimeData(leshyManager);
            DrawBaking(leshyManager);
        }

        // public static void FullBake(LeshyManager leshyManager, VegetationSystemPro vsp, PersistentVegetationStorage vspPersistence, MapScene mapScene,
        //     bool isBatchBake = false) {
        //     CheckScenesToSave();
        //
        //     using var bakingScope = BakingScope.Enter();
        //     using var trackingScope = new LeaksTrackingScope(NativeLeakDetectionMode.Disabled, true);
        //
        //     SubdividedScene subdividedScene = null;
        //     if (mapScene is SubdividedScene) {
        //         subdividedScene = mapScene as SubdividedScene;
        //         new SubdividedScene.EditorAccess(subdividedScene).LoadAllScenes(true);
        //     }
        //
        //     BakeVSP(vsp, vspPersistence);
        //     var handPlacedInstances = GetHandPlacedInstances(mapScene);
        //     BakeLeshy(leshyManager, vsp, vspPersistence, handPlacedInstances);
        //     
        //     if (subdividedScene != null) {
        //         new SubdividedScene.EditorAccess(subdividedScene).UnloadAllScenes(true, false);
        //     }
        //
        //     if (isBatchBake == false) {
        //         FinishBaking();
        //     }
        // }

        public static List<LeshyObjectSettings> GetHandPlacedInstances(MapScene mapScene) {
            if (mapScene == null) {
                return null;
            }

            var mapScenes = GetMapLoadedScenes(mapScene);
            var handPlacedInstancesLeshyPrefabSettings = new List<LeshyObjectSettings>(20);
            for (int i = 0; i < mapScenes.Count; i++) {
                GameObjects.FindComponentsByTypeInScene(mapScenes[i], true, ref handPlacedInstancesLeshyPrefabSettings);
            }

            return handPlacedInstancesLeshyPrefabSettings;
        }

        void DrawRuntimeData(LeshyManager leshyManager) {
            if (!Application.isPlaying) {
                return;
            }

            _cullInvisibleColor = EditorGUILayout.ColorField("Cull Invisible Color", _cullInvisibleColor);
            _cullVisibleColor = EditorGUILayout.ColorField("Cull Visible Color", _cullVisibleColor);
            if (leshyManager.EDITOR_Cells.cellsCatalog.IsCreated) {
                _cellInstancesDebug = EditorGUILayout.IntSlider("Cell instances debug", _cellInstancesDebug, -1,
                    leshyManager.EDITOR_Cells.CellsCount);
            }

            if (_cellInstancesDebug == -1) {
                if (leshyManager.EDITOR_Cells.cellsCatalog.IsCreated && _maxCellId > -1) {
                    _cellWithPrefabIdFilter =
                        EditorGUILayout.IntSlider("Prefab cells filter", _cellWithPrefabIdFilter, -1, _maxCellId);
                }

                _verbose = EditorGUILayout.Toggle("Verbose", _verbose);
                _showBounds = EditorGUILayout.Toggle("Show bounds", _showBounds);
                _showSpheres = EditorGUILayout.Toggle("Show spheres", _showSpheres);
            }

            _showMemoryInfo = EditorGUILayout.Foldout(_showMemoryInfo, "Memory Info");
            if (_showMemoryInfo) {
                MemorySnapshotMemoryInfo.DrawOnGUI(leshyManager);
            }
        }

        void DrawBaking(LeshyManager leshyManager) {
            if (Application.isPlaying) {
                return;
            }

            if (!leshyManager.EDITOR_Prefabs) {
                EditorGUILayout.HelpBox("Assign leshy prefabs first", MessageType.Error);
                return;
            }

            // var vsp = FindAnyObjectByType<VegetationSystemPro>();
            // if (!vsp) {
            //     EditorGUILayout.HelpBox("Cannot find VegetationSystemPro in scene.", MessageType.Error);
            //     return;
            // }
            //
            // var vspPersistence = vsp.GetComponent<PersistentVegetationStorage>();
            // if (!vspPersistence) {
            //     EditorGUILayout.HelpBox("Cannot find PersistentVegetationStorage in scene.", MessageType.Error);
            //     return;
            // }
            //
            // var mapScene = FindAnyObjectByType<MapScene>(FindObjectsInactive.Include);
            // if (!mapScene) {
            //     EditorGUILayout.HelpBox($"Cannot find {nameof(MapScene)} in scene.", MessageType.Error);
            //     return;
            // }
            //
            // if (vspPersistence.DisablePersistentStorage) {
            //     EditorGUILayout.HelpBox("VSP has disabled persistent storage.", MessageType.Error);
            //     return;
            // }
            //
            // if (!vspPersistence.IsBaked) {
            //     //If baking is cancelled before it is finished (crash or TaskManager), baking flag remains enabled
            //     //and forces to load all subscenes
            //     if (EditorPrefs.GetBool("Baking") && GUILayout.Button("Clear Baking Flag")) {
            //         EditorPrefs.SetBool("Baking", false);
            //     }
            //
            //     if (GUILayout.Button("Full bake")) {
            //         FullBake(leshyManager, vsp, vspPersistence, mapScene);
            //     } else if (GUILayout.Button("Bake VSP & Leshy")) {
            //         BakeVSP(vsp, vspPersistence);
            //         BakeLeshy(leshyManager, vsp, vspPersistence, GetHandPlacedInstances(mapScene));
            //         FinishBaking();
            //     }
            // } else if (vspPersistence.IsBaked && GUILayout.Button("Bake Leshy")) {
            //     BakeLeshy(leshyManager, vsp, vspPersistence, GetHandPlacedInstances(mapScene));
            //     FinishBaking();
            // }
        }

        // static void BakeVSP(VegetationSystemPro vegetationSystemPro, PersistentVegetationStorage persistentVegetationStorage) {
        //     vegetationSystemPro.ForceDisable = false;
        //     vegetationSystemPro.ClearCache();
        //     vegetationSystemPro.RefreshVegetationSystem();
        //
        //     persistentVegetationStorage.DisablePersistentStorage = false;
        //     persistentVegetationStorage.EditorView.HardReset();
        //     persistentVegetationStorage.EditorView.InitializePersistentStorage();
        //
        //     vegetationSystemPro.ClearCache();
        //     vegetationSystemPro.RefreshVegetationSystem();
        //
        //     PersistentVegetationStorageEditor.BakeAllVegetationItemsFromAllBiomes(persistentVegetationStorage, vegetationSystemPro, true, false);
        // }

        // static void BakeLeshy(LeshyManager leshyManager, VegetationSystemPro vegetationSystemPro, PersistentVegetationStorage persistentVegetationStorage,
        //     List<LeshyObjectSettings> handPlacedInstances) {
        //     LeshyPrefabsEditor.BakePrefabs(leshyManager.EDITOR_Prefabs, vegetationSystemPro, handPlacedInstances);
        //     LeshyDataBaker.TransformBakedVegetation(leshyManager.EDITOR_Prefabs, vegetationSystemPro, persistentVegetationStorage, handPlacedInstances);
        //
        //     ClearVsp(vegetationSystemPro, persistentVegetationStorage);
        //
        //     // Enable Leshy
        //     leshyManager.EDITOR_SetDisabled(false);
        //     EditorUtility.SetDirty(leshyManager);
        // }

        // static void ClearVsp(VegetationSystemPro vegetationSystemPro, PersistentVegetationStorage persistentVegetationStorage) {
        //     // Clear VSP data
        //     PersistentVegetationStorageEditor.ClearAllItemsFromAllVegetationPackages(persistentVegetationStorage, vegetationSystemPro, false, false);
        //     persistentVegetationStorage.EditorView.HardReset();
        //     // Disable VSP
        //     vegetationSystemPro.ForceDisable = true;
        //     vegetationSystemPro.ClearCache();
        //     vegetationSystemPro.RefreshVegetationSystem();
        // }

        static void CheckScenesToSave() {
            var scenes = EditorSceneManager.GetSceneManagerSetup();
            var askToSave = true;

            for (int i = 0; i < scenes.Length; i++) {
                if (!scenes[i].isLoaded) {
                    continue;
                }

                var scene = SceneManager.GetSceneByPath(scenes[i].path);
                if (!scene.isDirty) {
                    continue;
                }

                var save = false;
                if (askToSave) {
                    // Show dialog to save scene
                    var result = EditorUtility.DisplayDialogComplex("Save scene",
                        $"Scene {scene.name} is dirty. Save changes?",
                        "Yes",
                        "No",
                        "Yes to all");
                    if (result == 0 || result == 2) {
                        save = true;
                    }

                    if (result == 2) {
                        askToSave = false;
                    }
                } else {
                    save = true;
                }

                if (save) {
                    EditorSceneManager.SaveScene(scene);
                }
            }
        }

        static List<Scene> GetMapLoadedScenes(MapScene mapSceneComponent) {
            List<Scene> mapScenes;
            if (mapSceneComponent is SubdividedScene subdividedSceneComponent) {
                mapScenes = new List<Scene>(subdividedSceneComponent.SubscenesCount);
                mapScenes.Add(mapSceneComponent.gameObject.scene);
                new SubdividedScene.EditorAccess(subdividedSceneComponent).GetLoadedScenes(true, mapScenes);
            } else {
                mapScenes = new List<Scene>(1) { mapSceneComponent.gameObject.scene };
            }

            return mapScenes;
        }

        public static void FinishBaking() {
            EditorApplication.Beep();
            AssetDatabase.SaveAssets();

            var window = ScriptableObject.CreateInstance<LeshyBakedGitPopup>();
            window.position = new Rect(50, 50, Screen.currentResolution.width - 100, Screen.currentResolution.height - 100);
            window.ShowPopup();
        }

        void OnSceneGUI() {
            if (!Application.isPlaying) {
                return;
            }

            var leshyManager = (LeshyManager)target;
            var cells = leshyManager.EDITOR_Cells;
            if (!cells.Created) {
                return;
            }

            if (_cellInstancesDebug != -1) {
                DebugCellInstances(leshyManager);
                return;
            }

            var cellsDistances = cells.cellsDistances;
            var frustumCellsPartialVisibility = cells.frustumPartialCellsVisibility;
            var frustumCellsFullVisibility = cells.frustumFullCellsVisibility;
            var distanceCellsVisibility = cells.distanceCellsVisibility;
            var finalCellsVisibility = cells.finalCellsVisibility;
            var takenRanges = leshyManager.EDITOR_Rendering.TakenRanges;

            if (_cellWithPrefabIdFilter != -1) {
                DrawCameraBasedHandles(_cellWithPrefabIdFilter, leshyManager);
            }

            var sceneCamera = SceneView.currentDrawingSceneView.camera;
            var cameraData = new float3x2(sceneCamera.transform.position, sceneCamera.transform.forward);
            for (uint i = 0; i < cells.cellsCatalog.Length; i++) {
                if (_cellWithPrefabIdFilter != -1 && _cellWithPrefabIdFilter != cells.cellsCatalog[i].prefabId) {
                    continue;
                }

                var color = finalCellsVisibility.IsSet((int)i) ? _cullVisibleColor : _cullInvisibleColor;
                Handles.color = color;

                var bounds = cells.cellsCatalog[i].bounds;
                var center = bounds.Center;
                var size = bounds.Size.x;
                var radius = cells.cellsRadii[i];

                if (_showBounds) {
                    Handles.DrawWireCube(center, bounds.Size);
                }

                if (_showSpheres) {
                    HandlesUtils.DrawSphere(center, radius);
                }

                var labelPos = center;
                labelPos.x -= size * 0.4f;

                _cellDataBuilder.Append(i);
                _cellDataBuilder.Append('.');

                if (_verbose) {
                    CreateVerboseInfo(finalCellsVisibility, i, frustumCellsFullVisibility, frustumCellsPartialVisibility,
                        distanceCellsVisibility, cellsDistances, cells, takenRanges.AsUnsafeSpan());
                }

                if (HandlesUtils.Label(labelPos, _cellDataBuilder.ToString(), color, _cellLabelStyle,
                        out var rect, cameraData)) {
                    _cellDataBuilder.Clear();
                    Handles.BeginGUI();
                    var buttonRect = new Rect(rect.x, rect.y + rect.size.y, 62, 16);
                    if (GUI.Button(buttonRect, "Investigate")) {
                        _cellInstancesDebug = (int)i;
                    }

                    Handles.EndGUI();
                }
            }
        }

        void DrawCameraBasedHandles(int prefabId, LeshyManager leshyManager) {
            var camera = Camera.main;
            if (!camera) {
                return;
            }

            var prefabs = leshyManager.EDITOR_Prefabs;
            var center = camera.transform.position;
            var prefab = prefabs.Prefabs[prefabId];

            Handles.color = Color.magenta;
            var lodDistances = prefabs.Prefabs[prefabId].lodDistances;
            for (int k = 0; k < 8; k++) {
                var value = lodDistances.Get(k);
                if (float.IsFinite(value)) {
                    HandlesUtils.DrawSphere(center, value);
                }
            }

            Handles.color = Color.red;
            var radius = leshyManager.EDITOR_Quality.SpawnDistance(prefab.prefabType);
            HandlesUtils.DrawSphere(center, radius);

            if (prefab.HasBillboard) {
                Handles.color = Color.yellow;
                radius = leshyManager.EDITOR_Quality.BillboardDistance(prefab.prefabType);
                HandlesUtils.DrawSphere(center, radius);
            }
        }

        void DebugCellInstances(LeshyManager leshyManager) {
            if (!leshyManager.EDITOR_SpawnedCells[(uint)_cellInstancesDebug]) {
                return;
            }

            var transforms = leshyManager.EDITOR_Loading.filteredData[(uint)_cellInstancesDebug];
            var radius = leshyManager.EDITOR_Cells.prefabsRadii[(uint)_cellInstancesDebug];
            var instancesHandle = leshyManager.EDITOR_Cells.cellsInstances[(uint)_cellInstancesDebug];

            var cell = leshyManager.EDITOR_Cells.cellsCatalog[(uint)_cellInstancesDebug];
            var prefabs = leshyManager.EDITOR_Prefabs.RuntimePrefabs(leshyManager.EDITOR_Quality);
            var prefab = prefabs[cell.prefabId];
            var lods = prefab.lodDistances;
            var lastLod = 0;
            for (var i = 0; i < 8; i++) {
                if (float.IsFinite(lods.Get(i))) {
                    lastLod = i;
                }
            }

            ++lastLod;

            DrawCameraBasedHandles(cell.prefabId, leshyManager);

            var center = prefab.localBounds.Center;

            var camera = SceneView.currentDrawingSceneView.camera;
            var cameraData = new float3x2(camera.transform.position, camera.transform.forward);

            Handles.color = Color.magenta;
            for (uint i = 0; i < transforms.Length; i++) {
                var matrix = transforms[i];
                var position = matrix.position;
                var scale = math.cmax(matrix.scale);
                var rotation = matrix.rotation;
                position += math.mul(rotation, center * scale);
                HandlesUtils.DrawSphere(position, radius * scale);

                var lod = instancesHandle.instancesSelectedLod[i];

                _cellDataBuilder.Append("Lod: ");
                for (int j = 0; j < lastLod; j++) {
                    if ((lod & (1 << j)) != 0) {
                        _cellDataBuilder.Append(j);
                        _cellDataBuilder.Append(' ');
                    }
                }

                _cellDataBuilder.AppendLine();
                _cellDataBuilder.Append("Frustum: ");
                _cellDataBuilder.Append(instancesHandle.instanceVisibilities.IsSet((int)i) ? "Visible" : "Invisible");

                HandlesUtils.Label(position, _cellDataBuilder.ToString(), Color.magenta, _cellLabelStyle,
                    out _, cameraData);
                _cellDataBuilder.Clear();
            }

            var fullTransforms = leshyManager.EDITOR_Loading.loadedData[(uint)_cellInstancesDebug];

            Handles.color = Color.red;
            for (uint i = transforms.Length; i < fullTransforms.Length; i++) {
                var matrix = fullTransforms[i];
                var position = matrix.position;
                var scale = math.cmax(matrix.scale);
                var rotation = matrix.rotation;
                position += math.mul(rotation, center * scale);
                HandlesUtils.DrawSphere(position, radius * scale);

                HandlesUtils.Label(position, "Filtered out", Color.red, _cellLabelStyle, out _, cameraData);
            }
        }

        void CreateVerboseInfo(
            NativeBitArray finalCellsVisibility, uint i, NativeBitArray frustumCellsFullVisibility,
            NativeBitArray frustumCellsPartialVisibility, NativeBitArray distanceCellsVisibility,
            UnsafeArray<float> cellsDistances,
            LeshyCells cells, UnsafeArray<LeshyRendering.InstancesRange>.Span takenRanges) {
            _cellDataBuilder.AppendLine();
            _cellDataBuilder.Append("Visibility: ");
            _cellDataBuilder.Append(finalCellsVisibility.IsSet((int)i) ? "Visible" : "Invisible");
            _cellDataBuilder.AppendLine();
            _cellDataBuilder.Append("Frustum: ");
            if (frustumCellsFullVisibility.IsSet((int)i)) {
                _cellDataBuilder.Append("Fully");
            } else if (frustumCellsPartialVisibility.IsSet((int)i)) {
                _cellDataBuilder.Append("Partly");
            } else {
                _cellDataBuilder.Append("Invisible");
            }

            _cellDataBuilder.AppendLine();
            _cellDataBuilder.Append("Distance: ");
            _cellDataBuilder.Append(distanceCellsVisibility.IsSet((int)i) ? "Visible" : "Invisible");
            _cellDataBuilder.Append(" - ");
            _cellDataBuilder.AppendFormat("{0:f2}", cellsDistances[i]);
            _cellDataBuilder.Append('/');
            _cellDataBuilder.AppendFormat("{0:f2}", cells.spawnDistances[i]);

            _cellDataBuilder.AppendLine();
            _cellDataBuilder.Append("Calc LODs: ");
            var possibleLods = cells.possibleLods[i];
            for (int j = 0; j < UnsafeUtility.SizeOf<byte>() * 8; j++) {
                if ((possibleLods & (1 << j)) != 0) {
                    _cellDataBuilder.Append(j);
                    _cellDataBuilder.Append(' ');
                }
            }

            var instances = cells.cellsInstances[i];

            _cellDataBuilder.AppendLine();
            _cellDataBuilder.Append("Instances: ");
            if (!instances.rangeIds.IsCreated) {
                _cellDataBuilder.Append("---");
            } else {
                var ranges = instances.rangeIds;
                uint instancesCount = 0;
                for (uint j = 0; j < ranges.Length; j++) {
                    var rangeId = ranges[j];
                    var range = takenRanges[rangeId];
                    instancesCount += range.count;
                }

                _cellDataBuilder.Append(instancesCount);
            }

            _cellDataBuilder.AppendLine();
            _cellDataBuilder.Append("Ranges: ");
            if (!instances.rangeIds.IsCreated) {
                _cellDataBuilder.Append("not allocated");
            } else {
                var ranges = instances.rangeIds;
                for (uint j = 0; j < ranges.Length; j++) {
                    var rangeId = ranges[j];
                    var range = takenRanges[rangeId];
                    var from = range.gpuStartIndex;
                    var to = from + range.count;
                    _cellDataBuilder.Append('<');
                    _cellDataBuilder.Append(from);
                    _cellDataBuilder.Append("..");
                    _cellDataBuilder.Append(to);
                    _cellDataBuilder.Append('>');
                    _cellDataBuilder.Append(' ');
                }
            }
        }
    }
}