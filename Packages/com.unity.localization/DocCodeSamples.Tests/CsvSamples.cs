using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CsvHelper;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.CSV;
using UnityEditor.Localization.Plugins.CSV.Columns;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Tables;

#region custom-column

[Serializable]
[DisplayName("Custom Data")]
[Metadata(AllowedTypes = MetadataType.StringTableEntry)]
public class MyMetadata : IMetadata
{
    public string someValue;
    public int someOtherValue;
}

public class CsvCustomColumnsExample : CsvColumns
{
    [SerializeField] LocaleIdentifier m_LocaleIdentifier;

    int m_SomeValueIndex, m_SomeOtherValueIndex, m_CollectionTableIndex;
    StringTable m_ImportTable;

    string SomeValueFieldName => m_LocaleIdentifier + " Some Value";
    string SomeOtherValueFieldName => m_LocaleIdentifier + " Some Other Value";

    public override void ReadBegin(StringTableCollection collection, CsvReader reader)
    {
        m_ImportTable = collection.GetTable(m_LocaleIdentifier) as StringTable;
        if (m_ImportTable != null)
        {
            m_SomeValueIndex = reader.GetFieldIndex(SomeValueFieldName, isTryGet: true);
            m_SomeOtherValueIndex = reader.GetFieldIndex(SomeOtherValueFieldName, isTryGet: true);
        }
    }

    public override void ReadRow(SharedTableData.SharedTableEntry keyEntry, CsvReader reader)
    {
        if (m_ImportTable == null)
            return;

        // Get the entry or create one
        StringTableEntry entry = m_ImportTable.GetEntry(keyEntry.Id) ?? m_ImportTable.AddEntry(keyEntry.Id, string.Empty);

        // Get the metadata or add one
        var metadata = entry.GetMetadata<MyMetadata>();
        if (metadata == null)
        {
            metadata = new MyMetadata();
            entry.AddMetadata(metadata);
        }

        if (m_SomeValueIndex != -1)
        {
            metadata.someValue = reader.GetField(m_SomeValueIndex);
        }

        if (m_SomeOtherValueIndex != -1)
        {
            metadata.someOtherValue = reader.GetField<int>(m_SomeOtherValueIndex);
        }
    }

    public override void WriteBegin(StringTableCollection collection, CsvWriter writer)
    {
        // Does the collection contain a string table for our Locale Id?
        var tables = collection.StringTables;
        m_CollectionTableIndex = -1;
        for (int i = 0; i < tables.Count; ++i)
        {
            if (tables[i].LocaleIdentifier == m_LocaleIdentifier)
            {
                m_CollectionTableIndex = i;
            }
        }

        if (m_CollectionTableIndex != -1)
        {
            writer.WriteField(SomeValueFieldName);
            writer.WriteField(SomeOtherValueFieldName);
        }
    }

    public override void WriteRow(SharedTableData.SharedTableEntry keyEntry, IList<StringTableEntry> tableEntries, CsvWriter writer)
    {
        if (m_CollectionTableIndex != -1 && tableEntries[m_CollectionTableIndex] != null)
        {
            var entry = tableEntries[m_CollectionTableIndex];
            var metadata = entry.GetMetadata<MyMetadata>();
            if (metadata != null)
            {
                writer.WriteField(metadata.someValue, true);
                writer.WriteField(metadata.someOtherValue);
                return;
            }
        }

        // Write empty entries
        writer.WriteField(string.Empty);
        writer.WriteField(0);
    }
}
#endregion

public static class CsvExamples
{
    public static void SimpleExport()
    {
        #region export-file

        var collection = LocalizationEditorSettings.GetStringTableCollection("My Strings");
        using (var stream = new StreamWriter("My Strings CSV.csv", false, new UTF8Encoding(false)))
        {
            Csv.Export(stream, collection);
        }
        #endregion
    }

