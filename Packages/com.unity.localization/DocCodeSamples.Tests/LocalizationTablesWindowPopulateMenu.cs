using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;

public class JsonExportExample
{
    [Serializable]
    public class JsonStringTableEntry
    {
        public string key;
        public string value;
    }

    [Serializable]
    public class JsonStructure
    {
        public string locale;
        public List<JsonStringTableEntry> entries = new List<JsonStringTableEntry>();
    }

    [LocalizationImportMenu]
    public static void PopulateImportMenu(LocalizationTableCollection collection, DropdownMenu menu)
    {
        if (collection is StringTableCollection stringTableCollection)
        {
            menu.AppendAction("Json", _ => ImportIntoCollectionFromJson(stringTableCollection));
        }
    }

    [LocalizationEntryMenu]
    public static void PopulateEntryMenu(LocalizationTableCollection collection, LocalizationTable table, long id, GenericMenu menu)
    {
        if (table is StringTable st)
        {
            menu.AddItem(new GUIContent("Clear Entry"), false, () =>
            {
                if (st.GetEntry(id) is StringTableEntry entry)
                {
                    Undo.RecordObject(st, "Clear Entry");
                    
                    entry.Value = string.Empty;

                    EditorUtility.SetDirty(table);
                    EditorUtility.SetDirty(table.SharedData);

                    // Send an event so that the localization tables window will refresh.
                    LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null, collection);
                }
            });
        }
    }

    [LocalizationExportMenu]
    public static void PopulateExportMenu(LocalizationTableCollection collection, DropdownMenu menu)
    {
        if (collection is StringTableCollection stringTableCollection)
        {
            foreach (var table in stringTableCollection.StringTables)
            {
                menu.AppendAction($"Json/{table.LocaleIdentifier}", _ => ExportAsJson(table));
            }
        }
    }

    static void ExportAsJson(StringTable table)
    {
        var path = EditorUtility.SaveFilePanel("Save collection to Json", "", $"{table.TableCollectionName} - {table.LocaleIdentifier.Code}", "json");
        if (string.IsNullOrEmpty(path))
            return;

        // Convert to our json format
        var json = new JsonStructure { locale = table.LocaleIdentifier.Code };

        foreach (var entry in table.Values)
        {
            json.entries.Add(new JsonStringTableEntry
            {
                key = entry.Key,
                value = entry.Value
            });
        }

        File.WriteAllText(path, JsonUtility.ToJson(json, true));
    }

    static void ImportIntoCollectionFromJson(StringTableCollection collection)
    {
        var path = EditorUtility.OpenFilePanel("Import collection to Json", "", "json");
        if (string.IsNullOrEmpty(path))
            return;
        var jsonText = File.ReadAllText(path);
        var json = JsonUtility.FromJson<JsonStructure>(jsonText);
        var table = collection.GetTable(json.locale) as StringTable;
        if (table == null)
            table = collection.AddNewTable(json.locale) as StringTable;

        // Undo/Redo support
        Undo.RecordObjects(new UnityEngine.Object[] { table, table.SharedData }, "Import Json");
        foreach (var entry in json.entries)
        {
            table.AddEntry(entry.key, entry.value);
        }

        EditorUtility.SetDirty(table);
        EditorUtility.SetDirty(table.SharedData);

        // Send an event so that the localization tables window will refresh.
        LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null, collection);
    }
}
