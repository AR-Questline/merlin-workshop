using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.Plugins.Google.Columns
{
    /// <summary>
    /// Provides a column that can be used to synchronize <see cref="IMetadata"/> for a particular <see cref="Locale"/>.
    /// </summary>
    /// <typeparam name="TMetadata"></typeparam>
    /// <example>
    /// The following example demonstrates how to populate custom metadata into a column.
    /// <code source="../../../../DocCodeSamples.Tests/GoogleSheetsSamples.cs" region="locale-metadata-column"/>
    /// </example>
    /// <example>
    /// This is an example of how to synchronize the <see cref="StringTableEntry.IsSmart"/> property.
    /// Any value in the column causes the value to be marked as smart; leaving the field empty indicates it should not be smart.
    /// <code source="../../../../DocCodeSamples.Tests/GoogleSheetsSamples.cs" region="smart-string-column"/>
    /// </example>
    [Serializable]
    public abstract class LocaleMetadataColumn<TMetadata> : SheetColumn where TMetadata : IMetadata
    {
        [SerializeField]
        LocaleIdentifier m_LocaleIdentifier;

        int m_PushCollectionTableIndex = -1;
        StringTable m_PullTable;

        /// <summary>
        /// The Id of the Locale for this column.
        /// </summary>
        public LocaleIdentifier LocaleIdentifier
        {
            get => m_LocaleIdentifier;
            set => m_LocaleIdentifier = value;
        }

        /// <inheritdoc/>
        public override void PushBegin(StringTableCollection collection)
        {
            var tables = collection.StringTables;
            for (int i = 0; i < tables.Count; ++i)
            {
                if (tables[i].LocaleIdentifier == LocaleIdentifier)
                {
                    m_PushCollectionTableIndex = i;
                    return;
                }
            }
            m_PushCollectionTableIndex = -1;
        }

        /// <inheritdoc/>
        public abstract void PushMetadata(TMetadata metadata, out string value, out string note);

        /// <inheritdoc/>
        public override void PushCellData(SharedTableData.SharedTableEntry keyEntry, IList<StringTableEntry> tableEntries, out string value, out string note)
        {
            if (m_PushCollectionTableIndex != -1 && tableEntries[m_PushCollectionTableIndex] != null)
            {
                var entry = tableEntries[m_PushCollectionTableIndex];
                var metadata = entry.GetMetadata<TMetadata>();
                if (metadata != null)
                {
                    PushMetadata(metadata, out value, out note);
                    return;
                }
            }
            value = null;
            note = null;
        }

        /// <inheritdoc/>
        public override void PushEnd()
        {
            m_PushCollectionTableIndex = -1;
        }

        /// <inheritdoc/>
        public override void PullBegin(StringTableCollection collection)
        {
            var tables = collection.StringTables;
            for (int i = 0; i < tables.Count; ++i)
            {
                if (tables[i].LocaleIdentifier == LocaleIdentifier)
                {
                    m_PullTable = tables[i];
                    return;
                }
            }
            m_PullTable = null;
        }

        /// <inheritdoc/>
        public abstract void PullMetadata(StringTableEntry entry, TMetadata metadata, string cellValue, string cellNote);

        /// <inheritdoc/>
        public override void PullCellData(SharedTableData.SharedTableEntry keyEntry, string cellValue, string cellNote)
        {
            if (m_PullTable == null)
                return;

            var entry = m_PullTable.GetEntry(keyEntry.Id) ?? m_PullTable.AddEntry(keyEntry.Id, string.Empty);
            var metadata = entry.GetMetadata<TMetadata>();
            PullMetadata(entry, metadata, cellValue, cellNote);
        }

        /// <inheritdoc/>
        public override void PullEnd()
        {
            m_PullTable = null;
        }
    }
}
