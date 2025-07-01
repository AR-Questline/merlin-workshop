using System;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

#region custom-table-patcher

[Serializable]
public class CustomTablePatcher : ITablePostprocessor
{
    public void PostprocessTable(LocalizationTable table)
    {
        Debug.Log($"Postprocess {table}");

        if (table is StringTable stringTable)
        {
            // Add a new value
            stringTable.AddEntry("some new entry", "localized value");

            // Update an old value
            var entry = stringTable.GetEntry("some existing value");
            if (entry != null)
            {
                entry.Value = "updated localized value";
            }
        }
        else if (table is AssetTable assetTable)
        {
            // Add a new value
            var entry = assetTable.AddEntry("my texture asset", null);
            entry.SetAssetOverride(Texture2D.whiteTexture);

            // Override an existing value
            var overrideEntry = assetTable.GetEntry("existing entry");
            if (overrideEntry != null)
            {
                var texture = new Texture2D(10, 10);
                overrideEntry.SetAssetOverride(texture);
            }
        }
    }
}

#endregion

#region set-patcher-editor

public static class AssignCustomTablePatcherExample
{
    [MenuItem("Localization Samples/Assign Custom table postprocessor")]
    public static void AssignTablePostprocessor()
    {
        // Create an instance of the table provider.
        var provider = new CustomTablePatcher();

        // A table postprocessor can be assigned to each database or the same can be shared between both.
        var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
        settings.GetStringDatabase().TablePostprocessor = provider;
        settings.GetAssetDatabase().TablePostprocessor = provider;

        // Set dirty so the changes are saved.
        EditorUtility.SetDirty(settings);
    }
}

#endregion
