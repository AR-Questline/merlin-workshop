using System;
using System.Linq;

namespace Awaken.TG.Editor.Assets.Grouping.Modifiers {
    [Serializable]
    public class AssetGroupTypeSplitModifier : IAssetGroupModifier {
        public void Modify(ARAddressableManager manager) {
            foreach (AssetGroup group in manager.groups.ToArray()) {
                Modify(manager, group);
            }

            foreach (AssetGroup group in manager.groups) {
                group.SetGroupTypeFromFirstAsset();
            }
        }

        static void Modify(ARAddressableManager manager, AssetGroup group) {
            var subgroups = group.elements.GroupBy(guid => manager.data[guid].TypeName);
            var sets = subgroups.Select(Enumerable.ToHashSet).ToArray();
            @group.Split(sets);
        }
    }
}