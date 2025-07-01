using System;
using System.Linq;
using Sirenix.OdinInspector;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.VFX;

namespace Awaken.TG.Editor.Assets.Grouping {
    [Serializable]
    public class AssetEntry {
        // === Static
        static Type[] s_extractedTypes = {
            typeof(GameObject), 
            typeof(Texture), 
            typeof(Texture2D), 
            typeof(Texture2DArray), 
            typeof(Texture3D), 
            typeof(Cubemap), 
            typeof(Material), 
            typeof(Mesh), 
            typeof(Shader), 
            typeof(VisualEffectAsset), 
            typeof(FontAsset), 
            typeof(FontAsset)
        };

        bool IsExtractedType(string path) => s_extractedTypes.Contains(AssetDatabase.GetMainAssetTypeAtPath(path));
        // === End Static

        public string guid;
        /// <summary>
        /// asset dependency GUIDs
        /// </summary>
        public string[] dependencies;
        /// <summary>
        /// asset usage GUIDs
        /// </summary>
        public string[] usages;
        [SerializeField]
        public AssetGroup assetGroup;

        [ShowInInspector]
        public string TypeName => Entry?.MainAsset?.GetType().Name;
        [ShowInInspector]
        public AddressableAssetEntry Entry => assetGroup?.Manager.Settings.FindAssetEntry(guid);

        public AssetEntry(string guid, string assetPath) {
            this.guid = guid;
            dependencies = AssetDatabase.GetDependencies(assetPath, true)
                .Where(IsExtractedType)
                .Select(AssetDatabase.AssetPathToGUID)
                .ToArray();
        }
    }
}