using System;
using UnityEditor;
using UnityEngine;

namespace Awaken.Kandra.Editor {
    public class KandraMaterialPostprocessor : AssetPostprocessor {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
            foreach (var asset in importedAssets) {
                if (!asset.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                var material = (Material)AssetDatabase.LoadAssetAtPath(asset, typeof(Material));
                if (material == null) {
                    continue;
                }

                KandraRendererManager.Instance.MaterialBroker.Editor_OnMaterialChanged(material);
            }
        }
    }
}