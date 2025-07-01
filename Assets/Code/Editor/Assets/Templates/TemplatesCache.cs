using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Debugging;
using UnityEditor.AddressableAssets.Settings;

namespace Awaken.TG.Editor.Assets.Templates {
    public class TemplatesCache {
        const float CheckDelaySec = 5;

        AddressableAssetGroup[] _groups;
        int _startingEntriesHash;
        DateTime _lastCheckedTime;
        
        public List<ITemplate> Templates { get; private set; }
        public List<AddressableAssetEntry> Entries { get; private set; }

        public TemplatesCache(AddressableAssetGroup[] groups) {
            _groups = groups;
            Templates = new List<ITemplate>();
            Entries = new List<AddressableAssetEntry>();
            Refresh();
        }

        public void Refresh() {
            _startingEntriesHash = GetEntriesHash(_groups);
            _lastCheckedTime = DateTime.Now;
            Templates.Clear();
            Entries.Clear();

            foreach (AddressableAssetEntry entry in _groups.SelectMany(static g => g.entries)) {
                if (entry.TargetAsset != null) {
                    try {
                        var template = TemplatesUtil.ObjectToTemplate(entry.TargetAsset);
                        template.GUID = entry.guid;
                        Templates.Add(template);
                        Entries.Add(entry);
                    } catch (Exception) {
                        Log.Important?.Error($"Invalid addressable entry guid: {entry.guid} (try Reimport?)");
                        throw;
                    }
                }
            }
        }
        
        public bool ShouldBeRefreshed() {
            if (TemplatesSearcher.Groups.Sum(g => g.entries.Count) != Entries.Count) {
                return true;
            }

            if ((DateTime.Now - _lastCheckedTime).TotalSeconds > CheckDelaySec) {
                _lastCheckedTime = DateTime.Now;
                return AreHashesDifferent();
            }

            return false;
        }

        bool AreHashesDifferent() {
            int currentHash = GetEntriesHash(TemplatesSearcher.Groups);
            return currentHash != _startingEntriesHash;
        }

        int GetEntriesHash(IEnumerable<AddressableAssetGroup> groups) {
            return GetEntriesHash(groups.SelectMany(g => g.entries));
        }
        int GetEntriesHash(IEnumerable<AddressableAssetEntry> entries) {
            return ((IStructuralEquatable)entries.ToArray()).GetHashCode(EqualityComparer<AddressableAssetEntry>.Default);
        }
    }
}