using System;
using System.Linq;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.Grouping.Modifiers {
    [Serializable]
    public class AssetGroupSplitModifier : IAssetGroupModifier {

        public int
            defaultMaxSize,
            prefabsMaxSize,
            texturesMaxSize,
            materialsMaxSize,
            meshesMaxSize,
            shadersMaxSize,
            beardMinSize = 1;

        public void Modify(ARAddressableManager manager) {
            foreach (AssetGroup group in manager.groups.ToArray()) {
                var maxSize = GetMaxSize(group.type);
                if (group.elements.Count > maxSize) {
                    Split(group, Mathf.CeilToInt((float)group.elements.Count / maxSize));
                }
            }
        }

        void Split(AssetGroup group, int groupCount) {
            int i = 0;
            var sets = group.elements.GroupBy(_ => (i++) % groupCount).Select(Enumerable.ToHashSet);
            group.Split(sets.ToArray());
        }

        public int GetMaxSize(AssetGroup.AssetGroupType type) {
            return type switch {
                AssetGroup.AssetGroupType.Prefabs => prefabsMaxSize,
                AssetGroup.AssetGroupType.Textures => texturesMaxSize,
                AssetGroup.AssetGroupType.Materials => materialsMaxSize,
                AssetGroup.AssetGroupType.Meshes => meshesMaxSize,
                AssetGroup.AssetGroupType.Shaders => shadersMaxSize,
                AssetGroup.AssetGroupType.Beards => beardMinSize,
                _ => defaultMaxSize
            };
        }
    }
}