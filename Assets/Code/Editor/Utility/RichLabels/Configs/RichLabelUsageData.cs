using System;
using System.Collections.Generic;
using Awaken.TG.Main.Memories.Journal;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.RichLabels.Configs {
    [Serializable]
    public class RichLabelUsageData {
        [ReadOnly, SerializeField]
        List<ARGuid> owners = new();
        [ReadOnly, SerializeField]
        List<string> richLabelGuids = new();

        [field: ReadOnly, SerializeField] public int ContentHash { get; private set; }
        
        public IReadOnlyList<string> RichLabelGuids => richLabelGuids;
        public IReadOnlyList<ARGuid> Owners => owners;

        public RichLabelUsageData() { }

        public RichLabelUsageData(ARGuid owner, IEnumerable<string> guids) {
            owners.Add(owner);
            richLabelGuids.AddRange(guids);
            richLabelGuids.Sort();
            ContentHash = GenerateLabelHash(richLabelGuids);
        }

        public RichLabelUsageData(RichLabelUsageData other) {
            owners.AddRange(other.owners);
            richLabelGuids.AddRange(other.richLabelGuids);
            ContentHash = other.ContentHash;
        }
        
        public void ChangeLabelGuids(IEnumerable<string> newGuids) {
            richLabelGuids.Clear();
            richLabelGuids.AddRange(newGuids);
            richLabelGuids.Sort();
            ContentHash = GenerateLabelHash(richLabelGuids);
        }
        
        public void EnsureContentHash(bool force = false) {
            if (ContentHash == 0 || force) {
                if (force) {
                    richLabelGuids.Sort();
                }
                ContentHash = GenerateLabelHash(richLabelGuids);
            }
        }
        public bool Contains(string value) {
            return richLabelGuids.Contains(value);
        }
        
        public void AddOwner(ARGuid owner) {
            if (!owners.Contains(owner)) {
                owners.Add(owner);
            }
        }
        
        public void AddMultipleOwners(IEnumerable<ARGuid> newOwners) {
            foreach (var owner in newOwners) {
                AddOwner(owner);
            }
        }
        
        public void RemoveOwner(ARGuid owner) {
            owners.Remove(owner);
        }
        
        public bool ContainsOwner(ARGuid owner) => owners.Contains(owner);

        public static int GenerateLabelHash(IReadOnlyList<string> richLabelGuids) {
            const int Prime = 31;
            int result = 1;
            for (var index = 0; index < richLabelGuids.Count; index++) {
                result = result * Prime + richLabelGuids[index].GetHashCode();
            }

            return result;
        }
    }
}