    /// <summary>
    /// This example shows how to configure the data you wish to export in CSV.
    /// </summary>
    public static void ExportCustomColumns()
    {
        #region export-mappings

        var collection = LocalizationEditorSettings.GetStringTableCollection("My Strings");

        // Use custom column mappings to control what data gets exported
        var columnMappings = new List<CsvColumns>();

        // We must have 1 KeyIdColumns so that we can map the table entries correctly
        columnMappings.Add(new KeyIdColumns
        {
            IncludeId = true, // Include the Id column field.
            IncludeSharedComments = true, // Include Shared comments.
        });

        // Export English with no comments
        columnMappings.Add(new LocaleColumns
        {
            LocaleIdentifier = "en",
            IncludeComments = false
        });

        // Export Japanese with comments
        columnMappings.Add(new LocaleColumns
        {
            LocaleIdentifier = SystemLanguage.Japanese,
            IncludeComments = true
        });

        // Now export
        using (var stream = new StreamWriter("My Strings CSV.csv", false, new UTF8Encoding(false)))
        {
            Csv.Export(stream, collection, columnMappings);
        }
        #endregion
    }

    public static void SimpleImport()
    {
        #region import-file

        var collection = LocalizationEditorSettings.GetStringTableCollection("My Strings");
        using (var stream = new StreamReader("My Strings CSV.csv"))
        {
            Csv.ImportInto(stream, collection);
        }
        #endregion
    }

    public static void ImportCustomColumns()
    {
        #region import-mappings

        var collection = LocalizationEditorSettings.GetStringTableCollection("My Strings");

        // Use custom column mappings to control what data gets imported.
        var columnMappings = new List<CsvColumns>();

        // We must have 1 KeyIdColumns so that we can map the table entries correctly
        columnMappings.Add(new KeyIdColumns
        {
            IncludeId = true, // Include the Id column field.
            IncludeSharedComments = true, // Include Shared comments.
        });

        // Export English with no comments
        columnMappings.Add(new LocaleColumns
        {
            LocaleIdentifier = "en",
            IncludeComments = false
        });

        // Export Japanese with comments
        columnMappings.Add(new LocaleColumns
        {
            LocaleIdentifier = SystemLanguage.Japanese,
            IncludeComments = true
        });

        using (var stream = new StreamReader("My Strings CSV.csv"))
        {
            Csv.ImportInto(stream, collection, columnMappings);
        }
        #endregion
    }

    #region bulk-import

    [MenuItem("Localization/CSV/Import All CSV Files")]
    public static void ImportAllExtensions()
    {
        // Get every String Table Collection
        var stringTableCollections = LocalizationEditorSettings.GetStringTableCollections();

        foreach (var collection in stringTableCollections)
        {
            // Its possible a String Table Collection may have more than one extension.
            foreach (var extension in collection.Extensions)
            {
                if (extension is CsvExtension csvExtension)
                {
                    if (!string.IsNullOrEmpty(csvExtension.File) && File.Exists(csvExtension.File))
                    {
                        using (var stream = new StreamReader(csvExtension.File))
                        {
                            Csv.ImportInto(stream, collection, csvExtension.Columns);
                        }
                    }
                }
            }
        }
    }

    #endregion

    #region bulk-simple-export

    [MenuItem("Localization/CSV/Export All CSV Files")]
    public static void ExportAllCsv()
    {
        // Get every String Table Collection
        var stringTableCollections = LocalizationEditorSettings.GetStringTableCollections();

        var path = EditorUtility.SaveFolderPanel("Export All String Table Collections - CSV", "", "");
        if (string.IsNullOrEmpty(path))
            return;

        foreach (var collection in stringTableCollections)
        {
            var file = Path.Combine(path, collection.TableCollectionName + ".csv");
            using (var stream = new StreamWriter(file, false, new UTF8Encoding(false)))
            {
                Csv.Export(stream, collection);
            }
        }
    }

    #endregion

    #region bulk-export

    [MenuItem("Localization/CSV/Export All CSV Files(With Extensions)")]
    public static void ExportAllCsvExtensions()
    {
        // Get every String Table Collection
        var stringTableCollections = LocalizationEditorSettings.GetStringTableCollections();

        foreach (var collection in stringTableCollections)
        {
            // Its possible a String Table Collection may have more than one extension.
            foreach (var extension in collection.Extensions)
            {
                if (extension is CsvExtension csvExtension)
                {
                    if (!string.IsNullOrEmpty(csvExtension.File))
                    {
                        using (var stream = new StreamWriter(csvExtension.File, false, new UTF8Encoding(false)))
                        {
                            Csv.Export(stream, collection, csvExtension.Columns);
                        }
                    }
                }
            }
        }
    }

    #endregion
}
