using System.Linq;
using Awaken.TG.Assets;
using Awaken.TG.Editor.Utility.Paths;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Editor.Utility.Assets {
    public static class UnloadViews {
        [MenuItem("TG/Assets/UnloadViews", false, 5000)]
        public static void UnloadAllView() {
            var resourcesFullPath = PathUtils.AssetToFileSystemPath("Assets/Resources/Prefabs");
            var files = PathUtils.GetFiles(resourcesFullPath).Where(p => p.EndsWith("prefab"));
            foreach (var file in files) {
                var assetsPath = PathUtils.FilesystemToAssetPath(file);
                var resourcesPath = assetsPath.Substring(17, assetsPath.Length - 17);
                resourcesPath = System.IO.Path.ChangeExtension(resourcesPath, null);
                var resourcesPrefab = Resources.Load<GameObject>(resourcesPath);
                var lazyImages = resourcesPrefab.GetComponentsInChildren<LazyImage>(true);
                bool anyUnloaded = false;
                foreach (LazyImage lazyImage in lazyImages) {
                    if (!(lazyImage.arSpriteReference?.IsSet ?? false)) {
                        Log.Important?.Error($"There is invalid lazy image on {resourcesPath}/{lazyImage.gameObject.name}");
                    }
                    anyUnloaded = anyUnloaded || lazyImage.image.sprite != null;
                }

                if (anyUnloaded) {
                    using PrefabUtility.EditPrefabContentsScope prefabAssetScope = new (assetsPath);
                    var prefab = prefabAssetScope.prefabContentsRoot;
                    var prefabLazyImages = prefab.GetComponentsInChildren<LazyImage>(true);
                    foreach (LazyImage lazyImage in prefabLazyImages) {
                        if ((lazyImage.arSpriteReference?.IsSet ?? false) && lazyImage.image.sprite != null) {
                            lazyImage.image.sprite = null;
                        }
                    }
                }
            }
        }
    }
}