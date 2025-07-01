using System;
using System.Collections.Generic;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.Plugins.Google.Columns
{
    /// <summary>
    /// Implementation of <see cref="SheetColumn"/> that can be used to sync <see cref="IMetadata"/> that is part of <see cref="SharedTableData"/>.
    /// For a version that supports Table Metadata see <seealso cref="LocaleMetadataColumn{TMetadata}"/>.
    /// </summary>
    /// <typeparam name="TMetadata"></typeparam>
    [Serializable]
    public abstract class KeyMetadataColumn<TMetadata> : SheetColumn where TMetadata : IMetadata
    {
        /// <inheritdoc/>
        public override void PushBegin(StringTableCollection collection) {}

        /// <summary>
        /// Called during <see cref="GoogleSheets.PushStringTableCollection"/> or <see cref="GoogleSheets.PushStringTableCollectionAsync"/>.
        /// Used to extract values from <see cref="IMetadata"/> and convert them into a value and/or note that can be part of a Google Spreadsheet column cell.
        /// </summary>
        /// <param name="metadata">The metadata to update.</param>
        /// <param name="value">The value that should appear in the Google Spreadsheet column cell.</param>
        /// <param name="note">The value that should appear in the Google Spreadsheet column as a note. Return null to not include a note.</param>
        public abstract void PushMetadata(TMetadata metadata, out string value, out string note);

        /// <inheritdoc/>
        public override void PullBegin(StringTableCollection collection) {}

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="keyEntry"></param>
        /// <param name="tableEntries"></param>
        /// <param name="value"></param>
        /// <param name="note"></param>
        public override void PushCellData(SharedTableData.SharedTableEntry keyEntry, IList<StringTableEntry> tableEntries, out string value, out string note)
        {
            var metadata = keyEntry.Metadata.GetMetadata<TMetadata>();
            if (metadata != null)
            {
                PushMetadata(metadata, out value, out note);
            }
            else
            {
                value = null;
                note = null;
            }
        }

        /// <summary>
        /// Called during <see cref="GoogleSheets.PullIntoStringTableCollection"/> to extract data from a Google Spreadsheet column cell and convert it into <see cref="IMetadata"/>.
        /// </summary>
        /// <param name="keyEntry">The entry for this column row.</param>
        /// <param name="metadata">The metadata to populate with the cell value and/or note.</param>
        /// <param name="cellValue">The cell value from the Google Spreadsheet column.</param>
        /// <param name="cellNote">The cell note if one exists from the Google Spreadsheet column.</param>
        public abstract void PullMetadata(SharedTableData.SharedTableEntry keyEntry, TMetadata metadata, string cellValue, string cellNote);

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="keyEntry"></param>
        /// <param name="cellValue"></param>
        /// <param name="cellNote"></param>
        public override void PullCellData(SharedTableData.SharedTableEntry keyEntry, string cellValue, string cellNote)
        {
            var metadata = keyEntry.Metadata.GetMetadata<TMetadata>();
            PullMetadata(keyEntry, metadata, cellValue, cellNote);
        }
    }
}
