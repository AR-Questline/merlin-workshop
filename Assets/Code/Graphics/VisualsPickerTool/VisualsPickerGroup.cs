using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

namespace Awaken.TG.Graphics.VisualsPickerTool {
    [Serializable, InlineProperty]
    public class VisualsPickerGroup {
        [SerializeField, HideInInspector] string ownerKit;

        [SerializeField, HideIn(PrefabKind.PrefabAsset | PrefabKind.PrefabInstance), ARAssetReferenceSettings(new[] {typeof(GameObject)}, true)]
        List<ARAssetReference> assets = new();

        public string OwnerKit => ownerKit;
        public bool HasAssets => assets is { Count: > 0 };
        public int Count => assets.Count;

        public IEnumerable<ARAssetReference> Assets => assets;

        public VisualsPickerGroup(string ownerKit) {
            this.ownerKit = ownerKit;
        }

        public ARAssetReference GetAssetReference(int index) {
            return assets[index];
        }

        public void AddAsset(ARAssetReference assetReference) {
            assets.Add(assetReference);
        }

        public void RemoveAsset(ARAssetReference assetReference) {
            assets.Remove(assetReference);
        }
    }
}