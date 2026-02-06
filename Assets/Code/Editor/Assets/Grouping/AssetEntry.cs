using System;
using System.Linq;
using Awaken.Kandra;
using Sirenix.OdinInspector;
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
            typeof(KandraMesh),
            typeof(Shader),
            typeof(VisualEffectAsset),
            typeof(FontAsset)
        };

        static bool IsExtractedType(string path) {
            var investigateType = AssetDatabase.GetMainAssetTypeAtPath(path);
            foreach (var type in s_extractedTypes) {
                if (type.IsAssignableFrom(investigateType)) {
                    return true;
                }
            }
            return false;
        }

        static bool IsRuntime(string path) {
            if (path.Contains("/Editor/")) {
                return false;
            }

            return true;
        }

        static bool IsValid(string path) => IsExtractedType(path) && IsRuntime(path);
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
            if (assetPath.Contains("/Editor/")) {
                throw new ArgumentException("AssetEntry cannot be created for editor only assets");
            }
            dependencies = AssetDatabase.GetDependencies(assetPath, true)
                .Where(IsValid)
                .Select(AssetDatabase.AssetPathToGUID)
                .ToArray();
        }
    }
}