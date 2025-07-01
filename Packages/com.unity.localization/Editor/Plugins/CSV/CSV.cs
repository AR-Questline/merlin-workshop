using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using UnityEditor.Localization.Plugins.CSV.Columns;
using UnityEditor.Localization.Reporting;
using UnityEngine;
using UnityEngine.Localization.Metadata;
using static UnityEngine.Localization.Tables.SharedTableData;

namespace UnityEditor.Localization.Plugins.CSV
{
    /// <summary>
    /// Comma Separated Values (CSV) support.
    /// Used to transfer localized data and carry it from one step of the localization process to the other,
    /// while allowing interoperability between and among tools.
    /// </summary>
    public static class Csv
    {
        /// <summary>
        /// Exports all <see cref="UnityEngine.Localization.Tables.StringTable"/> in <paramref name="collection"/> using default column mappings generated through
        /// <see cref="ColumnMapping.CreateDefaultMapping(bool)"/>.
        /// </summary>
        /// <param name="writer">The target that will be populated with CSV data.</param>
        /// <param name="collection">The collection to export to CSV.</param>
        /// <param name="reporter">An optional reporter that can be used to provide feedback during export.</param>
        /// <example>
        /// This example shows how to export a <see cref="StringTableCollection"/> to a CSV file.
        /// <code source="../../../DocCodeSamples.Tests/CsvSamples.cs" region="export-file"/>
        /// </example>
        public static void Export(TextWriter writer, StringTableCollection collection, ITaskReporter reporter = null)
        {
            Export(writer, collection, ColumnMapping.CreateDefaultMapping(), reporter);
        }

        /// <summary>
        /// Exports a <see cref="StringTableCollection"/> using <paramref name="columnMappings"/> to control the contents of each exported column.
        /// <see cref="ColumnMapping.CreateDefaultMapping(bool)"/>.
        /// </summary>
        /// <param name="writer">The target that will be populated with CSV data.</param>
        /// <param name="collection">The collection to export to CSV.</param>
        /// <param name="columnMappings">Controls what will be exported.
        /// The <seealso cref="KeyIdColumns"/> can be used to export the Key, Id and shared comments whilst <seealso cref="LocaleColumns"/> can be
        /// used to export the values and comments for a specific <see cref="UnityEngine.Localization.Locale"/></param>.
        /// <seealso cref="ColumnMapping.CreateDefaultMapping(bool)"/> can be used to generate the default columns for the project.
        /// <param name="reporter">An optional reporter that can be used to provide feedback during export.</param>
        /// <example>
        /// This example shows how to configure the data you wish to export in CSV through column mappings.
        /// <code source="../../../DocCodeSamples.Tests/CsvSamples.cs" region="export-mappings"/>
        /// </example>
        /// <example>
        /// This example shows how to export every <see cref="StringTableCollection"/> that contains a <see cref="CsvExtension"/>.
        /// <code source="../../../DocCodeSamples.Tests/CsvSamples.cs" region="bulk-export"/>
        /// </example>
        /// <example>
        /// This example shows how to export all <see cref="StringTableCollection"/> to multiple CSV files.
        /// <code source="../../../DocCodeSamples.Tests/CsvSamples.cs" region="bulk-simple-export"/>
        /// </example>
        public static void Export(TextWriter writer, StringTableCollection collection, IList<CsvColumns> columnMappings, ITaskReporter reporter = null)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            VerifyColumnMappings(columnMappings);

