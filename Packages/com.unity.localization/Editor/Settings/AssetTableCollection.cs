using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Localization.Addressables;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Tables;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace UnityEditor.Localization
{
    /// <summary>
    /// Contains information on all the tables associated with a table collection name and type.
    /// </summary>
    public class AssetTableCollection : LocalizationTableCollection
    {
        /// <inheritdoc/>
        protected internal override Type TableType => typeof(AssetTable);

        /// <inheritdoc/>
        protected internal override Type RequiredExtensionAttribute => typeof(AssetTableCollectionExtensionAttribute);

        const string k_AssetTypeSetByScript = "set-by-script";

        /// <summary>
        /// A helper property which is the contents of <see cref="LocalizationTableCollection.Tables"/> loaded and cast to <see cref="AssetTable"/>.
        /// </summary>
        public virtual ReadOnlyCollection<AssetTable> AssetTables => new ReadOnlyCollection<AssetTable>(Tables.Select(t => t.asset as AssetTable).ToList().AsReadOnly());

        /// <inheritdoc/>
        protected internal override string DefaultGroupName => "Asset Table";

        /// <summary>
        /// Returns an enumerator that can be used to step through each key and its localized values, such as in a foreach loop.
        /// Internally <see cref="SharedTableData"/> and <see cref="AssetTable"/>'s are separate assets with their own internal list of values.
        /// This means that when iterating through each key a lookup must be made in each table in order to retrieve the localized value,
        /// this can become slow when dealing with a large number of tables and entries.
        /// GetRowEnumerator improves this process by first sorting the multiple internal lists and then stepping through each conceptual row at a time.
        /// It handles missing keys and table entries and provides a more efficient and faster way to iterate through the tables.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Row<AssetTableEntry>> GetRowEnumerator() => GetRowEnumerator<AssetTable, AssetTableEntry>(AssetTables);

        /// <inheritdoc/>
        protected override void RemoveTableFromAddressables(LocalizationTable table, bool createUndo)
        {
            if (table is AssetTable assetTable)
            {
                // This can be called when a table is about to be deleted or when it is removed from the collection.
                // If the asset is removed from the collection it will still exists but not be part of a build, essentially is is turned off.
                // A user may want to turn the asset back on at a later time and have all the assets restored, so when we remove the table we need
                // to keep its entries so that they can be added again.
                // We can do this by saving the state using JsonUtility and then restoring it afterwards.

                // Save the state
                var json = JsonUtility.ToJson(table);

                // We need to make a copy as the original will be modified as we remove each item.
                var valuesCopy = assetTable.Values.ToArray();
                foreach (var entry in valuesCopy)
                {
                    RemoveAssetFromTable(assetTable, entry.KeyId);
                }

                // Restore the entries
                JsonUtility.FromJsonOverwrite(json, table);
            }

            base.RemoveTableFromAddressables(table, createUndo);
        }

        /// <summary>
        /// Add a localized asset to the asset table that matches the <see cref="LocaleIdentifier"/>.
        /// This function ensures the localization system adds the asset to the Addressables system and sets the asset up for use.
        /// </summary>
        /// <param name="localeIdentifier">The table to add the asset to, if a table with the id does not exist a new one will be created.</param>
        /// <param name="entryReference">The table entry Key or Key Id.</param>
        /// <param name="asset">The asset to add.</param>
        /// <param name="createUndo">Should an undo operation be created?</param>
        public void AddAssetToTable(LocaleIdentifier localeIdentifier, TableEntryReference entryReference, Object asset, bool createUndo = false)
        {
            var table = GetTable(localeIdentifier) ?? AddNewTable(localeIdentifier);
            AddAssetToTable(table as AssetTable, entryReference, asset, createUndo);
        }

        /// <summary>
        /// Add a localized asset to the asset table.
        /// This function will ensure the localization system adds the asset to the Addressables system and sets the asset up for use.
        /// </summary>
        /// <param name="table">The table to add the asset to, must be part of the collection.</param>
        /// <param name="entryReference">The table entry Key or Key Id.</param>
        /// <param name="asset">The asset to add.</param>
        /// <param name="createUndo">Should an undo operation be created?</param>
        public virtual void AddAssetToTable(AssetTable table, TableEntryReference entryReference, Object asset, bool createUndo = false)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table), "Can not add asset to null table");

            if (asset == null)
                throw new ArgumentNullException(nameof(asset), "Can not add null asset to table");

            if (!ContainsTable(table))
            {
                throw new Exception("The table does not belong to this collection.");
            }

            if (!IsPersistentAsset(table))
                throw new AssetNotPersistentException(table);

            if (!IsPersistentAsset(asset))
                throw new AssetNotPersistentException(asset);

            // Reject Editor assets such as Folders. (LOC-1107)
            if (asset is DefaultAsset)
                throw new ArgumentException(nameof(asset), $"Can not add unsupported asset `{asset}` to table.");

            var path = AssetDatabase.GetAssetPath(asset);
            if (AssetUtility.IsBuiltInResource(path))
                throw new ArgumentException($"Builtin resources are not supported. Can not add {asset} from {path}.", nameof(asset));

            // Add the asset to the Addressables system and setup the table with the key to guid mapping.
            var aaSettings = LocalizationEditorSettings.Instance.GetAddressableAssetSettings(true);
            if (aaSettings == null)
                return;

            using (new UndoScope("Add asset to table", createUndo))
            {
                if (createUndo)
                    Undo.RecordObject(aaSettings, "Add asset to table");

                // Remove the old asset first
                var assetGuid = LocalizationEditorSettings.Instance.GetAssetGuid(asset);
                var tableEntry = table.GetEntryFromReference(entryReference);
                if (tableEntry != null)
                {
                    if (tableEntry.Guid != assetGuid)
                        RemoveAssetFromTable(table, entryReference, createUndo);
                }

                // Has the asset already been added? Perhaps it is being used by multiple tables or the user has added it manually.
                var entry = aaSettings.FindAssetEntry(assetGuid);
                var entryLabel = AddressHelper.FormatAssetLabel(table.LocaleIdentifier.Code);
                aaSettings.AddLabel(entryLabel);

                if (entry == null)
                {
                    entry = AddressableGroupRules.AddAssetToGroup(asset, new[] {table.LocaleIdentifier}, aaSettings, createUndo);
                    entry.SetLabel(entryLabel, true, true);

                    var mainAsset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(asset));
                    if (mainAsset == null)
                        mainAsset = asset;
                    entry.address = LocalizationEditorSettings.Instance.FindUniqueAssetAddress(mainAsset.name);
                }
                else
                {
                    if (createUndo)
                        Undo.RecordObject(entry.parentGroup, "Add asset to table");
                    entry.SetLabel(entryLabel, true, true);
                    UpdateAssetGroup(aaSettings, entry, createUndo);
                }

                if (createUndo)
                    Undo.RecordObjects(new Object[] { table, table.SharedData }, "Add asset to table");

                EditorUtility.SetDirty(table);
                EditorUtility.SetDirty(table.SharedData);

                if (AssetDatabase.IsSubAsset(asset))
                    assetGuid = AssetAddress.FormatAddress(assetGuid, asset.name);

                tableEntry = table.AddEntryFromReference(entryReference, assetGuid);
                SetEntryAssetType(tableEntry.KeyId, asset.GetType(), table.LocaleIdentifier.Code);
                LocalizationEditorSettings.EditorEvents.RaiseAssetTableEntryAdded(this, table, tableEntry);
            }
        }

        internal virtual bool IsPersistentAsset(Object asset) => EditorUtility.IsPersistent(asset);

        /// <summary>
        /// Remove the asset mapping from the table entry and also cleans up the Addressables if necessary.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="entryReference"></param>
        /// <param name="createUndo"></param>
        public void RemoveAssetFromTable(AssetTable table, TableEntryReference entryReference, bool createUndo = false)
        {
            using (new UndoScope("Remove asset from table", createUndo))
            {
                // Clear the asset but keep the key
                var tableEntry = table.GetEntryFromReference(entryReference);
                if (tableEntry == null)
                    return;

                var removedAssetGuid = tableEntry.Guid;
                tableEntry.Address = string.Empty;

                var aaSettings = LocalizationEditorSettings.Instance.GetAddressableAssetSettings(false);
                if (aaSettings == null)
                    return;

                EditorUtility.SetDirty(table);
                EditorUtility.SetDirty(table.SharedData);

                RemoveEntryAssetType(tableEntry.KeyId, table.LocaleIdentifier.Code);

                // If the entry has metadata then we will leave an empty entry otherwise we just remove the whole thing.
                if (tableEntry.MetadataEntries.Count == 0 && !string.IsNullOrEmpty(removedAssetGuid))
                    table.RemoveEntry(tableEntry.KeyId);

                // Determine if the asset is being referenced by any entries or tables with the same locale, if not then we can
                // remove the locale label and if no other labels exist also remove the asset from the Addressables system.
                var assetTableCollections = LocalizationEditorSettings.GetAssetTableCollections();
                foreach (var collection in assetTableCollections)
                {
                    if (collection.GetTable(table.LocaleIdentifier) is AssetTable tableWithMatchingLocaleId && tableWithMatchingLocaleId.ContainsValue(removedAssetGuid))
                    {
                        // The asset is referenced elsewhere by a table with the same Locale so we can not remove the locale label or asset.
                        return;
                    }
                }

                // Remove the locale label for this asset
                var assetEntry = aaSettings.FindAssetEntry(removedAssetGuid);
                if (assetEntry != null)
                {
                    if (createUndo)
                        Undo.RecordObject(assetEntry.parentGroup, "Remove asset from table");

                    var assetLabel = AddressHelper.FormatAssetLabel(table.LocaleIdentifier);
                    assetEntry.SetLabel(assetLabel, false);
                    UpdateAssetGroup(aaSettings, assetEntry, createUndo);
                }

                LocalizationEditorSettings.EditorEvents.RaiseAssetTableEntryRemoved(this, table, tableEntry, removedAssetGuid);
            }
        }

        /// <summary>
        /// Returns the expected type for the entry.
        /// When an asset is first added to an entry, the type is recorded so that the Editor can ensure all subsequent assets that are added are compatible.
        /// </summary>
        /// <param name="tableEntry">The entry to return the asset type for.</param>
        public Type GetEntryAssetType(TableEntryReference tableEntry)
        {
            long keyId = tableEntry.ReferenceType == TableEntryReference.Type.Name ? SharedData.GetId(tableEntry.Key) : tableEntry.KeyId;

            foreach (var metadata in SharedData.Metadata.MetadataEntries)
            {
                if (metadata is AssetTypeMetadata assetType && assetType.Contains(keyId))
                {
                    return assetType.Type;
                }
            }
            return typeof(Object);
        }

        /// <summary>
        /// The type of asset that is expected by this entry. By default this is determined by the first asset that is added in the Editor
        /// however this can be used to override it so it always expects this asset type instead of reverting back to Object when the last asset is removed.
        /// </summary>
        /// <param name="tableEntry">The entry to set the asset type for.</param>
        /// <param name="assetType">The asset type to expect for this entry. To reset the override and allow the Editor to control the type pass <see langword="null"/> or <c>typeof(Object)</c>.</param>
        public void SetEntryAssetType(TableEntryReference tableEntry, Type assetType)
        {
            if (assetType == null || assetType == typeof(Object))
                RemoveEntryAssetType(tableEntry, k_AssetTypeSetByScript);
            else
                SetEntryAssetType(tableEntry, assetType, k_AssetTypeSetByScript);
        }

        void SetEntryAssetType(TableEntryReference tableEntry, Type assetType, string entryCode)
        {
            long keyId = tableEntry.ReferenceType == TableEntryReference.Type.Name ? SharedData.GetId(tableEntry.Key, true) : tableEntry.KeyId;

            // Update type metadata
            AssetTypeMetadata entryMetadata = null;
            AssetTypeMetadata typeMetadata = null;

            // We cant use a foreach here as we are sometimes inside of a loop and exceptions will be thrown (Collection was modified).
            for (int i = 0; i < SharedData.Metadata.MetadataEntries.Count; ++i)
            {
                var md = SharedData.Metadata.MetadataEntries[i];
                if (md is AssetTypeMetadata at)
                {
                    if (at.Contains(keyId))
                    {
                        if (!at.Type.IsAssignableFrom(assetType))
                        {
                            at.RemoveEntry(keyId, entryCode);
                            if (at.IsEmpty)
                            {
                                SharedData.Metadata.RemoveMetadata(at);
                            }

                            // Are other tables still using the type for the same id?
                            if (at.Contains(tableEntry.KeyId))
                            {
                                var name = SharedData.GetEntry(tableEntry.KeyId);
                                Debug.LogWarning($"Table entry {name}({tableEntry.KeyId}) contains mixed types. Both {at.Type} and {assetType} are used.");
                            }
                        }
                        else
                        {
                            entryMetadata = at;
                            break;
                        }
                    }

                    if (at.Type == assetType)
                    {
                        typeMetadata = at;
                        break;
                    }
                }
            }
            var foundMetadata = entryMetadata ?? typeMetadata;
            if (foundMetadata == null)
            {
                foundMetadata = new AssetTypeMetadata() { Type = assetType };
                SharedData.Metadata.AddMetadata(foundMetadata);
            }

            foundMetadata.AddEntry(keyId, entryCode);
        }

        void RemoveEntryAssetType(TableEntryReference tableEntry, string entryCode)
        {
            long keyId = tableEntry.ReferenceType == TableEntryReference.Type.Name ? SharedData.GetId(tableEntry.Key) : tableEntry.KeyId;
            if (keyId == SharedTableData.EmptyId)
                return;

            // Update type metadata
            // We cant use a foreach here as we are sometimes inside of a loop and exceptions will be thrown (Collection was modified).
            for (int i = 0; i < SharedData.Metadata.MetadataEntries.Count; ++i)
            {
                var md = SharedData.Metadata.MetadataEntries[i];
                if (md is AssetTypeMetadata at)
                {
                    if (at.Contains(keyId))
                    {
                        at.RemoveEntry(keyId, entryCode);
                        if (at.IsEmpty)
                        {
                            SharedData.Metadata.RemoveMetadata(at);
                        }
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Updates the group the asset should belong to.
        /// Assets will be stored in groups for the Locale they are used by unless they are used
        /// by more than 1 <see cref="Locale"/>, then they will be moved to the shared group.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="assetEntry"></param>
        /// <param name="createUndo">Used to indicate if an Undo operation should be created.</param>
        protected virtual void UpdateAssetGroup(AddressableAssetSettings settings, AddressableAssetEntry assetEntry, bool createUndo)
        {
            if (settings == null || assetEntry == null)
                return;

            // Find all the locales that are using the asset using the Addressable labels.
            var localesUsingAsset = ListPool<LocaleIdentifier>.Get();
            foreach (var label in assetEntry.labels)
            {
                if (AddressHelper.TryGetLocaleLabelToId(label, out var id))
                {
                    localesUsingAsset.Add(id);
                }
            }

            // If no Locales depend on this asset then we can just remove it
            if (localesUsingAsset.Count == 0)
            {
                var oldGroup = assetEntry.parentGroup;
                settings.RemoveAssetEntry(assetEntry.guid);
                if (oldGroup.entries.Count == 0)
                {
                    if (createUndo)
                    {
                        // We cant use undo asset deletion so we will leave an empty group instead of deleting it.
                        Undo.RecordObject(oldGroup, "Remove group");
                    }
                    else
                    {
                        settings.RemoveGroup(oldGroup);
                    }
                }

                ListPool<LocaleIdentifier>.Release(localesUsingAsset);
                return;
            }

            AddressableGroupRules.AddAssetToGroup(assetEntry.MainAsset, localesUsingAsset, settings, createUndo);
        }

        ///<inheritdoc/>
        public override void RemoveEntry(TableEntryReference entryReference)
        {
            var entry = SharedData.GetEntryFromReference(entryReference);
            if (entry == null)
                return;

            foreach (var table in AssetTables)
                table.RemoveEntry(entry.Id);
            SharedData.RemoveKey(entry.Key);

            LocalizationEditorSettings.EditorEvents.RaiseTableEntryRemoved(this, entry);
        }

        /// <inheritdoc/>
        public override void ClearAllEntries()
        {
            foreach (var table in AssetTables)
            {
                if (table == null)
                    continue;

                for (int i = 0; i < table.Values.Count; ++i)
                {
                    var tableEntry = table.Values.ElementAt(i);

                    // Remove the asset from Addressables
                    if (!tableEntry.IsEmpty)
                    {
                        RemoveAssetFromTable(table, tableEntry.KeyId);
                    }
                }

                table.Clear();
                EditorUtility.SetDirty(table);
            }
            base.ClearAllEntries();
        }
    }
}
