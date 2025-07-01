using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Tables;

public static class LocalizationEditorSettingsSamples
{
    public static void CreateAndAddLocale()
    {
        #region add-locale

        // Create the Locale asset
        var locale = Locale.CreateLocale(SystemLanguage.Spanish);
        AssetDatabase.CreateAsset(locale, "Assets/Spanish.asset");

        // Add the Locale to the project
        LocalizationEditorSettings.AddLocale(locale);

        #endregion
    }

    public static void RemoveLocale()
    {
        #region remove-locale

        // Find the Locale
        var locale = LocalizationEditorSettings.GetLocale("en");

        // Remove it from the Localization system
        LocalizationEditorSettings.RemoveLocale(locale);

        #endregion
    }

    public static void GetLocales()
    {
        #region get-locales

        foreach (var locale in LocalizationEditorSettings.GetLocales())
        {
            Debug.Log(locale.LocaleName);
        }

        #endregion
    }

    public static void GetLocale()
    {
        #region get-locale

        // Find a Spanish Locale using the SystemLanguage
        var spanish = LocalizationEditorSettings.GetLocale(SystemLanguage.Spanish);

        // Find a Spanish (Mexico) Locale using the code
        var spanishMexico = LocalizationEditorSettings.GetLocale("es-MX");

        #endregion
    }

    #region get-string-tables

    [MenuItem("Localization Samples/Print All String Table Collection Contents")]
    public static void PrintStringTableCollectionContents()
    {
        // This example prints out the contents of every String Table Collection
        var stringBuilder = new StringBuilder();
        foreach (var stringTableCollection in LocalizationEditorSettings.GetStringTableCollections())
        {
            stringBuilder.AppendLine($"String Table Collection Name: {stringTableCollection.TableCollectionName}");
            foreach (var stringTable in stringTableCollection.StringTables)
            {
                stringBuilder.AppendLine($"\tTable {stringTable.LocaleIdentifier}");
                foreach (var stringTableValue in stringTable.Values)
                {
                    stringBuilder.AppendLine($"\t\t{stringTableValue.Key} - {stringTableValue.LocalizedValue}");
                    if (stringTableValue.MetadataEntries.Count > 0)
                    {
                        foreach (var metadataEntry in stringTableValue.MetadataEntries)
                        {
                            stringBuilder.AppendLine($"\t\t\t{metadataEntry}");
                        }
                    }
                }
            }
        }

        Debug.Log(stringBuilder.ToString());
    }

    #endregion

    public static void UpdateCollection()
    {
        #region get-string-table

        // Create the new Locale
        var locale = Locale.CreateLocale(SystemLanguage.Spanish);
        AssetDatabase.CreateAsset(locale, "Assets/Spanish.asset");
        LocalizationEditorSettings.AddLocale(locale);

        // Get the collection
        var collection = LocalizationEditorSettings.GetStringTableCollection("My String Table");

        // Add a new table
        var newTable = collection.AddNewTable(locale.Identifier) as StringTable;

        // Add a new entry to the table
        var entry = newTable.AddEntry("Hello", "Hola");

        // Add some metadata
        entry.AddMetadata(new Comment { CommentText = "This is a comment" });

        // We need to mark the table and shared table data entry as we have made changes
        EditorUtility.SetDirty(newTable);
        EditorUtility.SetDirty(newTable.SharedData);

        #endregion
    }

    #region get-asset-tables

    [MenuItem("Localization Samples/Print All Asset Table Collection Contents")]
    public static void PrintAssetTableCollectionContents()
    {
        // This example prints out the contents of every Asset Table Collection
        var stringBuilder = new StringBuilder();
        foreach (var stringTableCollection in LocalizationEditorSettings.GetAssetTableCollections())
        {
            stringBuilder.AppendLine($"String Table Collection Name: {stringTableCollection.TableCollectionName}");
            foreach (var assetTable in stringTableCollection.AssetTables)
            {
                stringBuilder.AppendLine($"\tTable {assetTable.LocaleIdentifier}");
                foreach (var assetTableValue in assetTable.Values)
                {
                    // Load the asset
                    Object asset = null;
                    if (!string.IsNullOrEmpty(assetTableValue.Guid))
                    {
                        var assetPath = AssetDatabase.GUIDToAssetPath(assetTableValue.Guid);
                        asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                    }

                    stringBuilder.AppendLine($"\t\t{assetTableValue.Key} - {asset}");
                    if (assetTable.MetadataEntries.Count > 0)
                    {
                        foreach (var metadataEntry in assetTable.MetadataEntries)
                        {
                            stringBuilder.AppendLine($"\t\t\t{metadataEntry}");
                        }
                    }
                }
            }
        }

        Debug.Log(stringBuilder.ToString());
    }

    #endregion

    #region get-asset-table

    public static void AddAssetToCollection(Texture asset)
    {
        var collection = LocalizationEditorSettings.GetAssetTableCollection("My Assets");
        if (collection == null)
        {
            collection = LocalizationEditorSettings.CreateAssetTableCollection("My Assets", "Assets/Asset Tables");
        }

        collection.AddAssetToTable(SystemLanguage.English, "My Texture", asset);
    }

    #endregion

    public static void CreateStringTableCollection1()
    {
        #region create-string-collection-1

        var newCollection = LocalizationEditorSettings.CreateStringTableCollection("My Strings", "Assets/String Tables");

        var table = newCollection.GetTable("en") as StringTable;
        table.AddEntry("START_MENU", "Start Game");
        table.AddEntry("OPTIONS_MENU", "Options");
        table.AddEntry("EXIT_MENU", "Quit");

        // Mark modified assets dirty so changes are saved.
        EditorUtility.SetDirty(table);
        EditorUtility.SetDirty(table.SharedData);

        #endregion
    }

    public static void CreateStringTableCollection2()
    {
        #region create-string-collection-2

        var english = LocalizationEditorSettings.GetLocale("en-GB");
        var japanese = LocalizationEditorSettings.GetLocale(SystemLanguage.Japanese);

        // Create a collection with a English (UK) and Japanese table
        var newCollection = LocalizationEditorSettings.CreateStringTableCollection("My Strings", "Assets/String Tables", new List<Locale> { english, japanese });

        var table = newCollection.GetTable("en") as StringTable;
        table.AddEntry("START_MENU", "Start Game");
        table.AddEntry("OPTIONS_MENU", "Options");
        table.AddEntry("EXIT_MENU", "Quit");

        #endregion
    }

    public static void SetPreloadFlag()
    {
        #region set-preload-flag

        var collection = LocalizationEditorSettings.GetStringTableCollection("My Strings");

        // Enable preloading for all tables in the collection
        foreach (var table in collection.StringTables)
        {
            LocalizationEditorSettings.SetPreloadTableFlag(table, true);
        }

        #endregion
    }

    public static void GetPreloadFlag()
    {
        #region get-preload-flag

        var collection = LocalizationEditorSettings.GetStringTableCollection("My Strings");

        var englishTable = collection.GetTable("en");
        Debug.Log($"Preload: {LocalizationEditorSettings.GetPreloadTableFlag(englishTable)}");

        #endregion
    }


    #region add-missing-tables

    [MenuItem("Localization/Add Missing Tables")]
    public static void AddMissingTables()
    {
        foreach (var stringTableCollection in LocalizationEditorSettings.GetStringTableCollections())
        {
            foreach (var locale in LocalizationEditorSettings.GetLocales())
            {
                var table = stringTableCollection.GetTable(locale.Identifier);
                if (table == null)
                {
                    table = stringTableCollection.AddNewTable(locale.Identifier);
                }
            }
        }
    }
    #endregion
}
