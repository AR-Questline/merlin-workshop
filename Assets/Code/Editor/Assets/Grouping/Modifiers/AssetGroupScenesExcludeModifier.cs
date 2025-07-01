using System;

namespace Awaken.TG.Editor.Assets.Grouping.Modifiers {
    [Serializable]
    public class AssetGroupScenesExcludeModifier : IAssetGroupModifier  {
        public void Modify(ARAddressableManager manager) {
            foreach (AssetGroup assetGroup in manager.groups.ToArray()) {
                if (assetGroup.type == AssetGroup.AssetGroupType.Scenes) {
                    manager.RemoveGroup(assetGroup, true);
                }
            }
        }
    }
}