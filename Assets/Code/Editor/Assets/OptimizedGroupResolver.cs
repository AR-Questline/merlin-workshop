using System.Collections.Generic;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Localization.Addressables;
using UnityEngine;
using UnityEngine.Localization;
using LogType = Awaken.Utility.Debugging.LogType;

namespace Awaken.TG.Editor.Assets {
    public class OptimizedGroupResolver : GroupResolver {
        public OptimizedGroupResolver() {}
        public OptimizedGroupResolver(string groupName) : base(groupName) {}
        public OptimizedGroupResolver(AddressableAssetGroup group) : base(group) {}
        public OptimizedGroupResolver(string localeGroupNamePattern, string sharedGroupName) : base(localeGroupNamePattern, sharedGroupName) {}

        public override AddressableAssetEntry AddToGroup(Object asset, IList<LocaleIdentifier> locales, AddressableAssetSettings aaSettings, bool createUndo) {
            var group = SharedGroup ?? GetGroup(locales, asset, aaSettings, createUndo);
            var guid = GetAssetGuid(asset);

            AddressableAssetEntry assetEntry = group.GetAssetEntry(guid);

            if (assetEntry == null) {
                if (createUndo)
                    Undo.RecordObjects(new Object[] { aaSettings, group }, "Add to group");
                assetEntry = aaSettings.CreateOrMoveEntry(guid, group, MarkEntriesReadOnly);
            }

            return assetEntry;
        }

        // Copied from GroupResolver
        static string GetAssetGuid(Object asset) {
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out long _))
                return guid;

            Log.Important?.Error("Failed to extract the asset Guid for " + asset.name, asset);
            return null;
        }
    }
}
