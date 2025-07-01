using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Assets.Grouping.Modifiers;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Editor.Assets.Grouping {
    public class ARAddressableManager : ScriptableObject {
        public AssetGroupMostUsagesSplitModifier mostUsagesSplitModifier;
        [HideInInspector]
        public AssetGroupTypeSplitModifier typeSplitModifier = new();
        [HideInInspector]
        public AssetGroupPrefabsExcludeModifier prefabsExcludeModifier = new();
        [HideInInspector]
        public AssetGroupScenesExcludeModifier scenesExcludeModifier = new();
        [HideInInspector]
        public AssetGroupUsagesSplitModifier commonUsagesModifier = new();
        [HideInInspector]
        public AssetGroupUnityAssetsExcludeModifier unityAssetsExcludeModifier = new();
        public AssetGroupSplitModifier splitModifier;
        public AssetGroupMergeModifier mergeModifier;

        public bool assignGroups;

        [ReadOnly] public AddressableData data;

        [ReadOnly] public List<AssetGroup> groups = new List<AssetGroup>();

        IEnumerable<IAssetGroupModifier> Modifiers {
            get {
                yield return mostUsagesSplitModifier;
                yield return typeSplitModifier;
                yield return prefabsExcludeModifier;
                yield return scenesExcludeModifier;
                yield return commonUsagesModifier;
                yield return splitModifier;
                yield return mergeModifier;
            }
        }

        IEnumerable<IAssetGroupModifier> UpdateModifiers {
            get {
                yield return typeSplitModifier;
                yield return prefabsExcludeModifier;
                yield return scenesExcludeModifier;
                yield return mergeModifier;
            }
        }

        IEnumerable<IAssetGroupModifier> AfterAssignEntriesModifiers{
            get {
                yield return unityAssetsExcludeModifier;
            }
        }

        AddressableAssetSettings _settings;

        public AddressableAssetSettings Settings {
            get {
                if (_settings == null) {
                    _settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
                }

                return _settings;
            }
        }

        float AllStepsUpdateSteps {
            get {
                const float steps = 4f;
                var allSteps = steps + UpdateModifiers.Count();
                return allSteps;
            }
        }

        // === Operations
        [Button(ButtonSizes.Large)]
        void CreateOrUpdateData() {
            if (data != null && data.Any()) {
                UpdateData();
            } else {
                CreateData();
            }
        }

        [Button]
        void CreateData() {
            RemoveGroups();

            data = new AddressableData(Settings);

            GroupConnectedAssets();

            foreach (IAssetGroupModifier modifier in Modifiers) {
                modifier.Modify(this);
            }

            if(assignGroups)
                AssignEntriesToAddressables();

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        public void UpdateData() {
            var completedSteps = 0;

            completedSteps = UpdateData_CreateNewData(completedSteps, false);
            completedSteps = UpdateData_RemoveInvalidElements(completedSteps, false);
            completedSteps = UpdateData_AssignEntryToGroup(completedSteps, false);
            completedSteps = UpdateData_RunModifiers(completedSteps, false);

            if(assignGroups)
                AssignEntriesToAddressables();

            EditorUtility.DisplayProgressBar("UpdateData", $"Finalising", completedSteps/AllStepsUpdateSteps);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
        }

        [Button, BoxGroup("UpdateData steps", order: 2)]
        int UpdateData_CreateNewData(int completedSteps, bool clearProgressBar) {
            // Create new AddressableData with all dependencies
            EditorUtility.DisplayProgressBar("UpdateData", "Addressable data create", completedSteps++ / AllStepsUpdateSteps);
            var updated = new AddressableData(Settings);

            // Try to assign old groups to new data
            if (data != null) {
                EditorUtility.DisplayProgressBar("UpdateData", "Try to assign old groups to new data", completedSteps++ / AllStepsUpdateSteps);
                foreach (AssetEntry entry in updated.Values) {
                    if (data.TryGetValue(entry.guid, out var oldEntry)) {
                        entry.assetGroup = oldEntry.assetGroup;
                    }
                }
            }

            // Switch data to new one
            data = updated;

            if (clearProgressBar) {
                EditorUtility.ClearProgressBar();
            }
            return completedSteps;
        }

        [Button, BoxGroup("UpdateData steps", order: 2)]
        int UpdateData_RemoveInvalidElements(int completedSteps, bool clearProgressBar) {
            // Remove invalid elements in asset groups
            EditorUtility.DisplayProgressBar("UpdateData", "Remove invalid elements in asset groups",
                completedSteps++ / AllStepsUpdateSteps);
            foreach (AssetGroup assetGroup in groups.ToArray()) {
                assetGroup.elements.RemoveAll(guid => !data.ContainsKey(guid));
                if (!assetGroup.elements.Any()) {
                    RemoveGroup(assetGroup);
                }
            }

            if (clearProgressBar) {
                EditorUtility.ClearProgressBar();
            }
            return completedSteps;
        }

        [Button, BoxGroup("UpdateData steps", order: 2)]
        int UpdateData_AssignEntryToGroup(int completedSteps, bool clearProgressBar) {
            // Assign groups to homeless entries
            EditorUtility.DisplayProgressBar("UpdateData", "AssignEntryToGroup", completedSteps++ / AllStepsUpdateSteps);
            foreach (AssetEntry entry in data.Values.Where(e => e.assetGroup == null)) {
                AssignEntryToGroup(entry);
            }

            if (clearProgressBar) {
                EditorUtility.ClearProgressBar();
            }
            return completedSteps;
        }

        [Button, BoxGroup("UpdateData steps", order: 2)]
        int UpdateData_RunModifiers(int completedSteps, bool clearProgressBar) {
            // Run modifiers
            foreach (IAssetGroupModifier modifier in UpdateModifiers) {
                EditorUtility.DisplayProgressBar("UpdateData", $"Modifier {modifier.GetType().Name}",
                    completedSteps++ / AllStepsUpdateSteps);
                modifier.Modify(this);
            }

            if (clearProgressBar) {
                EditorUtility.ClearProgressBar();
            }
            return completedSteps;
        }

        [Button, BoxGroup("UpdateData steps", order: 2)]
        int UpdateData_RunModifier(int completedSteps, int index, bool clearProgressBar) {
            EditorUtility.DisplayProgressBar("UpdateData", $"Modifier {UpdateModifiers.ElementAt(index).GetType().Name}",
                completedSteps++ / AllStepsUpdateSteps);
            UpdateModifiers.ElementAt(index).Modify(this);

            if (clearProgressBar) {
                EditorUtility.ClearProgressBar();
            }
            return completedSteps;
        }

        // === Groups
        AssetGroup CreateGroup(bool saveAssets = false) {
            var group = AssetGroup.CreateInstance<AssetGroup>();
            AddGroup(group, saveAssets);
            return group;
        }

        public void AddGroup(AssetGroup group, bool saveAssets = false) {
            AssetDatabase.AddObjectToAsset(group, this);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(group, out string guid, out long id);
            string uniqueName = $"AssetGroup_{guid}_{id}";
            group.name = $"ag_{uniqueName.GetHashCode()}"; 
            groups.Add(group);
            if (saveAssets) {
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
        }

        public void RemoveGroup(AssetGroup group, bool removeEntries = false) {
            foreach (string guid in group.elements.ToArray()) {
                if (data.TryGetValue(guid, out var entry)) {
                    if (removeEntries) {
                        data.Remove(guid);
                    }
                    group.Remove(entry);
                }
            }

            groups.Remove(group);
            Settings.RemoveGroup(group.GetGroup(false));
            AssetDatabase.RemoveObjectFromAsset(group);
        }

        [Button]
        void RemoveGroups() {
            foreach (AssetGroup group in groups.ToArray()) {
                RemoveGroup(group);
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        [Button]
        void ClearData() {
            data.Clear();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        void AssignEntryToGroup(AssetEntry entry) {
            var assetPath = AssetDatabase.GUIDToAssetPath(entry.guid);
            var type = AssetGroup.GetGroupType(AssetDatabase.GetMainAssetTypeAtPath(assetPath), assetPath);
            var maxGroupSize = splitModifier.GetMaxSize(type);

            var commonUsageEntries = entry.usages.SelectMany(g => data[g].dependencies);
            var commonDependencyEntries = entry.dependencies.SelectMany(g => data[g].usages);
            var correlatedEntries = commonUsageEntries
                .Concat(commonDependencyEntries)
                .Distinct()
                .Select(g => data[g])
                .Where(e => e != entry && e.assetGroup != null && e.assetGroup.type == type
                            && e.assetGroup.elements.Count < maxGroupSize)
                .ToArray();
            if (correlatedEntries.Length == 0) {
                CreateGroup().Add(entry);
            } else {
                var possibleGroups = correlatedEntries.GroupBy(e => e.assetGroup);
                var targetGroup = possibleGroups
                    .Aggregate((a, b) => a.Count() > b.Count() ? a : b);
                targetGroup.Key.Add(entry);
            }
        }

        // === Helpers
        void GroupConnectedAssets() {
            var connectedAssets = new HashSet<string>();
            foreach (AssetEntry entry in data.Values) {
                if (entry.assetGroup == null) {
                    connectedAssets.Clear();
                    data.GetAllConnectedAssets(entry, connectedAssets);
                    var group = CreateGroup();
                    foreach (string guid in connectedAssets) {
                        AssetEntry e = data[guid];
                        if (e.assetGroup == null) {
                            group.Add(e);
                        } else {
                            Log.Important?.Warning("Something is wrong!");
                        }
                    }
                }
            }
        }

        [Button]
        void CleanEmptyEntries() {
            foreach (AssetEntry entry in data.Values) {
                if (string.IsNullOrEmpty(entry.guid)) {
                    data.Remove(entry.guid);
                    if (entry.assetGroup != null) {
                        entry.assetGroup.Remove(entry);
                        if (!entry.assetGroup.elements.Any()) {
                            RemoveGroup(entry.assetGroup);
                        }
                    }

                    foreach (string guid in entry.dependencies) {
                        ArrayUtility.Remove(ref data[guid].usages, entry.guid);
                    }

                    foreach (string guid in entry.usages) {
                        ArrayUtility.Remove(ref data[guid].dependencies, entry.guid);
                    }
                }
            }
        }

        [Button]
        public void AssignEntriesToAddressables() {
            var settings = Settings;
            AssetDatabase.StartAssetEditing();
            try {
                foreach (AssetGroup group in groups) {
                    group.GetGroup(true);
                }
            } finally {
                AssetDatabase.StopAssetEditing();
            }

            float i = 0;
            foreach (AssetGroup group in groups) {
                EditorUtility.DisplayProgressBar("Having fun with Addressables", $"Group: {group.name} {group.type}", i / groups.Count);
                i++;
                var addressablesGroup = group.GetGroup(true);

                foreach (var guid in group.elements.Where(guid => data.ContainsKey(guid))) {
                    var entry = addressablesGroup.GetAssetEntry(guid);
                    if (entry == null) {
                        settings.CreateOrMoveEntry(guid, addressablesGroup);
                    }
                }
            }

            foreach (var entriesModifier in AfterAssignEntriesModifiers) {
                entriesModifier.Modify(this);
            }

            EditorUtility.ClearProgressBar();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [Button]
        public void PrintGroupCounts(AssetGroup.AssetGroupType type) {
            foreach (var group in groups) {
                if (group.type == type) {
                    Debug.Log($"{group.name} ({group.elements.Count})");
                }
            }
        }
    }
}