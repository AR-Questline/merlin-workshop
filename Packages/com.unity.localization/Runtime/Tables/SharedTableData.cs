using System;
using System.Collections.Generic;
using UnityEngine.Localization.Metadata;
using UnityEngine.Serialization;

namespace UnityEngine.Localization.Tables
{
    /// <summary>
    /// The SharedTableData holds data that is accessible across all tables.
    /// It is responsible for the localization keys and associating the keys to unique ids.
    /// Each collection of tables will reference a single SharedTableData asset.
    /// </summary>
    public class SharedTableData : ScriptableObject, ISerializationCallbackReceiver
    {
        /// <summary>
        /// A entry in the SharedTableData. Contains the unique id, the name of the key and optional Metadata.
        /// </summary>
        [Serializable]
        public class SharedTableEntry
        {
            [SerializeField]
            long m_Id;

            [SerializeField]
            string m_Key;

            [SerializeField]
            MetadataCollection m_Metadata = new MetadataCollection();

            /// <summary>
            /// Unique id(to this SharedTableData).
            /// </summary>
            public long Id
            {
                get => m_Id;
                internal set => m_Id = value;
            }

            /// <summary>
            /// The name of the key, must also be unique.
            /// </summary>
            public string Key
            {
                get => m_Key;
                internal set => m_Key = value;
            }

            /// <summary>
            /// Optional Metadata for this key that is also shared between all tables that use this <see cref="SharedTableData"/>.
            /// </summary>
            public MetadataCollection Metadata
            {
                get => m_Metadata;
                set => m_Metadata = value;
            }

            public override string ToString() => $"{Id} - {Key}";
        }

        /// <summary>
        /// Represents an empty or null Key Id.
        /// </summary>
        public const long EmptyId = 0;

        internal const string NewEntryKey = "New Entry";

        [FormerlySerializedAs("m_TableName")]
        [SerializeField]
        string m_TableCollectionName;

        [FormerlySerializedAs("m_TableNameGuidString")]
        [SerializeField]
        string m_TableCollectionNameGuidString;

        [SerializeField]
        List<SharedTableEntry> m_Entries = new List<SharedTableEntry>();

        [SerializeField]
        [MetadataType(MetadataType.SharedTableData)]
        MetadataCollection m_Metadata = new MetadataCollection();

        [SerializeReference]
        IKeyGenerator m_KeyGenerator = new DistributedUIDGenerator();

        Guid m_TableCollectionNameGuid;

        // Used for fast lookup. Only generated when required.
        Dictionary<long, SharedTableEntry> m_IdDictionary = new Dictionary<long, SharedTableEntry>();
        Dictionary<string, SharedTableEntry> m_KeyDictionary = new Dictionary<string, SharedTableEntry>();

        /// <summary>
        /// All entries.
        /// </summary>
        public List<SharedTableEntry> Entries
        {
            get => m_Entries;
            set
            {
                m_Entries = value;
                m_IdDictionary.Clear();
                m_KeyDictionary.Clear();
            }
        }

        /// <summary>
        /// Clear all entries in this table.
        /// </summary>
        public void Clear()
        {
            m_Entries.Clear();
            m_IdDictionary.Clear();
            m_KeyDictionary.Clear();
        }

        /// <summary>
        /// The name of this table collection.
        /// All <see cref="LocalizationTable"/> that use this SharedTableData will have this name.
        /// </summary>
        public string TableCollectionName
        {
            get => m_TableCollectionName;
            set => m_TableCollectionName = value;
        }

        /// <summary>
        /// A unique Id that will never change. Comes from the SharedTableData asset Guid.
        /// Provides a way to reference a table that will not be broken if the table collection name was to be changed.
        /// </summary>
        public Guid TableCollectionNameGuid
        {
            get => m_TableCollectionNameGuid;
            internal set => m_TableCollectionNameGuid = value;
        }

        /// <summary>
        /// Metadata that is shared between all tables.
        /// </summary>
        public MetadataCollection Metadata
        {
            get => m_Metadata;
            set => m_Metadata = value;
        }

