using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Awaken.TG.Main.Grounds;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Locations;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Utility.Assets {
    /// <summary>
    /// Static class used for validating Unity assets.
    /// </summary>
    public static class AssetsUtils {
        [MenuItem("TG/Find Object with GUID in clipboard")]
        static void FindObjectWithGUID() {
            string guid = GUIUtility.systemCopyBuffer.Trim();
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
            Selection.activeObject = obj;
        }

        [MenuItem("TG/Find Object with InstanceID in clipboard")]
        static void FindObjectWithInstanceID() {
            string instanceId = GUIUtility.systemCopyBuffer.Trim();
            if (!int.TryParse(instanceId, out int instanceIdInt)) {
                Log.Important?.Error("Invalid InstanceID");
                return;
            }
            var obj = EditorUtility.InstanceIDToObject(instanceIdInt);
            if (obj != null) {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }
        }
        
        [MenuItem("Assets/TG/Copy GUID")]
        static void CopyGUIDToClipboard() {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(Selection.activeObject, out string guid, out long _);
            GUIUtility.systemCopyBuffer = guid;
        }
        
        [MenuItem("Assets/TG/Copy GUID", true)]
        static bool CopyGUIDValidation() {
            Object activeObject = Selection.activeObject;
            if (activeObject != null) {
                string path = AssetDatabase.GetAssetPath(activeObject);
                if (string.IsNullOrWhiteSpace(path)) {
                    return false;
                }
                // check if given Object is a real asset
                if (!File.GetAttributes(path).HasFlag(FileAttributes.Directory)) {
                    return true;
                }
            }

            return false;
        }

        [MenuItem("Assets/TG/Print Instance IDs")]
        static void PrintInstanceIds() {
            foreach (var o in Selection.objects) {
                if (o is GameObject go) {
                    ProcessGameObject(go);
                } else if (o is Component component) {
                    ProcessComponent(component);
                } else if (o is ScriptableObject so) {
                    ProcessScriptableObject(so);
                }
            }

            void ProcessComponent(Component component) {
                Log.Important?.Info($"[{component.GetType().Name}] {component.name} InstanceID: {component.GetInstanceID()}");
            }

            void ProcessGameObject(GameObject go) {
                Log.Important?.Info($"[GameObject] {go.name} InstanceID: {go.GetInstanceID()}");
                foreach (var component in go.GetComponents<Component>()) {
                    ProcessComponent(component);
                }
            }

            void ProcessScriptableObject(ScriptableObject so) {
                Log.Important?.Info($"[ScriptableObject] {so.name} InstanceID: {so.GetInstanceID()}");
            }
        }

        [MenuItem("Assets/TG/Print Instance IDs", true)]
        static bool PrintInstanceIdsValidation() {
            return Selection.objects.IsNotNullOrEmpty();
        }

        [MenuItem("TG/Assets/Deselect Randomly", false, 1)]
        public static void RandomDeselect() {
            List<GameObject> tempList = new();
            foreach (GameObject o in Selection.gameObjects)
                if (UnityEngine.Random.Range(0, 99) < 50)
                    tempList.Add(item: o);
            Selection.objects = tempList.ToArray();
        }
        
        [MenuItem("TG/Assets/Reserialize Selected")]
        public static void ReserializeSelected() {
            AssetDatabase.StartAssetEditing();
            List<GameObject> objects = Selection.objects.OfType<GameObject>().ToList();
            List<string> paths = objects.Select(AssetDatabase.GetAssetPath).ToList();
            AssetDatabase.ForceReserializeAssets(paths);
            AssetDatabase.StopAssetEditing();
        }

        public static void ForEachScriptableObject<T>(Action<T> action, string[] folders) where T : ScriptableObject {
            var assetGuids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", folders);
            AssetDatabase.StartAssetEditing();
            foreach (var guid in assetGuids) {
                T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                try {
                    action(asset);
                    EditorUtility.SetDirty(asset);
                } catch (Exception e) {
                    Debug.LogException(e, asset);
                }
            }
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Use with caution, it's not prepared to properly handle nested prefabs madness! 
        /// </summary>
        public static void ForEachComponent<T>(Action<T> action, string[] folders, bool save = true) where T : Component {
            var assetGuids = AssetDatabase.FindAssets("t:Prefab", folders);
            if (save) {
                AssetDatabase.StartAssetEditing();
            }

            foreach (var guid in assetGuids) {
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (go == null) continue;
                foreach (var component in go.GetComponentsInChildren<T>(true)) {
                    try {
                        action(component);
                        if (save) {
                            EditorUtility.SetDirty(component);
                            EditorUtility.SetDirty(component.gameObject);
                            EditorUtility.SetDirty(go);
                        }
                    } catch (Exception e) {
                        Debug.LogException(e, go);
                    }
                }
            }

            if (save) {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
            }
        }

        //[MenuItem("Assets/TG/Remove Colliders from Medieval Package")]
        static void RemoveColliders() {
            var path = Application.dataPath + "/3DAssets/Props/MedievalVillage";
            var prefabsPaths = Directory.GetFiles(path, "*.prefab", SearchOption.AllDirectories);
            foreach (var prefabPath in prefabsPaths) {
                string assetPath = "Assets" + prefabPath.Replace(Application.dataPath, "").Replace('\\', '/');
                var prefab = (GameObject)AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject));
                bool colliderFound = false;
                foreach (var collider in prefab.GetComponentsInChildren<Collider>()) {
                    colliderFound = true;
                    try {
                        if (collider.gameObject.GetComponents<Component>().All(c => c is MeshFilter || c is Collider || c is Transform)) {
                            Object.DestroyImmediate(collider.gameObject, true);
                            Log.Important?.Info("Destroyed GameObject with collider");
                        }
                        else {
                            Object.DestroyImmediate(collider, true);
                            Log.Important?.Info("Destroyed Collider Only");
                        }
                    }
                    catch (Exception e) {
                        Log.Important?.Error("Failed to destroy collider in: " + assetPath + "\n" + e);
                    }
                }
                if (colliderFound) {
                    EditorUtility.SetDirty(prefab);   
                }
            }
            AssetDatabase.SaveAssets();
        }

        //[MenuItem("Assets/TG/Convert NavMeshObstacles to GridGraphObstacles")]
        static void ConvertNavMeshObstacles() {
            var locationPrefabsGUIDs = AssetDatabase.FindAssets("t:Prefab");
            List<GameObject> locationPrefabs = new List<GameObject>();
            foreach (var guid in locationPrefabsGUIDs) {
                var gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (gameObject.GetComponentInChildren<LocationPrefab>() != null) {
                    locationPrefabs.Add(gameObject);
                }
            }

            foreach (var location in locationPrefabs) {
                bool obstacleFound = false;
                var navMeshObstacles = location.GetComponentsInChildren<NavMeshObstacle>();
                foreach (var obstacle in navMeshObstacles) {
                    obstacleFound = true;
                    switch (obstacle.shape) {
                        case NavMeshObstacleShape.Box:
                            var boxCollider = obstacle.gameObject.AddComponent<BoxCollider>();
                            boxCollider.center = obstacle.center;
                            boxCollider.size = obstacle.size;
                            var ggo = obstacle.gameObject.AddComponent<GridGraphObstacle>();
                            ggo.colliderType = ArColliderType.Box;
                            break;
                        case NavMeshObstacleShape.Capsule:
                            var capsuleCollider = obstacle.gameObject.AddComponent<CapsuleCollider>();
                            capsuleCollider.center = obstacle.center;
                            capsuleCollider.height = obstacle.height*2;
                            capsuleCollider.radius = obstacle.radius;
                            var gridGraphObstacle = obstacle.gameObject.AddComponent<GridGraphObstacle>();
                            gridGraphObstacle.colliderType = ArColliderType.Capsule;
                            break;
                    }
                    obstacle.gameObject.layer = LayerMask.NameToLayer("NavMeshObstacle");
                    Object.DestroyImmediate(obstacle, true);
                }
                if (obstacleFound) {
                    EditorUtility.SetDirty(location);
                }
            }
            AssetDatabase.SaveAssets();
        }
        
        // --- Debug method when we want to copy/change some stat for all items at once.
        static void CopyItemStatsData() {
            AssetDatabase.StartAssetEditing();
            try {
                var loader = TemplatesLoader.CreateAndLoad();
                // --- Example Implementation
                foreach (var iTemplate in loader.typeMap[typeof(ItemTemplate)]) {
                    if (iTemplate is ItemTemplate itemTemplate &&
                        itemTemplate.TryGetComponent(out ItemStatsAttachment itemStats)) {
                        // --- Do Something Here
                        EditorUtility.SetDirty(itemTemplate);
                        EditorUtility.SetDirty(itemTemplate.gameObject);
                    }
                }
            } finally {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
            }
        }

        static readonly Regex ResourcePathRe = new Regex("^.*Resources/(.*).prefab$", RegexOptions.Compiled);
        public static string FindResourcePath(Object obj) {
            string rawPath = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(rawPath)) return null;
            if (!rawPath.Contains("Resources/")) return null;
            return ResourcePathRe.Match(rawPath).Groups[1].Value;
        }

        public static string ObjectToGuid(Object obj) {
            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
        }

        public static T LoadAssetByGuid<T>(string guid) where T : Object {
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
        }
        
        public class AssetEditingScope : IDisposable {
            readonly bool _save;
            public AssetEditingScope(bool save) {
                _save = save;
                AssetDatabase.StartAssetEditing();
            }
            
            public void Dispose() {
                AssetDatabase.StopAssetEditing();
                if (_save) {
                    AssetDatabase.SaveAssets();
                }
            }
        }
    }
}