using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Localization.Metadata;

namespace UnityEngine.Localization.Tables
{
    /// <summary>
    /// Player version of a table entry that can contain additional data that is not serialized.
    /// </summary>
    public class TableEntry : IMetadataCollection
    {
        SharedTableData.SharedTableEntry m_SharedTableEntry;

        /// <summary>
        /// The table that this entry is part of.
        /// </summary>
        public LocalizationTable Table { get; internal set; }

        /// <summary>
        /// The serialized data
        /// </summary>
        internal TableEntryData Data { get; set; }

        /// <summary>
        /// The shared table entry contains information for all locales, this is taken from <see cref="SharedTableData"/>.
        /// </summary>
        public SharedTableData.SharedTableEntry SharedEntry
        {
            get
            {
                if (m_SharedTableEntry == null)
                {
                    Assertions.Assert.IsNotNull(Table);
                    m_SharedTableEntry = Table.SharedData.GetEntry(KeyId);
                }
                return m_SharedTableEntry;
            }
        }

        /// <summary>
        /// The Key or Name of this table entry that is stored in <see cref="SharedEntry"/>.
        /// </summary>
        public string Key
        {
            get => SharedEntry?.Key;
            set => Table.SharedData.RenameKey(KeyId, value);
        }

        /// <summary>
        /// Key Id for this table entry.
        /// </summary>
        public long KeyId => Data.Id;

        /// <summary>
        /// Raw localized value.
        /// </summary>
        public string LocalizedValue => Data.Localized;

        /// <summary>
        /// The Metadata for this table entry.
        /// </summary>
        public IList<IMetadata> MetadataEntries => Data.Metadata.MetadataEntries;

        /// <summary>
        /// Returns the first Metadata item from <see cref="MetadataEntries"/> of type TObject.
        /// </summary>
        /// <typeparam name="TObject"></typeparam>
        /// <returns></returns>
        public TObject GetMetadata<TObject>() where TObject : IMetadata
        {
            return Data.Metadata.GetMetadata<TObject>();
        }

        /// <summary>
        /// Populates the list with all Metadata from <see cref="MetadataEntries"/> that is of type TObject.
        /// </summary>
        /// <typeparam name="TObject"></typeparam>
        /// <param name="foundItems"></param>
        public void GetMetadatas<TObject>(IList<TObject> foundItems) where TObject : IMetadata
        {
            Data.Metadata.GetMetadatas(foundItems);
        }

        /// <summary>
        /// Returns all Metadata from <see cref="MetadataEntries"/> that is of type TObject.
        /// </summary>
        /// <typeparam name="TObject"></typeparam>
        /// <returns></returns>
        public IList<TObject> GetMetadatas<TObject>() where TObject : IMetadata
        {
            return Data.Metadata.GetMetadatas<TObject>();
        }

        /// <summary>
        /// Returns true if any tag metadata of type TShared contains this entry.
        /// </summary>
        /// <typeparam name="TShared"></typeparam>
        /// <returns></returns>
        public bool HasTagMetadata<TShared>() where TShared : SharedTableEntryMetadata
        {
            var tag = Table.GetMetadata<TShared>();
            return tag?.IsRegistered(this) == true;
        }

        /// <summary>
        /// Tags are Metadata that can be shared across multiple table entries,
        /// they are often used to indicate an entry has a particular attribute or feature, e.g SmartFormat.
        /// Generally Tags do not contains data, for sharing data across multiple table entries see <see cref="AddSharedMetadata"/>.
        /// A Tag reference will be stored in <see cref="LocalizationTable.TableData"/> and <see cref="MetadataEntries"/>.
        /// </summary>
        /// <typeparam name="TShared"></typeparam>
        public void AddTagMetadata<TShared>() where TShared : SharedTableEntryMetadata, new()
        {
            TShared tag = null;
            foreach (var md in Table.MetadataEntries)
            {
                if (md is TShared shared)
                {
                    tag = shared;

                    // If we already have the tag then there is nothing we need to do. (LOC-779)
                    if (tag.IsRegistered(this))
                        return;

                    break;
                }
            }

            if (tag == null)
            {
                tag = new TShared();
                Table.AddMetadata(tag);
            }

            tag.Register(this);
            AddMetadata(tag);
        }

