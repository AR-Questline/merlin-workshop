using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Collections;

namespace Awaken.TG.Editor.Assets.Grouping.Modifiers {
    [Serializable]
    public class AssetGroupUsagesSplitModifier : IAssetGroupModifier{
        public void Modify(ARAddressableManager manager) {
            foreach (AssetGroup group in manager.groups.ToArray()) {
                Modify(manager, group);
            }
        }

        void Modify(ARAddressableManager manager, AssetGroup group) {
            var data = manager.data;
            
            var usageMap = new Dictionary<HashSet<string>, HashSet<string>>();
            foreach (string guid in group.elements) {
                var entry = data[guid];
                HashSet<string> key = usageMap.Keys.FirstOrDefault(k => k.Any(g => entry.usages.Contains(g)));
                if (key == null) {
                    key = entry.usages.ToHashSet();
                    (usageMap[key] = new HashSet<string>()).Add(guid);
                } else {
                    key.AddRange(entry.usages);
                    usageMap[key].Add(guid);
                }
            }
            
            group.Split(usageMap.Values.ToArray());
        }
    }
}