        /// <summary>
        /// The Key Generator to use when adding new entries.
        /// By default this will use <see cref="DistributedUIDGenerator"/>.
        /// </summary>
        /// <example>
        /// This example shows how the <see cref="KeyGenerator"/> could be configured to use a <see cref="SequentialIDGenerator"/>.
        /// <code source="../../DocCodeSamples.Tests/ChangeKeyGenerator.cs"/>
        /// </example>
        public IKeyGenerator KeyGenerator
        {
            get => m_KeyGenerator;
            set => m_KeyGenerator = value;
        }

        /// <summary>
        /// Get the key associated with the id.
        /// </summary>
        /// <param name="id">Id the key belongs to.</param>
        /// <returns>The found key or null if one can not be found.</returns>
        public string GetKey(long id)
        {
            var foundPair = FindWithId(id);
            return foundPair?.Key;
        }

        /// <summary>
        /// Get the unique Id for the key name from the shared table data.
        /// </summary>
        /// <param name="key">The key whose id is being requested.</param>
        /// <returns>The keys id value or <see cref="EmptyId"/> if one does not exist.</returns>
        public long GetId(string key)
        {
            var foundPair = FindWithKey(key);
            return foundPair?.Id ?? 0;
        }

        /// <summary>
        /// Get the unique Id for the key name, if one does not exist then a new entry is added.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="addNewKey"></param>
        /// <returns>Id of the Key that exists else the Id of the newly created Key if addNewKey is True or EmptyId.</returns>
        public long GetId(string key, bool addNewKey)
        {
            var foundPair = FindWithKey(key);
            var foundId = EmptyId;
            if (foundPair != null)
            {
                foundId = foundPair.Id;
            }
            else if (addNewKey)
            {
                foundId = AddKeyInternal(key).Id;
            }

            return foundId;
        }

        /// <summary>
        /// Returns the Entry for the key id, this contains all data for the key.
        /// </summary>
        /// <param name="tableEntryReference">Reference to the entry.</param>
        /// <returns>The found key entry or null if one can not be found.</returns>
        public SharedTableEntry GetEntryFromReference(TableEntryReference tableEntryReference)
        {
            if (tableEntryReference.ReferenceType == TableEntryReference.Type.Name)
                return GetEntry(tableEntryReference.Key);
            return GetEntry(tableEntryReference.KeyId);
        }

        /// <summary>
        /// Returns the Entry for the key id, this contains all data for the key.
        /// </summary>
        /// <param name="id">Id the key belongs to.</param>
        /// <returns>The found key entry or null if one can not be found.</returns>
        public SharedTableEntry GetEntry(long id)
        {
            return FindWithId(id);
        }

        /// <summary>
        /// Returns the Entry for the key, this contains all data for the key.
        /// </summary>
        /// <param name="key">The name of the key.</param>
        /// <returns>The found key entry or null if one can not be found.</returns>
        public SharedTableEntry GetEntry(string key)
        {
            return FindWithKey(key);
        }

        /// <summary>
        /// Is the Id value used by any entries in this SharedTableData?
        /// </summary>
        /// <param name="id">Id to check.</param>
        /// <returns></returns>
        public bool Contains(long id) => FindWithId(id) != null;

        /// <summary>
        /// Is the key value used by any entries in this SharedTableData?
        /// </summary>
        /// <param name="key">Key to check.</param>
        /// <returns></returns>
        public bool Contains(string key) => FindWithKey(key) != null;

        /// <summary>
        /// Adds a new key to this SharedTableData if one does not already exists with the same id.
        /// </summary>
        /// <param name="key">The unique key name to assign to the entry.</param>
        /// <param name="id">The unique id to assign to the key.</param>
        /// <returns>The new entry or null if an entry already exists with the same id.</returns>
        public SharedTableEntry AddKey(string key, long id)
        {
            return !Contains(id) ? AddKeyInternal(key, id) : null;
        }

