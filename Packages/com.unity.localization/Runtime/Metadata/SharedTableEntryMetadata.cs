using System;
using System.Collections.Generic;
using UnityEngine.Localization.Tables;

namespace UnityEngine.Localization.Metadata
{
    /// <summary>
    /// Shared Metadata is data that is associated to one or more entries in a single <see cref="LocalizationTable"/>.
    /// While it is possible to share any metadata across multiple entries in a table, this will track the entries that
    /// are currently using it providing a quick way to get all the entries.
    /// </summary>
    public abstract class SharedTableEntryMetadata : IMetadata, ISerializationCallbackReceiver
    {
        // Deprecated in favor of m_SharedEntries
        [SerializeField]
        List<long> m_Entries;

        // Unity will serialize a list of primatives as binary, this makes it hard to diff and merge
        // so we force it to serialize as a list of structs which does not use binary serialization. (LOC-1095)
        [Serializable]
        struct Entry
        {
            public long id;
        }
        [SerializeField]
        List<Entry> m_SharedEntries = new List<Entry>();

        HashSet<long> m_EntriesLookup = new HashSet<long>();

        internal int Count => m_EntriesLookup.Count;

        internal bool IsRegistered(TableEntry entry)
        {
            return m_EntriesLookup.Contains(entry.Data.Id);
        }

        internal void Register(TableEntry entry)
        {
            m_EntriesLookup.Add(entry.Data.Id);
        }

        internal void Unregister(TableEntry entry)
        {
            m_EntriesLookup.Remove(entry.Data.Id);
        }

        /// <summary>
        /// Converts the internal hashset into a serializable list.
        /// </summary>
        public void OnBeforeSerialize()
        {
            m_Entries = null;

            m_SharedEntries.Clear();
            foreach (var e in m_EntriesLookup)
            {
                m_SharedEntries.Add(new Entry { id = e });
            }
        }

        /// <summary>
        /// Converts the serializable list into a hashset.
        /// </summary>
        public void OnAfterDeserialize()
        {
            if (m_EntriesLookup == null)
                m_EntriesLookup = new HashSet<long>();
            else
                m_EntriesLookup.Clear();

            // Use the old system
            if (m_Entries != null && m_Entries.Count > 0)
            {
                foreach (var e in m_Entries)
                {
                    m_EntriesLookup.Add(e);
                }
                m_Entries = null;
            }

            if (m_SharedEntries != null && m_SharedEntries.Count > 0)
            {
                foreach (var e in m_SharedEntries)
                {
                    m_EntriesLookup.Add(e.id);
                }
            }
        }
    }
}
