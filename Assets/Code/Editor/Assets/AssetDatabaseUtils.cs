using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG
{
    public static class AssetDatabaseUtils 
    {
        public static T[] GetAllAssetsOfType<T>(params string[] foldersToSearch) where T : UnityEngine.Object {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).FullName}", foldersToSearch);
            T[] assets = guids.Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();
            return assets;
        }
        
        public static T GetSingletonScriptableObject<T>(params string[] foldersToSearch) where T : UnityEngine.ScriptableObject {
            return GetSingletonScriptableObject(typeof(T).FullName, foldersToSearch) as T;
        }
        
        public static ScriptableObject GetSingletonScriptableObject(string typeName, params string[] foldersToSearch) {
            string[] guids = AssetDatabase.FindAssets($"t:{typeName}", foldersToSearch);
            if (guids.Length == 0) {
                Log.Important?.Error($"There is no {typeName} {nameof(ScriptableObject)}");
                return null;
            }
            if (guids.Length > 1) {
                Log.Important?.Error($"There are more than one {typeName} {nameof(ScriptableObject)}");
            }

            return AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        
        public static void GetAssetsPaths(string typeName, List<string> paths, params string[] foldersToSearch) {
            string[] guids = AssetDatabase.FindAssets($"t:{typeName}", foldersToSearch);
            for (int i = 0; i < guids.Length; i++) {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                paths.Add(path);
            }
        }
    }
}