        /// <summary>
        /// Adds a new key to this SharedTableData with a default name. If the name already exists then a unique version is generated based on the provided name.
        /// </summary>
        /// <param name="key">The name for the new Key.</param>
        /// <returns></returns>
        public SharedTableEntry AddKey(string key = null)
        {
            var newKeyName = string.IsNullOrEmpty(key) ? NewEntryKey : key;
            SharedTableEntry entry = null;
            int counter = 1;
            var keyToTry = newKeyName;
            while (entry == null)
            {
                if (Contains(keyToTry))
                {
                    keyToTry = $"{newKeyName} {counter++}";
                }
                else
                {
                    entry = AddKeyInternal(keyToTry);
                }
            }
            return entry;
        }

        /// <summary>
        /// Attempts to remove the key with provided id.
        /// </summary>
        /// <param name="id"></param>
        public void RemoveKey(long id)
        {
            var foundEntry = FindWithId(id);
            if (foundEntry != null)
                RemoveKeyInternal(foundEntry);
        }

        /// <summary>
        /// Attempts to remove the key from this SharedTableData.
        /// </summary>
        /// <param name="key">The key to be removed.</param>
        public void RemoveKey(string key)
        {
            var foundEntry = FindWithKey(key);
            if (foundEntry != null)
                RemoveKeyInternal(foundEntry);
        }

        /// <summary>
        /// Rename the key value for the provided id if it exists.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="newValue"></param>
        public void RenameKey(long id, string newValue)
        {
            var foundEntry = FindWithId(id);
            if (foundEntry != null)
                RenameKeyInternal(foundEntry, newValue);
        }

        /// <summary>
        /// Rename the key value if it exists.
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        public void RenameKey(string oldValue, string newValue)
        {
            var foundEntry = FindWithKey(oldValue);
            if (foundEntry != null)
                RenameKeyInternal(foundEntry, newValue);
        }

        /// <summary>
        /// Attempts to change the Id of an entry.
        /// </summary>
        /// <param name="currentId">The current Id that should be changed. Must exist.</param>
        /// <param name="newId">The new Id to use. Must not already be in use.</param>
        /// <returns>True is the Id was changed successfully.</returns>
        public bool RemapId(long currentId, long newId)
        {
            // Is the new id in use?
            if (FindWithId(newId) != null)
                return false;

            var foundEntry = FindWithId(currentId);
            if (foundEntry == null)
                return false;

            foundEntry.Id = newId;
            m_IdDictionary.Remove(currentId);
            m_IdDictionary[newId] = foundEntry;
            return true;
        }

        /// <summary>
        /// Returns the <see cref="SharedTableEntry"/> that is the most similar to the text.
        /// Uses the Levenshtein distance method.
        /// </summary>
        /// <param name="text">The text to match against.</param>
        /// <param name="distance">The number of edits needed to turn <paramref name="text"/> into the returned <see cref="SharedTableEntry"/>, 0 being an exact match.</param>
        /// <returns>The <see cref="SharedTableEntry"/> that is the most similar to the text or null if one could not be found.</returns>
        [Obsolete("FindSimilarKey will be removed in the future, please use Unity Search. See TableEntrySearchData class for further details.")]
        public SharedTableEntry FindSimilarKey(string text, out int distance)
        {
            SharedTableEntry foundEntry = null;
            distance = int.MaxValue;
            foreach (var entry in Entries)
            {
                var d = ComputeLevenshteinDistance(text.ToLower(), entry.Key.ToLower());
                if (d < distance)
                {
                    foundEntry = entry;
                    distance = d;
                }
            }

            return foundEntry;
        }

        #pragma warning disable CA1814 // CA1814 Prefer jagged arrays over multidimensional

        /// <summary>
        /// Compute the distance between two strings.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>The number of edits needed to turn one string into another.</returns>
        static int ComputeLevenshteinDistance(string a, string b)
        {
            // Based on https://www.dotnetperls.com/levenshtein
            int n = a.Length;
            int m = b.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
                return m;

            if (m == 0)
                return n;

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++) {}

