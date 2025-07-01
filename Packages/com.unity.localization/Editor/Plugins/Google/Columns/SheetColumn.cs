using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.Plugins.Google.Columns
{
    /// <summary>
    /// The fields that will be populated during <see cref="GoogleSheets.PushStringTableCollection"/>.
    /// This is used to configure the send operation to send the data in the most efficient manner.
    /// This means that if you only wish to populate the value field of a sheet then using <see cref="PushFields.Value"/>
    /// will send only the data for that field, even if the note values have been set.
    /// </summary>
    [Flags]
    public enum PushFields
    {
        /// <summary>
        /// The value field of the cell.
        /// </summary>
        Value = 1,

        /// <summary>
        /// The notes field of the cell.
        /// </summary>
        Note = 2,

        /// <summary>
        /// Both the value and notes fields.
        /// </summary>
        ValueAndNote = Value | Note
    }

    /// <summary>
    /// Represents a column that is responsible for determining which entry to map to the current row in the sheet.
    /// This column will be resolved first on each row before any other columns.
    /// </summary>
    public interface IPullKeyColumn
    {
        /// <summary>
        /// Provides the entry that the current row represents.
        /// </summary>
        /// <param name="cellValue">The value in the cell for the column.</param>
        /// <param name="cellNote">The value in the notes field for the column.</param>
        /// <returns>The entry for the current row or <see langword="null"/> if one could not be found.</returns>
        SharedTableData.SharedTableEntry PullKey(string cellValue, string cellNote);
    }

    /// <summary>
    /// Represents a single Google sheet column with its value and note field.
    /// </summary>
    /// <example>
    /// This is an example of how to synchronize the <see cref="StringTableEntry.IsSmart"/> property.
    /// Any value in the column causes all values to be marked as smart; leaving the field empty indicates they should not be smart.
    /// <code source="../../../../DocCodeSamples.Tests/GoogleSheetsSamples.cs" region="global-smart-string-column"/>
    /// </example>
    [Serializable]
    public abstract class SheetColumn
    {
        [SerializeField]
        string m_Column;

        /// <summary>
        /// The Id of the column.
        /// </summary>
        public string Column
        {
            get => m_Column;
            set => m_Column = value;
        }

        /// <summary>
        /// Controls which cell fields to synchronize.
        /// </summary>
        public abstract PushFields PushFields { get; }

        /// <summary>
        /// <see cref="Column"/> as an index where 0 = 'A', 1 = 'B' etc.
        /// </summary>
        public int ColumnIndex
        {
            get => ColumnNameToIndex(Column);
            set => Column = IndexToColumnName(value);
        }

        /// <summary>
        /// Called when starting a push to allow a column to initialize itself.
        /// </summary>
        /// <param name="collection">The collection to push to a Google Sheet.</param>
        public abstract void PushBegin(StringTableCollection collection);

        /// <summary>
        /// Sets the column title and optional note. These values are always set regardless of the value of <see cref="PushFields"/>.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="header">The title to use for the column header.</param>
        /// <param name="headerNote">Optional note that can be added to the header.</param>
        public abstract void PushHeader(StringTableCollection collection, out string header, out string headerNote);

        /// <summary>
        /// Extracts the data that should populate the columns cell for the row associated with the Key.
        /// </summary>
        /// <param name="keyEntry">The Key that represents the row in the spreadsheet.</param>
        /// <param name="tableEntries">The <see cref="StringTableEntry"/> for the current <see cref="SharedTableData.SharedTableEntry"/>.
        /// The order of the tables will match the source <see cref="StringTableCollection"/>, If a table does not contain data for the current key then a null entry will be used.</param>
        /// <param name="value">The value to be used for the cell. This can be null if <see cref="PushFields"/> is <see cref="PushFields.Note"/> or the cell should be empty.</param>
        /// <param name="note">The value to be used for the cell note. This can be null if <see cref="PushFields"/> is <see cref="PushFields.Value"/> or if there should not be a note for this cell.</param>
        public abstract void PushCellData(SharedTableData.SharedTableEntry keyEntry, IList<StringTableEntry> tableEntries, out string value, out string note);

        /// <summary>
        /// Called after all calls to <see cref="PushCellData"/> to provide an opurtunity to deinitialize, cleanup etc.
        /// </summary>
        public virtual void PushEnd() {}

        /// <summary>
        /// Called when starting a pull to allow a column to initialize itself.
        /// </summary>
        /// <param name="collection">The collection to update from the Google Sheet.</param>
        public abstract void PullBegin(StringTableCollection collection);

        /// <summary>
        /// Called to update the <see cref="StringTableCollection"/> using the provided cell data.
        /// </summary>
        /// <param name="keyEntry">The entry being updated for this cell.</param>
        /// <param name="cellValue">The cell value or <see langword="null"/> if <see cref="PushFields"/> does not contain the flag <see cref="PushFields.Value"/>.</param>
        /// <param name="cellNote">The cell note or <see langword="null"/> if <see cref="PushFields"/> does not contain the flag <see cref="PushFields.Note"/>.</param>
        public abstract void PullCellData(SharedTableData.SharedTableEntry keyEntry, string cellValue, string cellNote);

        /// <summary>
        /// Called after all calls to <see cref="PullCellData"/> to provide an opurtunity to deinitialize, cleanup etc.
        /// </summary>
        public virtual void PullEnd() {}

        /// <summary>
        /// Converts a column id value into its name. Column ids start at 0.
        /// E.G 0 = 'A', 1 = 'B', 26 = 'AA', 27 = 'AB'
        /// </summary>
        /// <param name="index">Id of the column starting at 0('A').</param>
        /// <returns>The column name or null.</returns>
        public static string IndexToColumnName(int index)
        {
            index++;
            string result = null;
            while (--index >= 0)
            {
                result = (char)('A' + index % 26) + result;
                index /= 26;
            }
            return result;
        }

        /// <summary>
        /// Convert a column name to its id value.
        /// E.G 'A' = 0, 'B' = 1, 'AA' = 26, 'AB' = 27
        /// </summary>
        /// <param name="name">The name of the column, case insensitive.</param>
        /// <returns>The column index or 0.</returns>
        /// <exception cref="ArgumentException"></exception>
        public static int ColumnNameToIndex(string name)
        {
            int power = 1;
            int index = 0;
            for (int i = name.Length - 1; i >= 0; --i)
            {
                char c = name[i];
                char a = char.IsUpper(c) ? 'A' : 'a';
                int charId = c - a + 1;

                if (charId < 1 || charId > 26)
                    throw new ArgumentException($"Invalid Column Name '{name}'. Must only contain values 'A-Z'. Item at Index {i} was invalid '{c}'", nameof(name));

                index += charId * power;
                power *= 26;
            }
            return index - 1;
        }
    }
}
