using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Awaken.TG.Assets;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Utility.Assets {
    public static class AssetFieldsConverter {
        
        // Example usage
        // [MenuItem("TG/Assets/RepairAssetReferences")]
        // static void RepairAssetReference() {
        //     ForEachField<ARAssetReference>("Repairing AssetReference",
        //         (reference, message) => {
        //             if (reference.address != reference.assetReference?.AssetGUID) {
        //                 reference.address = reference.assetReference?.AssetGUID;
        //                 reference.subObjectName = reference.assetReference?.SubObjectName;
        //                 message.AppendLine($"{reference.assetReference?.AssetGUID} | {reference.address}");
        //                 return true;
        //             }
        //             return false;
        //         },
        //         new[] {
        //             ".asset",
        //             ".prefab",
        //         }
        //     );
        // }

        static void ForEachField<T>(string name, Func<T, StringBuilder, bool> action, string[] extensions = null, string[] folders = null) where T : class {
            var visited = new HashSet<object>();
            string[] guids = folders == null ? AssetDatabase.FindAssets(null) : AssetDatabase.FindAssets(null, folders);
            Log.Important?.Info($"Found {guids.Length} assets");
            for (int i = 0; i < guids.Length; i++) {
                var guid = guids[i];
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                EditorUtility.DisplayProgressBar(name, path, (float)i / guids.Length);

                if (extensions?.All(e => !path.EndsWith(e)) ?? false) continue;
                    
                try {
                    bool changed = false;
                    var message = new StringBuilder();
                    
                    if (obj is ScriptableObject so) {
                        changed = ForEachFieldIn(so, action, visited, message) || changed;
                    }

                    if (obj is GameObject go) {
                        foreach (var component in go.GetComponentsInChildren<Component>(true)) {
                            changed = ForEachFieldIn(component, action, visited, message) || changed;
                        }
                    }

                    if (obj is SceneAsset) {
                        using var scene = new SceneResources(path, false);
                        var sceneGameObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                        var sceneComponents = sceneGameObjects.SelectMany(sceneGO => sceneGO.GetComponents<Component>());
                        foreach (var sceneComponent in sceneComponents) {
                            changed = ForEachFieldIn(sceneComponent, action, visited, message) || changed;
                        }
                    }

                    if (changed) {
                        Log.Important?.Info($"Changed asset at {path}");
                        Log.Important?.Info(message.ToString());
                        EditorUtility.SetDirty(obj);
                    }
                } catch (Exception e) {
                    Log.Important?.Error($"Error when checking asset at path {path}", obj);
                    Debug.LogException(e, obj);
                }
            }
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
        }

        static bool ForEachFieldIn<T>(object obj, Func<T, StringBuilder, bool> action, HashSet<object> visited, StringBuilder message) where T : class {
            if (obj == null) return false;
            if (!visited.Add(obj)) return false;
            
            bool changed = false;
            var fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Concat(obj.GetType()
                    .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                    .Where(f => f.GetCustomAttribute<SerializeField>() != null || f.GetCustomAttribute<SerializeReference>() != null)
                );
            
            foreach (var field in fields) {
                if (field.FieldType == typeof(T)) {
                    if (field.GetValue(obj) is T t) {
                        if (action(t, message)) {
                            changed = true;
                        }
                    }
                } else if (typeof(ICollection).IsAssignableFrom(field.FieldType)) {
                    if (field.GetValue(obj) is ICollection collection) {
                        foreach (var element in collection) {
                            if (element is T t) {
                                if (action(t, message)) {
                                    changed = true;
                                }
                            } else {
                                changed = ForEachFieldIn(element, action, visited, message) || changed;
                            }
                        }
                    }
                } else {
                    changed = ForEachFieldIn(field.GetValue(obj), action, visited, message) || changed;
                }
            }
            return changed;
        }
    }
}