            for (int j = 0; j <= m; d[0, j] = j++) {}

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (b[j - 1] == a[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Mathf.Min(Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }

            // Step 7
            return d[n, m];
        }

        #pragma warning restore CA1814

        SharedTableEntry AddKeyInternal(string key)
        {
            var newEntry = new SharedTableEntry() { Id = m_KeyGenerator.GetNextKey(), Key = key };

            // Its possible that the Id already exists, such as when a custom entry was added by a user.
            // We need to to make sure this value is unique, if it is not then we keep trying until we find a unique Id.
            while (FindWithId(newEntry.Id) != null)
            {
                newEntry.Id = m_KeyGenerator.GetNextKey();
            }

            Entries.Add(newEntry);

            if (m_IdDictionary.Count > 0)
                m_IdDictionary[newEntry.Id] = newEntry;
            if (m_KeyDictionary.Count > 0)
                m_KeyDictionary[key] = newEntry;

            return newEntry;
        }

        SharedTableEntry AddKeyInternal(string key, long id)
        {
            var newEntry = new SharedTableEntry() { Id = id, Key = key };
            Entries.Add(newEntry);

            if (m_IdDictionary.Count > 0)
                m_IdDictionary[newEntry.Id] = newEntry;
            if (m_KeyDictionary.Count > 0)
                m_KeyDictionary[key] = newEntry;

            return newEntry;
        }

        void RenameKeyInternal(SharedTableEntry entry, string newValue)
        {
            if (m_KeyDictionary.Count > 0)
            {
                m_KeyDictionary.Remove(entry.Key);
                m_KeyDictionary[newValue] = entry;
            }

            entry.Key = newValue;
        }

        void RemoveKeyInternal(SharedTableEntry entry)
        {
            if (m_KeyDictionary.Count > 0)
                m_KeyDictionary.Remove(entry.Key);

            if (m_IdDictionary.Count > 0)
                m_IdDictionary.Remove(entry.Id);

            Entries.Remove(entry);
        }

        SharedTableEntry FindWithId(long id)
        {
            if (id == EmptyId)
                return null;

            if (m_IdDictionary.Count == 0)
            {
                foreach (var keyAndIdPair in m_Entries)
                {
                    m_IdDictionary[keyAndIdPair.Id] = keyAndIdPair;
                }
            }

            m_IdDictionary.TryGetValue(id, out var foundPair);
            return foundPair;
        }

        SharedTableEntry FindWithKey(string key)
        {
            if (m_KeyDictionary.Count == 0)
            {
                foreach (var keyAndIdPair in m_Entries)
                {
                    m_KeyDictionary[keyAndIdPair.Key] = keyAndIdPair;
                }
            }

            m_KeyDictionary.TryGetValue(key, out var foundPair);
            return foundPair;
        }

        public override string ToString() => $"{TableCollectionName}(Shared Table Data)";

        /// <summary>
        /// Converts the Guid into a serializable string.
        /// </summary>
        public void OnBeforeSerialize()
        {
            m_TableCollectionNameGuidString = TableReference.StringFromGuid(m_TableCollectionNameGuid);
        }

        /// <summary>
        /// Converts the serializable string into a Guid.
        /// </summary>
        public void OnAfterDeserialize()
        {
            m_IdDictionary.Clear();
            m_KeyDictionary.Clear();

            m_TableCollectionNameGuid = string.IsNullOrEmpty(m_TableCollectionNameGuidString) ? Guid.Empty : Guid.Parse(m_TableCollectionNameGuidString);

            #if UNITY_EDITOR
            // If in the editor we can try and repair missing GUID issues.
            if (m_TableCollectionNameGuid == Guid.Empty)
            {
                // We have to defer as we can not use the asset database whilst inside of OnAfterDeserialize.
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null && UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(this, out string guid, out long _))
                    {
                        m_TableCollectionNameGuid = Guid.Parse(guid);
                        UnityEditor.EditorUtility.SetDirty(this);
                    }
                };
            }
            #endif
        }
    }
}
