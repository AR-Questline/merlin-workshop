using System;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.Grouping.Modifiers {
    [Serializable]
    public class AssetGroupPrefabsExcludeModifier : IAssetGroupModifier  {
        public void Modify(ARAddressableManager manager) {
            AssetDatabase.StartAssetEditing();
            try {
                foreach (AssetGroup assetGroup in manager.groups.ToArray()) {
                    if (assetGroup.type == AssetGroup.AssetGroupType.Prefabs) {
                        manager.RemoveGroup(assetGroup, true);
                    }
                }
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                AssetDatabase.StopAssetEditing();
            }
        }
    }
}