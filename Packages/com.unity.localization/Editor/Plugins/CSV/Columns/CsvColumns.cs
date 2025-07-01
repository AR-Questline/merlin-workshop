using System;
using System.Collections.Generic;
using CsvHelper;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.Plugins.CSV.Columns
{
    /// <summary>
    /// Synchronizes one or more CSV columns with localization data.
    /// </summary>
    /// <example>
    /// This shows how synchronize custom <see cref="UnityEngine.Localization.Metadata.IMetadata"/> over multiple CSV columns.
    /// Note: To use **CsvWriter** and **CsvReader** you will need to add an [Assembly Reference](https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html) to **Unity.Localization.ThirdParty.Editor**.
    /// <code source="../../../../DocCodeSamples.Tests/CsvSamples.cs" region="custom-column"/>
    /// </example>
    [Serializable]
    public abstract class CsvColumns
    {
        /// <summary>
        /// Called when writing the header row of the CSV file.
        /// Each column that will be written to during <see cref="WriteRow"/> should have
        /// its column header written here using <c>writer.WriteField("Header Title")</c>.
        /// </summary>
        /// <param name="collection">The collection being exported.</param>
        /// <param name="csvWriter">Provides access to write to a CSV file.</param>
        public abstract void WriteBegin(StringTableCollection collection, CsvWriter csvWriter);

        /// <summary>
        /// Called after the CSV file has been exported. This can be used to perform any required cleanup.
        /// </summary>
        /// <param name="collection">The target collection the CSV file was exported from.</param>
        public virtual void WriteEnd(StringTableCollection collection) {}

        /// <summary>
        /// Called when reading a CSV file column header row.
        /// This call should be used to determine which column indexes to use during <see cref="ReadRow"/>.
        /// Use <c>reader.GetFieldIndex("Column Header", isTryGet: true)</c> to check for a column with a matching name.
        /// </summary>
        /// <param name="collection">The target collection the CSV file is being imported into.</param>
        /// <param name="csvReader">Provides acces to the CSV file.</param>
        public abstract void ReadBegin(StringTableCollection collection, CsvReader csvReader);

        /// <summary>
        /// Called when the CSV file has been imported. This can be used to perform any required cleanup.
        /// </summary>
        /// <param name="collection">The target collection the CSV file was imported into.</param>
        public virtual void ReadEnd(StringTableCollection collection) {}

        /// <summary>
        /// Called when writing each row of the CSV file. This is called in the same order as <see cref="WriteBegin"/> and expects each
        /// column that was written to be populated each time. If a column will not always contain a value, you can use <c>writer.WriteField(string.Empty)</c>.
        /// </summary>
        /// <param name="keyEntry">The shared table data for the current entry that is being written to the row.</param>
        /// <param name="tableEntries">The locale specific table entries for this row.</param>
        /// <param name="writer">Provides write acces to the CSV file.</param>
        public abstract void WriteRow(SharedTableData.SharedTableEntry keyEntry, IList<StringTableEntry> tableEntries, CsvWriter writer);

        /// <summary>
        /// Called when reading each row of the CSV file. Unity recommends to store the indexes of the rows you are
        /// interested in during <see cref="ReadBegin"/> and then use them to access the row using <c>reader.GetField</c>.
        /// </summary>
        /// <param name="keyEntry">The current entry being read.</param>
        /// <param name="reader">Provides read access to the CSV file.</param>
        public abstract void ReadRow(SharedTableData.SharedTableEntry keyEntry, CsvReader reader);
    }
}
