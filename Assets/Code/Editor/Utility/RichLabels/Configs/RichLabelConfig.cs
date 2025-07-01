using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Memories.Journal;
using Awaken.TG.Main.Utility.RichLabels;
using Awaken.TG.Utility.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.RichLabels.Configs {
    public abstract class RichLabelConfig : ScriptableObject {
        public const string RichLabelResourcePath = "Data/RichLabelConfigs";
        
        [field: SerializeField, ColumnList] public List<RichLabelCategory> RichLabelCategories { get; private set; } = new();

        [PropertySpace(30)]
        [field: SerializeField, FoldoutGroup("Debug"), PropertyOrder(100)]
        public List<RichLabelUsageData> PersistentRichLabelUsageData { get; private set; } = new();
        
        public int CategoriesCount => GetPossibleCategories().Count();

        public abstract IEnumerable<RichLabelCategory> GetPossibleCategories();

        public int GetCategoryIndexExpensive(string richLabelGuid) {
            return GetPossibleCategories().ToList().FindIndex(p => p.Labels.Any(l => l.Guid == richLabelGuid));
        }
        
        public string GetCategoryOrEmpty(string richLabelGuid) {
            if (TryGetCategory(richLabelGuid, out var category)) {
                return category.Name;
            }

            return string.Empty;
        }
        
        public bool TryGetCategory(string richLabelGuid, out RichLabelCategory category) {
            category = null;
            foreach (var cat in GetPossibleCategories()) {
                if (cat.Labels.Any(p => p.Guid == richLabelGuid)) {
                    category = cat;
                    return true;
                }
            }

            return false;
        }

        public string GetLabelOrEmpty(string richLabelGuid) {
            if (TryGetLabel(richLabelGuid, out var label)) {
                return label.Name;
            }

            return string.Empty;
        }
        
        public bool TryGetLabel(string richLabelGuid, out RichLabel label) {
            label = null;
            foreach (var cat in GetPossibleCategories()) {
                label = cat.Labels.FirstOrDefault(p => p.Guid == richLabelGuid);
                if (label != null) {
                    return true;
                }
            }

            return false;
        }
        
        public bool TryGetLabel(string richLabelGuid, out RichLabel label, out RichLabelCategory category) {
            label = null;
            category = null;
            foreach (var cat in GetPossibleCategories()) {
                label = cat.Labels.FirstOrDefault(p => p.Guid == richLabelGuid);
                if (label != null) {
                    category = cat;
                    return true;
                }
            }

            return false;
        }
        
        public RichLabel[] TryGetSavedLabels(RichLabelSet set) {
            var richLabelCategories = GetPossibleCategories().ToList();
            RichLabel[] result = new RichLabel[richLabelCategories.Count];
            foreach (var richLabelGuid in set.richLabelGuids) {
                if (TryGetLabel(richLabelGuid, out var label, out var category)) {
                    result[richLabelCategories.IndexOf(category)] = label;
                }
            }

            return result;
        }

        RichLabelUsageData ReadPersistentData(ARGuid guid) {
            return PersistentRichLabelUsageData.FirstOrDefault(p => p.ContainsOwner(guid));
        }
        
        RichLabelUsageData ReadPersistentData(IReadOnlyList<string> labels) {
            var newDataCandidateHash = RichLabelUsageData.GenerateLabelHash(labels);
            var existing = PersistentRichLabelUsageData.FirstOrDefault(d => d.ContentHash == newDataCandidateHash);
            return existing;
        }
        
        public void SetPersistentData(RichLabelSet set) {
            SetPersistentData_Internal(set);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        void SetPersistentData_Internal(RichLabelSet set) {
            if (set.richLabelGuids == null || set.richLabelGuids.Count == 0 || set.richLabelGuids.All(l => l == null)) {
                return;
            }

            ARGuid owner = set.OwnerGuid;
            var labels = set.richLabelGuids;
            
            RichLabelUsageData oldContainer = ReadPersistentData(owner);
            RichLabelUsageData existing = ReadPersistentData(labels);

            // our new data already exists
            if (existing != null) {
                if (oldContainer != null) {
                    if (oldContainer == existing) {
                        return;
                    }
                    // are we the last owner of the container?
                    if (oldContainer.Owners.Count == 1) {
                        PersistentRichLabelUsageData.Remove(oldContainer);
                    } else {
                        oldContainer.RemoveOwner(owner);
                    }
                }
                existing.AddOwner(owner);
                return;
            }
            if (oldContainer == null) {
                // our save data is unique, and we do not have a container
                PersistentRichLabelUsageData.Add(new RichLabelUsageData(owner, labels));
                return;
            }
            
            if (oldContainer.Owners.Count == 1) {
                // our new data does not exist anywhere else, we use our existing container
                oldContainer.ChangeLabelGuids(labels);
                
                // re-add old container to keep history in order
                PersistentRichLabelUsageData.Remove(oldContainer);
                PersistentRichLabelUsageData.Add(oldContainer);
                return;
            }

            // not the only owner and our new data is unique: we need to create a new copy
            RichLabelUsageData newDataSet = new(owner, labels);
            PersistentRichLabelUsageData.Add(newDataSet);
            oldContainer.RemoveOwner(owner);
        }

        public void RemovePersistentData(RichLabelSet set) {
            var data = PersistentRichLabelUsageData.FirstOrDefault(p => p.ContainsOwner(set.OwnerGuid));
            if (data != null) {
                if (data.Owners.Count == 1) {
                    PersistentRichLabelUsageData.Remove(data);
                } else {
                    data.RemoveOwner(set.OwnerGuid);
                }
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
        
        [Button, FoldoutGroup("Debug")]
        void MergeDataWithSameContentHash(bool force = false) {
            var data = PersistentRichLabelUsageData.ToList();
            for (int i = 0; i < data.Count; i++) {
                var current = data[i];
                current.EnsureContentHash(force);
                for (int j = i + 1; j < data.Count; j++) {
                    var other = data[j];
                    other.EnsureContentHash(force);
                    if (current.ContentHash == other.ContentHash) {
                        current.AddMultipleOwners(other.Owners);
                        PersistentRichLabelUsageData.Remove(other);
                    }
                }
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }

    [UnityEditor.InitializeOnLoad]
    public static class RichLabelNonPersistentDataManager {
        public static Dictionary<string, int> guidByInstanceId = new();

        static RichLabelNonPersistentDataManager() {
            UnityEditor.EditorApplication.playModeStateChanged += HandleAssetReset;
        }

        static void HandleAssetReset(UnityEditor.PlayModeStateChange playModeStateChange) {
            Reset();
        }

        static void Reset() {
            guidByInstanceId.Clear();
        }
    }
}