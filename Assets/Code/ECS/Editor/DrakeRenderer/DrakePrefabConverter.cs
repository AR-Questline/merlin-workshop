using Awaken.ECS.DrakeRenderer.Authoring;
using Awaken.ECS.MedusaRenderer;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.ECS.Editor.DrakeRenderer {
    public static class DrakePrefabConverter {
        [MenuItem("TG/Assets/Drake/Convert to Drake Prefab")]
        static void ConvertSelected() {
            var selectedGameObjects = Selection.gameObjects;
            if (selectedGameObjects.Length > 0) {
                AssetDatabase.StartAssetEditing();
                try {
                    foreach (var selectedGameObject in selectedGameObjects) {
                        ConvertPrefabToDrake(selectedGameObject);
                    }
                } finally {
                    AssetDatabase.StopAssetEditing();
                }
                AssetDatabase.Refresh();
            } else if (Selection.activeObject is DefaultAsset folder) {
                var folderPath = AssetDatabase.GetAssetPath(folder);
                ConvertAll(new[] { folderPath });
            }
        }

        [MenuItem("TG/Assets/Drake/Convert all to Drake Prefab")]
        static void ConvertAll() {
            ConvertAll(null);
        }

        static void ConvertAll(string[] folders) {
            var withAloneLods = EditorUtility.DisplayDialog("Drake convert", "Should convert alone lod group?", "Yes", "No");
            var prefabGuids = AssetDatabase.FindAssets("t:prefab", folders);
            var count = prefabGuids.Length;
            AssetDatabase.StartAssetEditing();
            try {
                for (int i = 0; i < count; i++) {
                    string prefabGuid = prefabGuids[i];
                    try {
                        string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                        if (EditorUtility.DisplayCancelableProgressBar("Converting to drake", $"Converting {prefab}",
                                i / (float)count)) {
                            break;
                        }
                        if (withAloneLods && prefab.GetComponentsInChildren<LODGroup>().Length > 0) {
                            ConvertPrefabToDrake(prefab);
                        }
                    } catch (System.Exception e) {
                        Log.Minor?.Error($"For prefab with guid: {prefabGuid} can not bake drake");
                        Debug.LogException(e);
                    }
                }
            } finally {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.Refresh();
        }

        static void ConvertPrefabToDrake(GameObject selectedGameObject) {
            if (!selectedGameObject) {
                return;
            }
            if (!PrefabUtility.IsPartOfPrefabAsset(selectedGameObject)) {
                return;
            }
            if (selectedGameObject.GetComponentInChildren<MedusaRendererPrefab>()) {
                return;
            }

            using var prefabEditing =
                new PrefabUtility.EditPrefabContentsScope(AssetDatabase.GetAssetPath(selectedGameObject));
            var editablePrefab = prefabEditing.prefabContentsRoot;
            ConvertEditableOPrefab(editablePrefab);
        }

        public static void ConvertEditableOPrefab(GameObject editablePrefab) {
            var lodGroups = editablePrefab.GetComponentsInChildren<LODGroup>();
            foreach (var lodGroup in lodGroups) {
                if (IsValidTarget(lodGroup)) {
                    var drakeLodGroup = lodGroup.gameObject.AddComponent<DrakeLodGroup>();
                    DrakeEditorHelpers.Bake(drakeLodGroup, lodGroup);
                }
            }

            var meshRenderers = editablePrefab.GetComponentsInChildren<MeshRenderer>();
            foreach (var meshRenderer in meshRenderers) {
                if (IsValidTarget(meshRenderer)) {
                    var drakeMeshRenderer = meshRenderer.gameObject.AddComponent<DrakeMeshRenderer>();
                    if (!DrakeMeshRendererEditor.Bake(drakeMeshRenderer, meshRenderer)) {
                        Object.DestroyImmediate(drakeMeshRenderer);
                    }
                }
            }
        }

        static bool IsValidTarget(Object componentOrGameObject) {
            var outermost = PrefabUtility.GetOutermostPrefabInstanceRoot(componentOrGameObject);
            if (outermost == null) {
                return true;
            }
            if (PrefabUtility.IsPartOfModelPrefab(outermost)) {
                return true;
            }
            return false;
        }
    }
}
