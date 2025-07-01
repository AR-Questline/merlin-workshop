using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.Grouping {
    [Serializable]
    public class AddressableData : Dictionary<string, AssetEntry>, ISerializationCallbackReceiver {
        public AddressableData(AddressableAssetSettings settings) {
            var usages = new MultiMap<string, string>();
            ExtractPrefabsFromGroups(settings, usages);
            ExtractUsages(usages);
            AttachUsages(usages);
        }

        void ExtractPrefabsFromGroups(AddressableAssetSettings settings, MultiMap<string, string> usages) {
            foreach (var group in settings.groups.Where(ShouldProcess)) {
                foreach (var entry in group.entries) {
                    AddEntry(new AssetEntry(entry.guid, entry.AssetPath), usages);
                }
            }
        }

        bool ShouldProcess(AddressableAssetGroup group) {
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            if (schema != null && !schema.IncludeInBuild) {
                return false;
            }
            
            if (group.name.StartsWith("Templates")) {
                return false;
            }
            if (group.name.StartsWith("Localization")) {
                return false;
            }
            if (group.name.StartsWith("UniqueTextures")) {
                return false;
            }
            if (group.name.StartsWith("Video")) {
                return false;
            }
            
            return true;
        }

        void ExtractUsages(MultiMap<string, string> usages) {
            var unregisteredUsages = Enumerable.Empty<string>();
            do {
                foreach (string usage in unregisteredUsages) {
                    AddEntry(new AssetEntry(usage, AssetDatabase.GUIDToAssetPath(usage)), usages);
                }
                unregisteredUsages = usages.Keys.Where(k => !ContainsKey(k));
            } while (unregisteredUsages.Any());
        }

        void AttachUsages(MultiMap<string, string> usages) {
            foreach (AssetEntry entry in Values) {
                entry.usages = usages.GetValues(entry.guid, true).ToArray();
            }
        }

        void AddEntry(AssetEntry entry, MultiMap<string, string> usages) {
            Add(entry.guid, entry);
            foreach (string dependency in entry.dependencies) {
                usages.Add(dependency, entry.guid);
            }
        }

        public void GetAllConnectedAssets(AssetEntry assetEntry, HashSet<string> result) {
            result.Add(assetEntry.guid);
            foreach (string guid in assetEntry.dependencies.Concat(assetEntry.usages)) {
                if (!result.Contains(guid)) {
                    GetAllConnectedAssets(this[guid], result); 
                }
            }
        }
        
        [HideInInspector] [SerializeField] List<string> _keys = new List<string>();
        [HideInInspector] [SerializeField] List<AssetEntry> _values = new List<AssetEntry>();

        public void OnBeforeSerialize() {
            _keys ??= new List<string>();
            _values ??= new List<AssetEntry>();
            _keys.Clear();
            _values.Clear();

            foreach (var kvp in this) {
                _keys.Add(kvp.Key);
                _values.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize() {
            Clear();

            for (var i = 0; i != Math.Min(_keys.Count, _values.Count); i++) {
                Add(_keys[i], _values[i]);
            }
        }
    }
}