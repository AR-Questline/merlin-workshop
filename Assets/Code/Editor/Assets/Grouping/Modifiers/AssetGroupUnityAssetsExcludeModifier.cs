using System;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.Grouping.Modifiers
{
    [Serializable]
    public class AssetGroupUnityAssetsExcludeModifier : IAssetGroupModifier  {
        public void Modify(ARAddressableManager manager) {
            AssetDatabase.StartAssetEditing();
            var settings = manager.Settings;
            try {
                foreach (var entry in manager.data.Values) {
                    var addressableEntry = entry.Entry;
                    if (addressableEntry == null) {
                        continue;
                    }

                    if (addressableEntry.ReadOnly) {
                        Remove(entry, settings);
                    } else if (addressableEntry.MainAsset.hideFlags.HasFlag(HideFlags.DontSave)) {
                        Remove(entry, settings);
                    } else if (addressableEntry.TargetAsset.hideFlags.HasFlag(HideFlags.DontSave)) {
                        Remove(entry, settings);
                    }
                }
            } catch (Exception e) {
                Debug.LogException(e);
            } finally {
                AssetDatabase.StopAssetEditing();
            }
        }

        static void Remove(AssetEntry entry, AddressableAssetSettings settings) {
            entry.assetGroup.Remove(entry);
            settings.RemoveAssetEntry(entry.guid);
        }
    }
}
