using System;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Tables;

// Metadata used to indicate that the entry should be translated.
[Serializable]
[Metadata(AllowedTypes = MetadataType.StringTableEntry)]
public class RequiresTranslating : SharedTableEntryMetadata
{
}

public class EntryMenuExample
{
    [LocalizationEntryMenu]
    public static void PopulateEntryMenu(LocalizationTableCollection collection, LocalizationTable table, long id, GenericMenu menu)
    {
        if (table is StringTable st && st.GetEntry(id) is StringTableEntry entry)
        {
            var requiresTranslating = entry.HasTagMetadata<RequiresTranslating>();

            menu.AddItem(new GUIContent("Requires Translating"), requiresTranslating, () =>
            {
                Undo.RecordObject(st, "Toggle Requires Translating");

                if (requiresTranslating)
                    entry.RemoveTagMetadata<RequiresTranslating>();
                else
                    entry.AddTagMetadata<RequiresTranslating>();

                EditorUtility.SetDirty(table);
                EditorUtility.SetDirty(table.SharedData);

                // Send an event so that the localization tables window will refresh.
                LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null, collection);
            });
        }
    }
}
