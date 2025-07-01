using System;
using Awaken.TG.Main.Utility.Animations.ARAnimator;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility {
    public static class ScriptableObjectUtility {
        /// <summary>
        ///	This makes it easy to create, name and place unique new ScriptableObject asset files.
        /// </summary>
        public static T CreateAsset<T>(string name = null) where T : ScriptableObject {
            return (T) CreateAsset(typeof(T), name);
        }

        public static ScriptableObject CreateAsset(Type type, string name = null) {
            ScriptableObject asset = ScriptableObject.CreateInstance(type);

            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path == "") {
                path = "Assets";
            } else if (System.IO.Path.GetExtension(path) != "") {
                path = path.Replace(System.IO.Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
            }

            if (string.IsNullOrWhiteSpace(name)) {
                name = "New " + type;
            }
        
            string assetPathAndName =
                AssetDatabase.GenerateUniqueAssetPath(path + "/" + name + ".asset");

            AssetDatabase.CreateAsset(asset, assetPathAndName);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            return asset;
        }
    }
}