using System;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

#region custom-table-provider

[Serializable]
public class CustomTableProvider : ITableProvider
{
    public string customTableCollectionName = "My Custom Table";

    public AsyncOperationHandle<TTable> ProvideTableAsync<TTable>(string tableCollectionName, Locale locale) where TTable : LocalizationTable
    {
        Debug.Log($"Requested {locale.LocaleName} {typeof(TTable).Name} with the name `{tableCollectionName}`.");

        // Provide a custom string table only with the name "My Custom Table".
        if (typeof(TTable) == typeof(StringTable) && tableCollectionName == customTableCollectionName)
        {
            // Create the table and its shared table data.
            var table = ScriptableObject.CreateInstance<StringTable>();
            table.SharedData = ScriptableObject.CreateInstance<SharedTableData>();
            table.SharedData.TableCollectionName = customTableCollectionName;
            table.LocaleIdentifier = locale.Identifier;

            // Add some values
            table.AddEntry("My Entry 1", "My localized value 1");
            table.AddEntry("My Entry 2", "My localized value 2");

            return Addressables.ResourceManager.CreateCompletedOperation(table as TTable, null);
        }

        // Fallback to default table loading.
        return default;
    }
}

#endregion

#region set-provider-editor

public static class AssignCustomTableProviderExample
{
    [MenuItem("Localization Samples/Assign Custom table provider")]
    public static void AssignTableProvider()
    {
        // Create an instance of the table provider.
        var provider = new CustomTableProvider();

        // A provider can be assigned to each database or the same provider can be shared between both.
        var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
        settings.GetStringDatabase().TableProvider = provider;
        settings.GetAssetDatabase().TableProvider = provider;

        // Set dirty so the changes are saved.
        EditorUtility.SetDirty(settings);
    }
}

#endregion
