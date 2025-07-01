using System;
using System.Collections.Generic;
using System.Linq;

namespace Awaken.TG.Editor.Assets.Grouping.Modifiers {
    [Serializable]
    public class AssetGroupMergeModifier : IAssetGroupModifier {
        public int
            defaultMinSize,
            prefabsMinSize,
            texturesMinSize,
            materialsMinSize,
            meshesMinSize,
            shadersMinSize,
            beardMinSize = 1;

        public void Modify(ARAddressableManager manager) {
            var groups = manager.groups.GroupBy(g => g.type);
            foreach (IGrouping<AssetGroup.AssetGroupType, AssetGroup> grouping in groups) {
                Merge(grouping, GetMinSize(grouping.Key));
            }
        }

        public void Merge(IEnumerable<AssetGroup> groups, int minSize) {
            var toMerge = groups.Where(g => g.elements.Count < minSize)
                .OrderBy(g => -g.elements.Count).ToList();
            while (toMerge.Count > 0) {
                var target = toMerge[0];
                toMerge.RemoveAt(0);
                while (toMerge.Count > 0 &&  target.elements.Count < minSize) {
                    var index = toMerge.Count - 1;
                    target.Merge(toMerge[index]);
                    toMerge.RemoveAt(index);
                }
            }
        }

        public int GetMinSize(AssetGroup.AssetGroupType type) {
            return type switch {
                AssetGroup.AssetGroupType.Prefabs => prefabsMinSize,
                AssetGroup.AssetGroupType.Textures => texturesMinSize,
                AssetGroup.AssetGroupType.Materials => materialsMinSize,
                AssetGroup.AssetGroupType.Meshes => meshesMinSize,
                AssetGroup.AssetGroupType.Shaders => shadersMinSize,
                AssetGroup.AssetGroupType.Beards => beardMinSize,
                _ => defaultMinSize
            };
        }
    }
}