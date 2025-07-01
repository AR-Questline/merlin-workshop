using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Editor.Assets;
using Awaken.TG.Main.Scenes.SceneConstructors.SubdividedScenes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Awaken.TG.Editor.Main.Scenes.SubdividedScenes {
    public static class SubsceneEditorDataManager {
        static readonly Dictionary<Scene, SubsceneEditorData> Cache = new(new SceneEqualityComparer());

        [InitializeOnLoadMethod]
        static void Init() {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneSaved += OnSceneSaved;
            EditorSceneManager.sceneClosed += OnSceneClosed;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        
        static void OnPlayModeStateChanged(PlayModeStateChange obj) {
            if (obj == PlayModeStateChange.EnteredEditMode) {
                Cache.Clear();
                for (int i = 0; i < SceneManager.sceneCount; i++) {
                    OnSceneOpened(SceneManager.GetSceneAt(i), OpenSceneMode.Single);
                }
            }
        }
        
        static void OnSceneOpened(Scene scene, OpenSceneMode mode) {
            var data = FindOrCreateFor(scene);
            Cache[scene] = data;
            data?.UpdateFor(scene);
        }
        
        static void OnSceneSaved(Scene scene) {
            if (!Cache.TryGetValue(scene, out var data) || data == null) {
                Cache[scene] = data = FindOrCreateFor(scene);
            }
            data?.UpdateFor(scene);
        }
        
        static void OnSceneClosed(Scene scene) {
            Cache.Remove(scene);
        }

        static SubsceneEditorData FindOrCreateFor(Scene scene) {
            bool isChildScene = scene.GetRootGameObjects().Any(root => {
                return root.activeSelf && root.TryGetComponent(out SubdividedSceneChild _);
            });
            if (!isChildScene) {
                return null;
            }
            
            GetDataPaths(scene.path, out var directoryPath, out var filePath);
            
            if (!AssetDatabase.IsValidFolder(directoryPath)) {
                int separatorIndex = directoryPath.LastIndexOf('/');
                var parentDirectoryPath = directoryPath[..separatorIndex];
                var directoryName = directoryPath[(separatorIndex + 1)..];
                AssetDatabase.CreateFolder(parentDirectoryPath, directoryName);
            }
            
            var data = AssetDatabase.LoadAssetAtPath<SubsceneEditorData>(filePath);
            if (data == null) {
                data = ScriptableObject.CreateInstance<SubsceneEditorData>();
                AssetDatabase.CreateAsset(data, filePath);
                AssetDatabase.SaveAssets();
            }

            return data;
        }
        
        public static bool TryFindFor(Scene scene, out SubsceneEditorData data) {
            if (scene.IsValid()) {
                return TryFindFor(scene.path, out data);
            } else {
                data = null;
                return false;
            }
        }
        public static bool TryFindFor(SceneReference sceneRef, out SubsceneEditorData data) {
            if (sceneRef.TryGetSceneAssetGUID(out var guid)) {
                return TryFindFor(AssetDatabase.GUIDToAssetPath(guid), out data);
            } else {
                data = null;
                return false;
            }
        }
        public static bool TryFindFor(SceneAsset sceneAsset, out SubsceneEditorData data) {
            if (sceneAsset != null) {
                return TryFindFor(AssetDatabase.GetAssetPath(sceneAsset), out data);
            } else {
                data = null;
                return false;
            }
        }
        static bool TryFindFor(string scenePath, out SubsceneEditorData data) {
            GetDataPaths(scenePath, out _, out var filePath);
            data = AssetDatabase.LoadAssetAtPath<SubsceneEditorData>(filePath);
            return data != null;
        }

        static void GetDataPaths(string scenePath, out string directoryPath, out string filePath) {
            directoryPath = scenePath.EndsWith(".unity") ? scenePath[..^6] : scenePath;
            filePath = $"{directoryPath}/SubsceneData.asset";
        }
    }
}