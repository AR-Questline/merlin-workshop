using System;
using Awaken.CommonInterfaces;
using Awaken.TG.Editor.Helpers;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Editor.Assets {
    public static class FixMissingScripts {
        [MenuItem("TG/Assets/Fix missing scripts from scenes")]
        static void FixMissing() {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null) {
                FixPrefabStage(prefabStage);
            } else {
                FixScene();
            }
        }

        [MenuItem("TG/Assets/Fix missing scripts from prefabs")]
        public static void BakeAll() {
            var prefabGUIDs = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/3DAssets" });
            var count = prefabGUIDs.Length;

            AssetDatabase.StartAssetEditing();
            try {
                for (int i = 0; i < count; i++) {
                    string prefabGUID = prefabGUIDs[i];
                    string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGUID);
                    try {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                        if (EditorUtility.DisplayCancelableProgressBar("Baking drake", $"Baking {prefab}", i / (float)count)) {
                            break;
                        }
                        if (HasMissing(prefab)) {
                            FixPrefab(prefabPath);
                        }
                    } catch (Exception e) {
                        Log.Minor?.Error($"For prefab with guid: {prefabGUID} can not remove missings");
                        Debug.LogException(e);
                    }
                }
            } finally {
                EditorUtility.ClearProgressBar();
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.SaveAssets();
        }

        static void FixPrefabStage(PrefabStage prefabStage) {
            var root = prefabStage.prefabContentsRoot;
            FixGameObject(root);
        }

        static void FixScene() {
            var scenesCount = SceneManager.sceneCount;
            ISubscenesOwner subscenesOwner = null;
            for (int i = 0; i < scenesCount; i++) {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded) {
                    subscenesOwner ??= GameObjects.FindComponentByTypeInScene<ISubscenesOwner>(scene, false);
                    FixScene(scene);
                }
            }
            EditorSceneManager.SaveOpenScenes();
            if (subscenesOwner == null) {
                return;
            }
            PerSubscene.Action(FixScene);
        }

        static void FixScene(Scene scene) {
            var gameObjects = scene.GetRootGameObjects();
            foreach (var gameObject in gameObjects) {
                FixGameObject(gameObject);
            }
        }

        static void FixGameObject(GameObject gameObject, Func<GameObject, bool> canEdit = null) {
            canEdit ??= static _ => true;
            if (GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject) > 0) {
                Debug.Log($"Removed missing scripts from {gameObject.name}");
                EditorUtility.SetDirty(gameObject);
            }
            var transform = gameObject.transform;
            var childCount = gameObject.transform.childCount;
            for (int i = 0; i < childCount; i++) {
                var child = transform.GetChild(i).gameObject;
                if (canEdit(child)) {
                    FixGameObject(child);
                }
            }
        }

        static bool HasMissing(GameObject gameObject) {
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject) > 0) {
                return true;
            }
            var transform = gameObject.transform;
            var childCount = gameObject.transform.childCount;
            for (int i = 0; i < childCount; i++) {
                if (HasMissing(transform.GetChild(i).gameObject)) {
                    return true;
                }
            }
            return false;
        }

        static void FixPrefab(string prefabPath) {
            using var editPrefab = new PrefabUtility.EditPrefabContentsScope(prefabPath);
            Debug.Log($"Fixing prefab {prefabPath}");
            FixGameObject(editPrefab.prefabContentsRoot.gameObject, CanEdit);

            static bool CanEdit(GameObject go) {
                return PrefabUtility.GetNearestPrefabInstanceRoot(go) == PrefabUtility.GetOutermostPrefabInstanceRoot(go);
            }
        }
    }
}
