using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Awaken.TG.Assets;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;
using LogType = Awaken.Utility.Debugging.LogType;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Assets {
    /// <summary>
    /// Contains editor utilities, whose allows dealing with addressables in editor 
    /// </summary>
    public static class AddressableHelper
    {
        public static AddressableAssetSettings Settings => AddressableAssetSettingsDefaultObject.GetSettings(true);
        
        public static string GetAddress(AssetReference assetReference) {
            var entry = FindEntry(assetReference.AssetGUID);
            if (entry == null) {
                return "";
            }
            return entry.address;
        }

        public static AddressableAssetEntry GetEntry(ARAssetReference reference) {
            return GetEntry(reference?.Address, reference?.SubObjectName);
        }
        
        public static AddressableAssetEntry GetEntry(AssetReference reference) {
            return GetEntry(reference?.AssetGUID, reference?.SubObjectName);
        }

        public static AddressableAssetEntry GetEntry(string address, string subObject) {
            if (string.IsNullOrEmpty(address)) return null;
            var entry = FindEntry(address);
            if (entry == null || string.IsNullOrWhiteSpace(subObject)) {
                return entry;
            }
            var assets = new List<AddressableAssetEntry>();
            entry.GatherAllAssets(assets, false, false, true, _ => true);
            return assets.FirstOrDefault(e => e.TargetAsset.name == subObject);
        }

        public static void EnsureAsset(
            string guid,
            Func<Object, AddressableAssetEntry, string, string> groupProvider = null,
            Func<Object, AddressableAssetEntry, string> addressProvider = null,
            Func<Object, AddressableAssetEntry, HashSet<string>> labelsProvider = null) {
            EnsureAsset( guid, null, groupProvider, addressProvider, labelsProvider );
        }
        
        public static void EnsureAsset(
            AssetReference assetReference,
            Func<Object, AddressableAssetEntry, string, string> groupProvider = null,
            Func<Object, AddressableAssetEntry, string> addressProvider = null,
            Func<Object, AddressableAssetEntry, HashSet<string>> labelsProvider = null) {
            
            var entry = FindEntry(assetReference.AssetGUID);
            if (entry == null) {
                return;
            }
            var target = FindFirstEntry<Object>(assetReference);
            if (target == null) {
                return;
            }

            entry = ChangeGroup(assetReference, groupProvider, entry, target);

            ChangeAddress(addressProvider, entry, target);

            ChangeLabels(labelsProvider, entry, target);
        }

        public static void EnsureAsset(
            string address, string subObject,
            Func<Object, AddressableAssetEntry, string, string> groupProvider = null,
            Func<Object, AddressableAssetEntry, string> addressProvider = null,
            Func<Object, AddressableAssetEntry, HashSet<string>> labelsProvider = null) {
            
            var entry = FindEntry(address);
            if (entry == null) {
                return;
            }
            var target = FindFirstEntry<Object>(address, subObject);
            if (target == null) {
                return;
            }

            entry = ChangeGroup(address, groupProvider, entry, target);

            ChangeAddress(addressProvider, entry, target);

            ChangeLabels(labelsProvider, entry, target);
            
        }
        
        // === Loading asset
        public static List<AddressableAssetEntry> FindByLabels(ICollection<string> labels) {
            List<AddressableAssetEntry> entries = new List<AddressableAssetEntry>();
            Settings.GetAllAssets( entries, false, null, assetEntry => labels.Count == assetEntry.labels.Intersect(labels).Count());
            return entries;
        }
        public static List<T> FindByLabel<T>(string label) where T : Object {
            List<AddressableAssetEntry> entries = new List<AddressableAssetEntry>();
            Settings.GetAllAssets( entries, false, null, assetEntry => assetEntry.labels.Contains(label));

            return entries.Select(e => AssetDatabase.LoadAssetAtPath<T>(e.AssetPath)).Where(a => a != null).ToList();
        }


        public static T FindFirstEntry<T>(ARAssetReference reference) where T : Object {
            return FindFirstEntry<T>(reference?.Address, reference?.SubObjectName);
        }
        
        public static T FindFirstEntry<T>(AssetReference reference) where T : Object {
            return FindFirstEntry<T>(reference?.AssetGUID, reference?.SubObjectName);
        }

        public static T FindFirstEntry<T>(string address, string subObject = null) where T : Object {
            // Check if asset is addressable
            var entry = FindEntry(address);
            if (entry == null) {
                return null;
            }
            
            // Check if we want load main asset or sub asset
            if (string.IsNullOrWhiteSpace(subObject)) {
                return AssetDatabase.LoadAssetAtPath<T>(entry.AssetPath);
            } else {
                return AssetDatabase.LoadAllAssetsAtPath(entry.AssetPath).OfType<T>().FirstOrDefault(asset => asset.name.Equals(subObject));
            }
            
        }

        // === Adding assets to Addressables
        public static string AddEntry(AddressableEntryDraft entryDraft, bool saveAssets = true) {
            var entry = FindEntry(entryDraft.Guid) ?? CreateEntry(entryDraft);

            if (entryDraft.AddressProvider != null) {
                entry.address = entryDraft.AddressProvider.Invoke(entryDraft.Obj, entry);
            }
            
            foreach (string label in entryDraft.Labels) {
                entry.SetLabel(label, true, true);
            }
            if (saveAssets) {
                AssetDatabase.SaveAssetIfDirty(entryDraft.Group);
                AssetDatabase.SaveAssetIfDirty(Settings);
            }
            
            return entry.guid;

            static AddressableAssetEntry CreateEntry(AddressableEntryDraft draft) {
                var entry = Settings.CreateOrMoveEntry(draft.Guid, draft.Group);
                entry.address = AssetDatabase.GetAssetPath(draft.Obj);
                return entry;
            }
        }


        public static void RemoveEntry(string guid) {
            Log.Debug?.Error($"Remove {guid}");
            Settings.RemoveAssetEntry(guid);
        }

        public static void MoveEntry(AddressableAssetEntry entry, AddressableAssetGroup group) {
            Log.Debug?.Error($"Move {entry.guid} to {group.Name}");
            Settings.MoveEntry(entry, group);
        }
        
        public static ARAssetReference MakeReference(Object obj, string group) {
            var guid = AddEntry(new AddressableEntryDraft.Builder(obj).InGroup(group).Build());
            return new ARAssetReference(guid);
        }

        public static ARAssetReference MakeReference(Object obj, AddressableGroup group) {
            var guid = AddEntry(new AddressableEntryDraft.Builder(obj).InGroup(group).Build());
            return new ARAssetReference(guid);
        }

        // === Groups
        public static AddressableAssetGroup FindGroup(string name) {
            return Settings.FindGroup(name) ?? Settings.DefaultGroup;
        }
        
        public static AddressableAssetGroup FindOrCreateGroup(string name) {
            return Settings.FindGroup(name) ?? Settings.CreateGroup(name, false, false, false, Settings.DefaultGroup.Schemas) ;
        }
        
        // === Structure changing
        public static AddressableAssetEntry ChangeGroup(AssetReference assetReference, Func<Object, AddressableAssetEntry, string, string> groupProvider, AddressableAssetEntry entry, Object target) {
            return ChangeGroup(assetReference.AssetGUID, groupProvider, entry, target);
        }

        public static AddressableAssetEntry ChangeGroup(string address, Func<Object, AddressableAssetEntry, string, string> groupProvider, AddressableAssetEntry entry, Object target) {
            if (groupProvider != null) {
                var groupName = entry.parentGroup.Name;
                var newGroup = groupProvider(target, entry, groupName);
                if (groupName != newGroup) {
                    entry = Settings.CreateOrMoveEntry(address, FindOrCreateGroup(newGroup));
                }
            }

            return entry;
        }
        
        public static void ChangeAddress(Func<Object, AddressableAssetEntry, string> addressProvider, AddressableAssetEntry entry, Object target) {
            if (addressProvider != null) {
                var oldAddress = entry.address;
                var newAddress = addressProvider(target, entry);
                if (newAddress != oldAddress) {
                    entry.address = newAddress;
                }
            }
        }
        
        public static void ChangeLabels(Func<Object, AddressableAssetEntry, HashSet<string>> labelsProvider, AddressableAssetEntry entry, Object target) {
            if (labelsProvider != null) {
                var oldLabels = new HashSet<string>(entry.labels);
                var newLabels = labelsProvider(target, entry);
                if (oldLabels.SetEquals(newLabels)) {
                    return;
                }

                foreach (string label in oldLabels) {
                    entry.SetLabel(label, false, true, false);
                }

                foreach (string label in newLabels) {
                    entry.SetLabel(label, true, true, false);
                }
            }
        }

        public static bool IsAddressable(string guid) {
            return FindEntry(guid) != null;
        }

        // === Optimizations
        static AddressableAssetEntry FindEntry(string address) {
            AddressableAssetEntry entry = null;
            var groups = Settings.groups;
            int i = 0;
            while (entry == null && i < groups.Count) {
                entry = groups[i].GetAssetEntry(address);
                ++i;
            }
            return entry;
        }
        
        public static HashSet<string> ExistingAddresses() {
            var result = new HashSet<string>();
            var groups = Settings.groups;
            foreach (var group in groups) {
                foreach (var entry in group.entries) {
                    result.Add(entry.guid);
                }
            }

            return result;
        }
        
        public static void SetAddressablesToUseExistingBuild() => SetAddressablesPlayModeBuilderScript<BuildScriptPackedPlayMode>();
        
        public static void SetAddressablesPlayModeBuilderScript<T>() where T : ScriptableObject, IDataBuilder {
            var settings = Settings;
            int index = settings.DataBuilders.FindIndex(static x => x is T);
            if (index >= 0) {
                settings.ActivePlayModeDataBuilderIndex = index;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            } else {
                Log.Minor?.Error($"{typeof(T).Name} must be added to the DataBuilders list before it can be made active. Using last run builder instead.");
            }
            
        }

        static bool TryGetAddressablesBuilder<T>(out T builder) where T : ScriptableObject, IDataBuilder {
            var guids = AssetDatabase.FindAssets($"t: {typeof(T).Name}");
            if (guids.Length == 0) {
                builder = null;
                return false;
            }
            builder = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
            return builder != null;
        }
    }

    public static class AddressableHelperEditor {
        const string TestModeMenuName = "TG/Addressables/Test mode";
        
        [MenuItem(TestModeMenuName)]
        public static void TestMode() {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var isBuildMode = settings.ActivePlayModeDataBuilder is BuildScriptPackedPlayMode;
            if (!isBuildMode) {
                try {
                    AddressableHelper.SetAddressablesPlayModeBuilderScript<BuildScriptPackedPlayMode>();
                    AddressableAssetSettings.BuildPlayerContent();
                } catch (Exception e) {
                    Debug.LogException(e);
                    // If error return to fast mode
                    isBuildMode = true;
                }
            }
            if (isBuildMode) {
                AddressableHelper.SetAddressablesPlayModeBuilderScript<BuildScriptFastMode>();
            } 
        }
        
        [MenuItem(TestModeMenuName, true)]
        public static bool TestModeVal() {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var isBuildMode = settings.ActivePlayModeDataBuilder is BuildScriptPackedPlayMode;
            if (isBuildMode != Menu.GetChecked(TestModeMenuName)) {
                Menu.SetChecked(TestModeMenuName, isBuildMode);
            }
            return true;
        }

        [MenuItem("TG/Addressables/Find invalid entries")]
        public static void CheckEntries() {
            List<AddressableAssetEntry> invalidEntries = FindInvalidEntries();
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            foreach (AddressableAssetEntry assetEntry in invalidEntries) {
                Log.Important?.Info($"Removed addressable entry {assetEntry.address} from group {assetEntry.parentGroup.Name}");
                settings.RemoveAssetEntry(assetEntry.guid);
            }

            if (invalidEntries.Count > 0) {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
        
        static List<AddressableAssetEntry> FindInvalidEntries() {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var groups = settings.groups;
            var invalidEntries = new List<AddressableAssetEntry>();
            foreach (AddressableAssetGroup assetGroup in groups) {
                if (assetGroup.ReadOnly) {
                    continue;
                }
                foreach (AddressableAssetEntry entry in assetGroup.entries) {
                    if (string.IsNullOrWhiteSpace(entry.AssetPath)) {
                        invalidEntries.Add(entry);
                    }
                }
            }
            return invalidEntries;
        }

        static readonly Regex ResourcesPathRegex = new Regex(@"\/Resources\/", RegexOptions.Compiled);
        public static bool IsResourcesPath(string assetPath) {
            return ResourcesPathRegex.IsMatch(assetPath);
        }
    }
}