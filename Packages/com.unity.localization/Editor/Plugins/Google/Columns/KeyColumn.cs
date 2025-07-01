using System;
using System.Collections.Generic;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.Plugins.Google.Columns
{
    /// <summary>
    /// Represents a column in a Google Sheets that can be used to map the row to a <see cref="StringTableEntry"/>.
    /// </summary>
    [Serializable]
    public class KeyColumn : SheetColumn, IPullKeyColumn
    {
        SharedTableData m_SharedTableData;

        /// <summary>
        /// The default name to use for the column is "Key".
        /// </summary>
        public const string ColumnHeader = "Key";

        /// <inheritdoc/>
        public override PushFields PushFields => PushFields.ValueAndNote;

        /// <inheritdoc/>
        public override void PushBegin(StringTableCollection collection) {}

        /// <inheritdoc/>
        public override void PushHeader(StringTableCollection collection, out string header, out string headerNote)
        {
            header = ColumnHeader;
            headerNote = null;
        }

        /// <inheritdoc/>
        public override void PushCellData(SharedTableData.SharedTableEntry keyEntry, IList<StringTableEntry> tableEntries, out string value, out string note)
        {
            value = keyEntry.Key;
            note = keyEntry.Id.ToString();
        }

        /// <inheritdoc/>
        public override void PullBegin(StringTableCollection collection)
        {
            m_SharedTableData = collection.SharedData;
        }

        /// <inheritdoc/>
        public override void PullCellData(SharedTableData.SharedTableEntry keyEntry, string cellValue, string cellNote) {}

        /// <inheritdoc/>
        /// <remarks>
        /// The <see cref="SharedTableData.SharedTableEntry"/> is resolved in the following way:
        /// ![](../manual/images/scripting/GoogleKeyColumn_PullKey.dot.svg)
        /// </remarks>
        public SharedTableData.SharedTableEntry PullKey(string cellValue, string cellNote)
        {
            if (!string.IsNullOrEmpty(cellNote) && long.TryParse(cellNote, out var keyId))
            {
                var entry = m_SharedTableData.GetEntry(keyId);
                if (entry != null)
                {
                    if (entry.Key != cellValue)
                        m_SharedTableData.RenameKey(entry.Id, cellValue);
                    return entry;
                }
                else
                {
                    // Create a new entry with the id
                    return m_SharedTableData.AddKey(cellValue, keyId);
                }
            }

            return m_SharedTableData.GetEntry(cellValue) ?? m_SharedTableData.AddKey(cellValue);
        }
    }
}