        /// <summary>
        /// SharedTableEntryMetadata is Metadata that can be shared across multiple entries in a single table.
        /// The instance reference will be stored in <see cref="LocalizationTable.TableData"/> and <see cref="MetadataEntries"/>.
        /// </summary>
        /// <param name="md"></param>
        public void AddSharedMetadata(SharedTableEntryMetadata md)
        {
            if (!Table.Contains(md))
            {
                Table.AddMetadata(md);
            }

            // If we already have the tag then there is nothing we need to do.
            if (md.IsRegistered(this))
                return;

            md.Register(this);
            AddMetadata(md);
        }

        /// <summary>
        /// SharedTableCollectionMetadata is Metadata that can be applied to multiple table entries in a table collection.
        /// The Metadata is stored in the <see cref="SharedTableData"/>.
        /// </summary>
        /// <param name="md"></param>
        public void AddSharedMetadata(SharedTableCollectionMetadata md)
        {
            if (!Table.SharedData.Metadata.Contains(md))
            {
                Table.SharedData.Metadata.AddMetadata(md);
            }

            md.AddEntry(Data.Id, Table.LocaleIdentifier.Code);
        }

        /// <summary>
        /// Add an entry to <see cref="MetadataEntries"/>.
        /// </summary>
        /// <param name="md"></param>
        public void AddMetadata(IMetadata md)
        {
            Data.Metadata.AddMetadata(md);
        }

