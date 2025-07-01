using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.Grouping.Modifiers {
    [Serializable]
    public class AssetGroupMostUsagesSplitModifier : IAssetGroupModifier {

        public int minGroupSize;
        [Range(0f, 1f)]
        public float partToSplit;
        
        public void Modify(ARAddressableManager manager) {
            foreach (var group in manager.groups.ToArray()) {
                var count = group.elements.Count;
                if (count >= minGroupSize) {
                    var sorted = group.elements.OrderBy(guid => -manager.data[guid].usages.Length);
                    var toSplit = (int) (partToSplit * count);
                    var sets = new[] {
                        sorted.Take(toSplit).ToHashSet(),
                        sorted.Skip(toSplit).ToHashSet()
                    };
                    group.Split(sets);
                }
            }
        }
    }
}