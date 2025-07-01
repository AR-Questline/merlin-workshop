using System;
using System.IO;
using System.Text.RegularExpressions;
using Awaken.TG.Editor.Utility.Paths;
using Awaken.Utility.Debugging;
using Awaken.Utility.GameObjects;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Editor.Assets {
    public static class PrefabCreation {
        // === Consts
        static readonly Regex MeshNameRegex = new Regex(@"(?<=mesh_).+", RegexOptions.IgnoreCase);

        static void SaveAsPrefab(GameObject sceneInstance, Type[] components, string preferredPath = null, bool forceFocus = true) {
            // create prefab and focus on it
            
            if (components != null) {
                foreach (Type type in components) {
                    sceneInstance.AddComponent(type);
                }
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(sceneInstance,
                AssetPaths.GetPathForAsset(sceneInstance.name + ".prefab", preferredPath));
            AssetDatabase.SaveAssets();
            if (forceFocus) {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = prefab;
            }

            // destroy the template we used to save
            UnityEngine.Object.DestroyImmediate(sceneInstance);
        }

        public static bool IsModel(string path) {
            return path.EndsWith(".fbx", StringComparison.InvariantCultureIgnoreCase)
                   || path.EndsWith(".obj", StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsNotAnimationModel(string p) {
            return !(p.Contains("@") || p.IndexOf("anim", StringComparison.InvariantCultureIgnoreCase) >= 0);
        }

        public static void CreatePrefabFromMesh(PathSourcePair<GameObject> pathMesh, Type[] components = null, string suffix = null) {
            string originalPath = pathMesh.path;
            GameObject originalMesh = pathMesh.source;

            var pathDirectory = PathUtils.ParentDirectory(Path.GetDirectoryName(originalPath));
            var nameMatch = MeshNameRegex.Match(Path.GetFileNameWithoutExtension(originalPath));
            var prefabName = nameMatch.Success ? nameMatch.Value : Path.GetFileNameWithoutExtension(originalPath);
            
            if (!string.IsNullOrWhiteSpace(suffix)) {
                prefabName = $"{prefabName}_{suffix}";
            }

            var fileName = prefabName;
            fileName += ".prefab";

            pathDirectory = Path.Combine(pathDirectory, "Prefabs");
            var prefabPath = Path.Combine(pathDirectory, fileName);
            
            // if there is already prefab without prefix Prefab_, don't create new one
            if (!File.Exists(prefabPath)) {
                prefabName = $"Prefab_{prefabName}";
                prefabPath = Path.Combine(pathDirectory, fileName);
            }

            if (!Directory.Exists(pathDirectory)) {
                Directory.CreateDirectory(pathDirectory);
            }

            if (File.Exists(prefabPath)) {
                Log.Important?.Info($"Omitted mesh {fileName}");
                return;
            }

            GameObject prefabObject = new GameObject(prefabName);

            InstantiateModelForEditing(originalMesh, prefabObject.transform);
            SaveAsPrefab(prefabObject, components, PathUtils.FilesystemToAssetPath(pathDirectory), false);
            GameObjects.DestroySafely(prefabObject);
            Log.Important?.Info($"Created prefab {fileName} at {prefabPath}");
        }

        static void InstantiateModelForEditing(GameObject model, Transform parent = null) {
            GameObject instance = (GameObject) PrefabUtility.InstantiatePrefab(model, parent);
            instance.name = model.name;
        }
    }
}