        /// <summary>
        /// Removes the Metadata tag from this entry and the table if it is no longer used by any other table entries.
        /// </summary>
        /// <typeparam name="TShared"></typeparam>
        public void RemoveTagMetadata<TShared>() where TShared : SharedTableEntryMetadata
        {
            var tableMetada = Table.MetadataEntries;
            var entryMetadata = Data.Metadata.MetadataEntries;

            // We check both the entry and table metadata as we had some bugs in the past that caused them to go out of sync. (LOC-779)

            // Check entry
            for (int i = entryMetadata.Count - 1; i >= 0; --i)
            {
                if (entryMetadata[i] is TShared tag)
                {
                    tag.Unregister(this);
                    entryMetadata.RemoveAt(i);
                }
            }

            // Check table 
            for (int i = tableMetada.Count - 1; i >= 0; --i)
            {
                if (tableMetada[i] is TShared tag)
                {
                    tag.Unregister(this);

                    // Remove the shared data if it is no longer used
                    if (tag.Count == 0)
                    {
                        tableMetada.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// Removes the entry from the shared Metadata in the table and removes the
        /// shared Metadata if no other entries are using it.
        /// </summary>
        /// <param name="md"></param>
        public void RemoveSharedMetadata(SharedTableEntryMetadata md)
        {
            md.Unregister(this);
            RemoveMetadata(md);

            // Remove the shared data if it is no longer used
            if (md.Count == 0 && Table.Contains(md))
            {
                Table.RemoveMetadata(md);
            }
        }

        /// <summary>
        /// Removes the entry from the Shared Metadata and removes it from the
        /// <see cref="SharedTableData"/> if no other entries are using it.
        /// </summary>
        /// <param name="md"></param>
        public void RemoveSharedMetadata(SharedTableCollectionMetadata md)
        {
            md.RemoveEntry(Data.Id, Table.LocaleIdentifier.Code);

            if (md.IsEmpty)
            {
                Table.SharedData.Metadata.RemoveMetadata(md);
            }
        }

        /// <summary>
        /// Remove an entry from <see cref="MetadataEntries"/>.
        /// </summary>
        /// <param name="md"></param>
        /// <returns></returns>
        public bool RemoveMetadata(IMetadata md)
        {
            return Data.Metadata.RemoveMetadata(md);
        }

        /// <summary>
        /// Checks if the Metadata is contained within <see cref="MetadataEntries"/>.
        /// </summary>
        /// <param name="md"></param>
        /// <returns></returns>
        public bool Contains(IMetadata md)
        {
            return Data.Metadata.Contains(md);
        }

        public override string ToString() => $"{KeyId} - {LocalizedValue}";
    };

    /// <summary>
    /// Options for how to handle missing entries when using <see cref="DetailedLocalizationTable{TEntry}.CheckForMissingSharedTableDataEntries(MissingEntryAction)"/>.
    /// </summary>
    public enum MissingEntryAction
    {
        /// <summary>
        /// Do nothing.
        /// </summary>
        Nothing,

        /// <summary>
        /// Add the missing entries to the <see cref="SharedTableData"/>.
        /// </summary>
        AddEntriesToSharedData,

        /// <summary>
        /// Remove the missing entries from the table.
        /// </summary>
        RemoveEntriesFromTable
    }

    /// <summary>
    /// Provides common functionality for both string and asset tables.
    /// </summary>
    /// <typeparam name="TEntry"></typeparam>
    public abstract class DetailedLocalizationTable<TEntry> : LocalizationTable, IDictionary<long, TEntry>, ISerializationCallbackReceiver where TEntry : TableEntry
    {
        Dictionary<long, TEntry> m_TableEntries = new Dictionary<long, TEntry>();

        ICollection<long> IDictionary<long, TEntry>.Keys => m_TableEntries.Keys;

        /// <summary>
        /// All values in this table.
        /// </summary>
        public ICollection<TEntry> Values => m_TableEntries.Values;

        /// <summary>
        /// The number of entries in this Table.
        /// </summary>
        public int Count => m_TableEntries.Count;

        /// <summary>
        /// Will always be false. Implemented because it is required by the System.Collections.IList interface.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Get/Set a value using the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TEntry this[long key]
        {
            get => m_TableEntries[key];
            set
            {
                if (key == SharedTableData.EmptyId)
                    throw new ArgumentException("Key Id value 0, is not valid. All Key Id's must be non-zero.");

                if (value.Table != this)
                    throw new ArgumentException("Table entry does not belong to this table. Table entries can not be shared across tables.");

                // Move the entry
                RemoveEntry(value.Data.Id);
                value.Data.Id = key;
                m_TableEntries[key] = value;
            }
        }

        /// <summary>
        /// Get/Set a value using the specified key name.
        /// </summary>
        /// <param name="keyName"></param>
        /// <returns></returns>
        public TEntry this[string keyName]
        {
            get => GetEntry(keyName);
            set
            {
                if (value.Table != this)
                    throw new ArgumentException("Table entry does not belong to this table. Table entries can not be shared across tables.");

                var key = FindKeyId(keyName, true);
                this[key] = value;
            }
        }

        /// <summary>
        /// Returns a new instance of TEntry.
        /// </summary>
        /// <returns></returns>
        public abstract TEntry CreateTableEntry();

        internal TEntry CreateTableEntry(TableEntryData data)
        {
            var entry = CreateTableEntry();
            entry.Data = data;
            return entry;
        }

        /// <inheritdoc/>
        public override void CreateEmpty(TableEntryReference entryReference)
        {
            AddEntryFromReference(entryReference, string.Empty);
        }

        /// <summary>
        /// Add or update an entry in the table.
        /// </summary>
        /// <param name="key">The name of the key.</param>
        /// <param name="localized">The localized item, a string for <see cref="StringTable"/> or asset guid for <see cref="AssetTable"/>.</param>
        /// <returns></returns>
        public TEntry AddEntry(string key, string localized)
        {
            var keyId = FindKeyId(key, true);
            return keyId == 0 ? null : AddEntry(keyId, localized);
        }

        /// <summary>
        /// Add or update an entry in the table.
        /// </summary>
        /// <param name="keyId">The unique key id.</param>
        /// <param name="localized">The localized item, a string for <see cref="StringTable"/> or asset guid for <see cref="AssetTable"/>.</param>
        /// <returns></returns>
        public virtual TEntry AddEntry(long keyId, string localized)
        {
            if (keyId == SharedTableData.EmptyId)
                throw new ArgumentException($"Key Id value {nameof(SharedTableData.EmptyId)}({SharedTableData.EmptyId}), is not valid. All Key Id's must be non-zero.", nameof(keyId));

            if (!m_TableEntries.TryGetValue(keyId, out var tableEntry))
            {
                tableEntry = CreateTableEntry();
                tableEntry.Data = new TableEntryData(keyId);
                m_TableEntries[keyId] = tableEntry;
            }

            tableEntry.Data.Localized = localized;
            return tableEntry;
        }

        /// <summary>
        /// Add or update an entry in the table.
        /// </summary>
        /// <param name="entryReference">The <see cref="TableEntryReference"/> containing a valid Key or Key Id.</param>
        /// <param name="localized">The localized item, a string for <see cref="StringTable"/> or asset guid for <see cref="AssetTable"/></param>
        /// <returns></returns>
        public TEntry AddEntryFromReference(TableEntryReference entryReference, string localized)
        {
            if (entryReference.ReferenceType == TableEntryReference.Type.Id)
                return AddEntry(entryReference.KeyId, localized);
            if (entryReference.ReferenceType == TableEntryReference.Type.Name)
                return AddEntry(entryReference.Key, localized);
            throw new ArgumentException($"{nameof(TableEntryReference)} should not be Empty", nameof(entryReference));
        }

        /// <summary>
        /// Remove an entry from the table if it exists.
        /// </summary>
        /// <param name="key">The name of the key.</param>
        /// <returns>True if the entry was found and removed.</returns>
        public bool RemoveEntry(string key)
        {
            var keyId = FindKeyId(key, false);
            return keyId != 0 && RemoveEntry(keyId);
        }

        /// <summary>
        /// Remove an entry from the table if it exists.
        /// </summary>
        /// <param name="keyId">The key id to remove.</param>
        /// <returns>True if the entry was found and removed.</returns>
        public virtual bool RemoveEntry(long keyId)
        {
            if (m_TableEntries.TryGetValue(keyId, out var item))
            {
                // We also need to remove any references to this entry in shared metadata.
                for (int i = 0; i < MetadataEntries.Count; ++i)
                {
                    var metadataEntry = MetadataEntries[i];
                    if (metadataEntry is SharedTableEntryMetadata sharedMetadata)
                    {
                        sharedMetadata.Unregister(item);

                        // Remove the shared data if it is no longer used
                        if (sharedMetadata.Count == 0)
                        {
                            MetadataEntries.RemoveAt(i);
                            i--;
                        }
                    }
                }

                for (int i = 0; i < SharedData?.Metadata.MetadataEntries.Count; i++)
                {
                    var metadata = SharedData.Metadata.MetadataEntries[i];
                    if (metadata is SharedTableCollectionMetadata sharedMetadata)
                    {
                        sharedMetadata.RemoveEntry(keyId, LocaleIdentifier.Code);

                        // Remove the shared data if it is no longer used
                        if (sharedMetadata.IsEmpty)
                        {
                            SharedData.Metadata.MetadataEntries.RemoveAt(i);
                            i--;
                        }
                    }
                }

                item.Data.Id = SharedTableData.EmptyId;
                item.Table = null;
                return m_TableEntries.Remove(keyId);
            }

            return false;
        }

        /// <summary>
        /// Returns the entry reference or null if one does not exist.
        /// </summary>
        /// <param name="entryReference"></param>
        /// <returns></returns>
        public TEntry GetEntryFromReference(TableEntryReference entryReference)
        {
            if (entryReference.ReferenceType == TableEntryReference.Type.Id)
                return GetEntry(entryReference.KeyId);
            else if (entryReference.ReferenceType == TableEntryReference.Type.Name)
                return GetEntry(entryReference.Key);
            return null;
        }

        /// <summary>
        /// Returns the entry for the key or null if one does not exist.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TEntry GetEntry(string key)
        {
            var keyId = FindKeyId(key, false);
            return keyId == 0 ? null : GetEntry(keyId);
        }

        /// <summary>
        /// Returns the entry for the key id or null if one does not exist.
        /// </summary>
        /// <param name="keyId"></param>
        /// <returns></returns>
        public virtual TEntry GetEntry(long keyId)
        {
            m_TableEntries.TryGetValue(keyId, out var tableEntry);
            return tableEntry;
        }

        /// <summary>
        /// Adds the entry with the specified keyId.
        /// </summary>
        /// <param name="keyId"></param>
        /// <param name="value"></param>
        public void Add(long keyId, TEntry value)
        {
            this[keyId] = value;
        }

        /// <summary>
        /// Adds the item value with the specified keyId.
        /// </summary>
        /// <param name="item"></param>
        public void Add(KeyValuePair<long, TEntry> item)
        {
            this[item.Key] = item.Value;
        }

        /// <summary>
        /// Returns true if the table contains an entry with the keyId.
        /// </summary>
        /// <param name="keyId"></param>
        /// <returns></returns>
        public bool ContainsKey(long keyId) => m_TableEntries.ContainsKey(keyId);

        /// <summary>
        /// Returns true if the table contains an entry with the same value.
        /// </summary>
        /// <param name="localized">The value to check for in all table entries.</param>
        /// <returns>True if a match was found else false.</returns>
        public bool ContainsValue(string localized)
        {
            foreach (var entry in m_TableEntries.Values)
            {
                if (entry.Data.Localized == localized)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if the table contains the item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(KeyValuePair<long, TEntry> item) => m_TableEntries.Contains(item);

        /// <summary>
        /// Remove the entry with the keyId.
        /// </summary>
        /// <param name="keyId"></param>
        /// <returns></returns>
        public bool Remove(long keyId) => RemoveEntry(keyId);

        /// <summary>
        /// Remove the item from the table if it exists.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(KeyValuePair<long, TEntry> item)
        {
            if (Contains(item))
            {
                RemoveEntry(item.Key);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Tables do not store the full information for an entry, instead they store just the Id of that entry which can then be referenced in <see cref="LocalizationTable.SharedData"/>.
        /// It is possible that something may have caused an entry to be in the Table but missing from <see cref="LocalizationTable.SharedData"/>.
        /// This will cause issues and often result in the entry being ignored. This will check for any entries that exist in the table but do not have an entry in <see cref="LocalizationTable.SharedData"/>.
        /// </summary>
        /// <param name="action">The action to take on the found missing entries.</param>
        /// <returns>The identified missing entries.</returns>
        public IList<TEntry> CheckForMissingSharedTableDataEntries(MissingEntryAction action = MissingEntryAction.Nothing)
        {
            // Find all entries that are missing from the Shared Table Data.
            var results = m_TableEntries.Where(e => !SharedData.Contains(e.Key)).Select(e => e.Value).ToArray();
            if (results.Length == 0)
                return results;

            if (action == MissingEntryAction.AddEntriesToSharedData)
            {
                for (int i = 0; i < results.Length; ++i)
                {
                    // Add a default key, then remap the id
                    var sharedEntry = SharedData.AddKey();
                    SharedData.RemapId(sharedEntry.Id, results[i].KeyId);
                }
            }
            else if (action == MissingEntryAction.RemoveEntriesFromTable)
            {
                for (int i = 0; i < results.Length; ++i)
                {
                    RemoveEntry(results[i].KeyId);
                }
            }

            return results;
        }

        /// <summary>
        /// Find the entry, if it exists in the table.
        /// </summary>
        /// <param name="keyId"></param>
        /// <param name="value"></param>
        /// <returns>True if the entry was found.</returns>
        public bool TryGetValue(long keyId, out TEntry value) => m_TableEntries.TryGetValue(keyId, out value);

        /// <summary>
        /// Clear all entries in this table.
        /// </summary>
        public void Clear()
        {
            TableData.Clear();
            m_TableEntries.Clear();
        }

        /// <summary>
        /// Copies the contents of the table into an array starting at the arrayIndex.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(KeyValuePair<long, TEntry>[] array, int arrayIndex)
        {
            foreach (var entry in m_TableEntries)
            {
                array[arrayIndex++] = entry;
            }
        }

        /// <summary>
        /// Return an enumerator for the entries in this table.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<long, TEntry>> GetEnumerator() => m_TableEntries.GetEnumerator();

        /// <summary>
        /// Return an enumerator for the entries in this table.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator() => m_TableEntries.GetEnumerator();

        /// <summary>
        /// Creates a string representation of the table as "{TableCollectionName}({LocaleIdentifier})".
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{TableCollectionName}({LocaleIdentifier})";

        /// <summary>
        /// Does nothing but required for <see cref="OnAfterDeserialize"/>.
        /// </summary>
        public void OnBeforeSerialize()
        {
            TableData.Clear();
            foreach (var entry in this)
            {
                // Sync the id
                entry.Value.Data.Id = entry.Key;

                TableData.Add(entry.Value.Data);
            }
        }

        /// <summary>
        /// Converts the serialized data into <see cref="m_TableEntries"/>.
        /// </summary>
        public void OnAfterDeserialize()
        {
            try
            {
                m_TableEntries = TableData.ToDictionary(o => o.Id, CreateTableEntry);
            }
            catch (Exception e)
            {
                var error = $"Error Deserializing Table Data \"{TableCollectionName}({LocaleIdentifier})\".\n{e.Message}\n{e.InnerException}";
                Debug.LogError(error, this);
            }
        }
    }
}
