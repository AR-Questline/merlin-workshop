using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Tables;
using static UnityEngine.Localization.Tables.SharedTableData;

namespace UnityEditor.Localization
{
    /// <summary>
    /// Provides methods for managing multiple <see cref="StringTable"/> in the Editor.
    /// </summary>
    public class StringTableCollection : LocalizationTableCollection
    {
        /// <inheritdoc/>
        protected internal override Type TableType => typeof(StringTable);

        /// <inheritdoc/>
        protected internal override Type RequiredExtensionAttribute => typeof(StringTableCollectionExtensionAttribute);

        /// <inheritdoc/>
        protected internal override string DefaultGroupName => "String Table";

        /// <summary>
        /// A helper property which is the contents of <see cref="LocalizationTableCollection.Tables"/> loaded and cast to <see cref="StringTable"/>.
        /// </summary>
        public virtual ReadOnlyCollection<StringTable> StringTables => new ReadOnlyCollection<StringTable>(Tables.Select(t => t.asset as StringTable).ToList().AsReadOnly());

        /// <summary>
        /// Returns a string that contains all the unique characters that are used for all localized values in the tables that belong to the supplied <see cref="LocaleIdentifier"/>'s.
        /// This will also include Smart String entries but will only consider the <see cref="UnityEngine.Localization.SmartFormat.Core.Parsing.LiteralText"/> values,
        /// it will not consider <see cref="UnityEngine.Localization.SmartFormat.Core.Parsing.Placeholder"/> values.
        /// </summary>
        /// <param name="localeIdentifiers">The tables to be included.</param>
        /// <returns>All distinct characters or an empty string if no tables or entries.</returns>.
        public string GenerateCharacterSet(params LocaleIdentifier[] localeIdentifiers)
        {
            if (localeIdentifiers == null || localeIdentifiers.Length == 0)
                throw new ArgumentException(nameof(localeIdentifiers), "Must provide at least 1 LocaleIdentifier");

            var characters = ExtractLiteralCharacters(localeIdentifiers);
            var distinct = characters.Distinct().OrderBy(c => c);
            return string.Concat(distinct);
        }

        internal IEnumerable<char> ExtractLiteralCharacters(params LocaleIdentifier[] localeIdentifiers)
        {
            IEnumerable<char> e = "";
            foreach (var id in localeIdentifiers)
            {
                // Create an enumerator for all the tables and entries.
                var table = GetTable(id) as StringTable;
                if (table != null)
                    e = e.Concat(table.CollectLiteralCharacters());
            }
            return e;
        }

        /// <summary>
        /// Updates the collection entries with a <paramref name="sortedEntries"/> and optionally removes entries that are missing from the update.
        /// Used by various importers such as <see cref="Plugins.Google.GoogleSheets"/> and <see cref="Plugins.CSV.Csv"/>.
        /// </summary>
        /// <param name="entriesToKeep">The entries that should not be removed if <paramref name="removeMissingEntries"/> is <see langword="true"/>.</param>
        /// <param name="sortedEntries">The new sorted entries.</param>
        /// <param name="removedEntriesLog">Optional log for reporting what entries were removed.</param>
        /// <param name="removeMissingEntries">Should missing entries be removed? If <see langword="false"/> they will be placed at the end after the sorted entries.</param>
        internal void MergeUpdatedEntries(HashSet<long> entriesToKeep, List<SharedTableEntry> sortedEntries, StringBuilder removedEntriesLog, bool removeMissingEntries)
        {
            // We either remove missing entries or add them to the end.
            var stringTables = StringTables;

            removedEntriesLog.AppendLine("Removed missing entries:");
            for (int i = 0; i < SharedData.Entries.Count; ++i)
            {
                var entry = SharedData.Entries[i];
                if (entriesToKeep.Contains(entry.Id))
                    continue;

                if (!removeMissingEntries)
                {
                    // Missing entries that we want to keep go to the bottom of the list.
                    sortedEntries.Add(entry);
                }
                else if (entry.Metadata.HasMetadata<ExcludeEntryFromExport>())
                {
                    // Add back the entry which has ExcludeEntryFromExport Metadata.
                    sortedEntries.Insert(i, entry);
                }
                else
                {
                    removedEntriesLog.AppendLine($"  {entry}");

                    // Remove from tables
                    foreach (var table in stringTables)
                    {
                        table.Remove(entry.Id);
                    }
                }
            }

            // Now replace the old list with our new one that is in the correct order.
            SharedData.Entries = sortedEntries;
        }

        /// <summary>
        /// Returns an enumerator that can be used to step through each key and its localized values, such as in a foreach loop.
        /// Internally <see cref="SharedTableData"/> and <see cref="StringTable"/>'s are separate assets with their own internal list of values.
        /// This means that when iterating through each key a lookup must be made in each table in order to retrieve the localized value,
        /// this can become slow when dealing with a large number of tables and entries.
        /// GetRowEnumerator improves this process by first sorting the multiple internal lists and then stepping through each conceptual row at a time.
        /// It handles missing keys and table entries and provides a more efficient and faster way to iterate through the tables.
        /// </summary>
        /// <example>
        /// This example shows how a StringTableCollection could be exported as CSV.
        /// <code source="../../DocCodeSamples.Tests/TableCollectionSamples.cs" region="row-enumerator"/>
        /// </example>
        /// <returns></returns>
        public IEnumerable<Row<StringTableEntry>> GetRowEnumerator() => GetRowEnumerator<StringTable, StringTableEntry>(StringTables);

        /// <summary>
        /// Returns an enumerator that can be used to step through each key and its localized values, such as in a foreach loop.
        /// This version does not sort the items by the Key Id but instead returns them in the order of the <see cref="SharedTableData.Entries"/>.
        /// If the order of the rows is not important then using <see cref="GetRowEnumerator"/> will provide better performance.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Row<StringTableEntry>> GetRowEnumeratorUnsorted() => GetRowEnumeratorUnsorted<StringTable, StringTableEntry>(StringTables);

        /// <summary>
        /// <inheritdoc cref="GetRowEnumerator"/>
        /// </summary>
        /// <param name="tables"></param>
        /// <returns></returns>
        public static IEnumerable<Row<StringTableEntry>> GetRowEnumerator(params StringTable[] tables) => GetRowEnumerator<StringTable, StringTableEntry>(tables);

        ///<inheritdoc/>
        public override void RemoveEntry(TableEntryReference entryReference)
        {
            var entry = SharedData.GetEntryFromReference(entryReference);
            if (entry == null)
                return;

            foreach (var table in StringTables)
                table.RemoveEntry(entry.Id);
            SharedData.RemoveKey(entry.Key);

            LocalizationEditorSettings.EditorEvents.RaiseTableEntryRemoved(this, entry);
        }

        /// <inheritdoc/>
        public override void ClearAllEntries()
        {
            foreach (var table in StringTables)
            {
                if (table == null)
                    continue;

                table.Clear();
                EditorUtility.SetDirty(table);
            }

            base.ClearAllEntries();
        }
    }
}