            using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                try
                {
                    if (reporter?.Started != true)
                        reporter?.Start("Importing CSV", string.Empty);
                    reporter?.ReportProgress("Writing Headers", 0);
                    foreach (var cell in columnMappings)
                    {
                        cell.WriteBegin(collection, csvWriter);
                    }

                    reporter?.ReportProgress("Writing Contents", 0.1f);
                    foreach (var row in collection.GetRowEnumeratorUnsorted())
                    {
                        if (row.TableEntries[0] != null && row.TableEntries[0].SharedEntry.Metadata.HasMetadata<ExcludeEntryFromExport>())
                            continue;

                        csvWriter.NextRecord();
                        foreach (var cell in columnMappings)
                        {
                            if (string.IsNullOrEmpty(row.KeyEntry.Key))
                                continue;

                            cell.WriteRow(row.KeyEntry, row.TableEntries, csvWriter);
                        }
                    }

                    foreach (var cell in columnMappings)
                    {
                        cell.WriteEnd(collection);
                    }

                    reporter?.Completed("Finished Exporting");
                }
                catch (Exception e)
                {
                    reporter?.Fail("Failed Exporting.\n" + e.Message);
                    throw;
                }
            }
        }

        /// <summary>
        /// Import the CSV data into <paramref name="collection"/> using <paramref name="columnMappings"/> to control what data will be imported.
        /// See <seealso cref="KeyIdColumns"/> and <seealso cref="LocaleColumns"/> for further details.
        /// </summary>
        /// <param name="reader">The source of the CSV data.</param>
        /// <param name="collection">The target collection to be updated using the CSV data.</param>
        /// <param name="createUndo">Should an Undo operation be created so the changes can be undone?</param>
        /// <param name="reporter">An optional reporter that can be used to provide feedback during import.</param>
        /// <param name="removeMissingEntries">After a pull has completed, any keys that exist in the <paramref name="collection"/> but did not exist in the CSV are considered missing,
        /// this may be because they have been deleted from the sheet. A value of true will remove these missing entries; false will preserve them.</param>
        /// <example>
        /// This example show how to import a collection with default settings.
        /// <code source="../../../DocCodeSamples.Tests/CsvSamples.cs" region="import-file"/>
        /// </example>
        public static void ImportInto(TextReader reader, StringTableCollection collection, bool createUndo = false, ITaskReporter reporter = null, bool removeMissingEntries = false)
        {
            ImportInto(reader, collection, ColumnMapping.CreateDefaultMapping(), createUndo, reporter, removeMissingEntries);
        }

        /// <summary>
        /// Import the CSV data into <paramref name="collection"/> using default column mappings generated using <see cref="ColumnMapping.CreateDefaultMapping(bool)"/>.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="collection"></param>
        /// <param name="columnMappings"></param>
        /// <param name="createUndo"></param>
        /// <param name="reporter"></param>
        /// <param name="removeMissingEntries">After a pull has completed, any keys that exist in the <paramref name="collection"/> but did not exist in the CSV are considered missing,
        /// this may be because they have been deleted from the sheet. A value of true will remove these missing entries; false will preserve them.</param>
        /// <example>
        /// This example shows how to configure the data you wish to import in CSV through column mappings.
        /// <code source="../../../DocCodeSamples.Tests/CsvSamples.cs" region="import-mappings"/>
        /// </example>
        /// <example>
        /// This example shows how to import every <see cref="StringTableCollection"/> that contains a <see cref="CsvExtension"/>.
        /// <code source="../../../DocCodeSamples.Tests/CsvSamples.cs" region="bulk-import"/>
        /// </example>
        public static void ImportInto(TextReader reader, StringTableCollection collection, IList<CsvColumns> columnMappings, bool createUndo = false, ITaskReporter reporter = null, bool removeMissingEntries = false)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            VerifyColumnMappings(columnMappings);

            var modifiedAssets = collection.StringTables.Select(t => t as UnityEngine.Object).ToList();
            modifiedAssets.Add(collection.SharedData);

            if (createUndo)
                Undo.RegisterCompleteObjectUndo(modifiedAssets.ToArray(), "Import CSV");

            try
            {
                using (var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csvReader.Read();
                    csvReader.ReadHeader();

                    if (reporter?.Started != true)
                        reporter?.Start("Exporting CSV", string.Empty);
                    reporter?.ReportProgress("Mapping Headers", 0);
                    foreach (var col in columnMappings)
                    {
                        col.ReadBegin(collection, csvReader);
                    }

                    var keyCell = columnMappings.First(o => o is IKeyColumn) as IKeyColumn;

                    reporter?.ReportProgress("Reading Contents", 0.1f);

                    // We want to keep track of the order the entries are pulled in so we can match it
                    var sortedEntries = new List<SharedTableEntry>();
                    var keysProcessed = new HashSet<long>();

                    while (csvReader.Read())
                    {
                        var keyEntry = keyCell.ReadKey(csvReader);
                        if (keyEntry == null)
                            continue;

                        sortedEntries.Add(keyEntry);
                        keysProcessed.Add(keyEntry.Id);

                        foreach (var cell in columnMappings)
                        {
                            cell.ReadRow(keyEntry, csvReader);
                        }
                    }

                    foreach (var cell in columnMappings)
                    {
                        cell.ReadEnd(collection);
                    }

                    var removedEntriesLog = new StringBuilder();
                    collection.MergeUpdatedEntries(keysProcessed, sortedEntries, removedEntriesLog, removeMissingEntries);
                    reporter?.ReportProgress(removedEntriesLog.ToString(), 0.9f);
                }

                modifiedAssets.ForEach(EditorUtility.SetDirty);
                LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null, collection);

                // Flush changes to disk.
                collection.SaveChangesToDisk();
                reporter?.Completed("Finished Importing");
            }
            catch (Exception e)
            {
                reporter?.Fail("Failed Importing.\n" + e.Message);
                throw;
            }
        }

        static void VerifyColumnMappings(IList<CsvColumns> columnMappings)
        {
            if (columnMappings == null)
                throw new ArgumentNullException(nameof(columnMappings));
            if (columnMappings.Any(c => c is null))
                throw new ArgumentException("Column Mappings must not contain any null columns.");
            if (columnMappings.Count == 0)
                throw new ArgumentException("Must include at least 1 column.", nameof(columnMappings));
            var keyColumnCount = columnMappings.Count(c => typeof(IKeyColumn).IsAssignableFrom(c.GetType()));
            if (keyColumnCount != 1)
                throw new ArgumentException($"Must include 1 {nameof(IKeyColumn)} however {keyColumnCount} were found.", nameof(columnMappings));
        }
    }
}
