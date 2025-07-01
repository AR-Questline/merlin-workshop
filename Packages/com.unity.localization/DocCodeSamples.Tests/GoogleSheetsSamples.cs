using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.Google;
using UnityEditor.Localization.Plugins.Google.Columns;
using UnityEditor.Localization.Reporting;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Tables;

#region setup-extension

public static class SetupExtensionSample
{
    [MenuItem("CONTEXT/StringTableCollection/Add Configured Google Sheet Extension")]
    public static void AddAndConfigureExtension(MenuCommand command)
    {
        var collection = command.context as StringTableCollection;

        // Get or add a new GoogleSheetsExtension.
        var googleExtension = collection.Extensions.FirstOrDefault(e => e is GoogleSheetsExtension) as GoogleSheetsExtension;
        if (googleExtension == null)
        {
            googleExtension = new GoogleSheetsExtension();
            collection.AddExtension(googleExtension);
        }

        // Clear old data.
        googleExtension.Columns.Clear();

        // We need configure what each column will contain in the sheet
        var columnMappings = new List<SheetColumn>
        {
            // Column A will contain the Key
            new KeyColumn { Column = "A" },

            // Column B will contain any shared comments. These are Comment Metadata in the Shared category.
            new KeyCommentColumn { Column = "B" },

            // Column C will contain the English Locale and any comments that are just for this Locale.
            new LocaleColumn { Column = "C", LocaleIdentifier = "en", IncludeComments = true },
        };

        // Assign the columns to the extension
        googleExtension.Columns.AddRange(columnMappings);

        // Assign our Google Sheets service asset
        const string pathToYourAsset = "Assets/Google Sheets Service.asset"; //The path to your SheetsServiceProvider asset. See docs for further info.
        var sheetsServiceProvider = AssetDatabase.LoadAssetAtPath<SheetsServiceProvider>(pathToYourAsset);
        googleExtension.SheetsServiceProvider = sheetsServiceProvider;

        googleExtension.SpreadsheetId = "My spread sheet id"; // We need to provide the Spreadsheet id. This can be found in the url. See docs for further info.
        googleExtension.SheetId = 123456; // This is the id of the sheet in the Google Spreadsheet. it will be in the url after `gid=`.

        // Mark the collection dirty so that the changes are saved
        EditorUtility.SetDirty(collection);
    }
}

#endregion

#region locale-metadata-column

[Serializable]
[DisplayName("Custom Data")]
[Metadata(AllowedTypes = MetadataType.StringTableEntry)]
public class MyCustomDataMetadata : IMetadata
{
    public string someValue;
    public string someNoteValue;
}

/// <summary>
/// LocaleMetadataColumn is a version of SheetColumn only used for handling Metadata.
/// This can now be added to the Column Mappings for any Push or Pull request.
/// </summary>
public class MyCustomColumn : LocaleMetadataColumn<MyCustomDataMetadata>
{
    public override PushFields PushFields => PushFields.ValueAndNote; // For our example we use both value and note.

    public override void PullMetadata(StringTableEntry entry, MyCustomDataMetadata metadata, string cellValue, string cellNote)
    {
        // Metadata will be null if the entry does not already contain any.
        if (metadata == null)
        {
            metadata = new MyCustomDataMetadata();
            entry.AddMetadata(metadata);
        }

        metadata.someValue = cellValue;
        metadata.someNoteValue = cellNote;
    }

    public override void PushHeader(StringTableCollection collection, out string header, out string headerNote)
    {
        // The title of the Google Sheet column
        header = "My Custom Data";
        headerNote = null;
    }

    public override void PushMetadata(MyCustomDataMetadata metadata, out string value, out string note)
    {
        // Metadata will never be null as this is only called if the entry contains a metadata entry.
        value = metadata.someValue;
        note = metadata.someNoteValue;
    }
}
#endregion

#region smart-string-column

public class SmartStringColumn : LocaleMetadataColumn<SmartFormatTag>
{
    public override PushFields PushFields => PushFields.Value;

    public override void PullMetadata(StringTableEntry entry, SmartFormatTag metadata, string cellValue, string cellNote)
    {
        entry.IsSmart = !string.IsNullOrEmpty(cellValue);
    }

    public override void PushHeader(StringTableCollection collection, out string header, out string headerNote)
    {
        header = $"{LocaleIdentifier.ToString()} - Is Smart String";
        headerNote = null;
    }

    public override void PushMetadata(SmartFormatTag metadata, out string value, out string note)
    {
        value = "x"; // We mark here with an x but it could be anything.
        note = null;
    }
}

#endregion

#region global-smart-string-column

[Serializable]
public class GlobalSmartStringColumn : SheetColumn
{
    public override PushFields PushFields => PushFields.Value;

    StringTableCollection m_TableCollection;

    public override void PullBegin(StringTableCollection collection)
    {
        m_TableCollection = collection;
    }

    public override void PullCellData(SharedTableData.SharedTableEntry keyEntry, string cellValue, string cellNote)
    {
        bool enableSmartString = !string.IsNullOrEmpty(cellValue);

        // Go through all the entries
        foreach (var table in m_TableCollection.StringTables)
        {
            var entry = table.GetEntry(keyEntry.Id);
            if (entry != null)
                entry.IsSmart = enableSmartString;
        }
    }

    public override void PushBegin(StringTableCollection collection)
    {
        m_TableCollection = collection;
    }

    public override void PushCellData(SharedTableData.SharedTableEntry keyEntry, IList<StringTableEntry> tableEntries, out string value, out string note)
    {
        // Use the first table as our source of truth
        var entry = m_TableCollection.StringTables[0].GetEntry(keyEntry.Id);
        value = entry != null && entry.IsSmart ? "x" : string.Empty;
        note = null;
    }

    public override void PushHeader(StringTableCollection collection, out string header, out string headerNote)
    {
        header = "Smart String";
        headerNote = null;
    }
}

#endregion

/// <summary>
/// These examples show various ways to sync a String Table Collection with Google Sheets.
/// These examples are illustrative and will not work as they are without correct Google Sheets credentials and String Table data.
/// </summary>
public class GoogleSheetsExamples
{
    static SheetsServiceProvider GetServiceProvider()
    {
        #region sheets-service-provider

        // The Sheets service provider performs the authentication and keeps track of the
        // authentication tokens so that we do not need to authenticate each time.
        // It is recommended to have a SheetsServiceProvider asset pre-configured for
        // use however in this example we will create a new one.
        var sheetServiceProvider = ScriptableObject.CreateInstance<SheetsServiceProvider>();

        // OAuth is required when making changes. See the docs for info setting up OAuth credentials.
        sheetServiceProvider.SetOAuthCredentials("some-client-id", "some-client-secret");
        return sheetServiceProvider;
        #endregion
    }

    #region push-collection-english

    [MenuItem("Localization/Google Sheets/Push English")]
    public static void PushEnglish()
    {
        // Setup the connection to Google. You will need a preconfigured SheetsServiceProvider asset.
        var sheetServiceProvider = AssetDatabase.LoadAssetAtPath<SheetsServiceProvider>("Assets/Sheets Service Provider.asset");
        var googleSheets = new GoogleSheets(sheetServiceProvider);
        googleSheets.SpreadSheetId = "My spread sheet id"; // We need to provide the Spreadsheet id. This can be found in the url. See docs for further info.

        // Prepare the data we want to push.
        // You should provide your String Table Collection name here
        var tableCollection = LocalizationEditorSettings.GetStringTableCollection("My Strings");

        // We need configure what each column will contain in the sheet
        var columnMappings = new SheetColumn[]
        {
            // Column A will contain the Key
            new KeyColumn { Column = "A" },

            // Column B will contain any shared comments. These are Comment Metadata in the Shared category.
            new KeyCommentColumn { Column = "B" },

            // Column C will contain the English Locale and any comments that are just for this Locale.
            new LocaleColumn { Column = "C", LocaleIdentifier = "en", IncludeComments = true },
        };

        int mySheetId = 123456; // This it the id of the sheet in the Google Spreadsheet. it will be in the url after `gid=`.

        // Now send the update.
        googleSheets.PushStringTableCollection(mySheetId, tableCollection, columnMappings);
    }

    #endregion

    #region pull-collection-english

    [MenuItem("Localization/Google Sheets/Pull English")]
    public static void PullEnglish()
    {
        // Setup the connection to Google. You will need a preconfigured SheetsServiceProvider asset.
        var sheetServiceProvider = AssetDatabase.LoadAssetAtPath<SheetsServiceProvider>("Assets/Sheets Service Provider.asset");
        var googleSheets = new GoogleSheets(sheetServiceProvider);
        googleSheets.SpreadSheetId = "My spread sheet id"; // We need to provide the Spreadsheet id. This can be found in the url. See docs for further info.

        // You should provide your String Table Collection name here
        var tableCollection = LocalizationEditorSettings.GetStringTableCollection("My Strings");

        // We need configure what each column contains in the sheet
        var columnMappings = new SheetColumn[]
        {
            // Column A contains the Key
            new KeyColumn { Column = "A" },

            // Column B contains any shared comments. These are Comment Metadata in the Shared category.
            new KeyCommentColumn { Column = "B" },

            // Column C contains the English Locale and any comments that are just for this Locale.
            new LocaleColumn { Column = "C", LocaleIdentifier = "en", IncludeComments = true },
        };

        int mySheetId = 123456; // This it the id of the sheet in the Google Spreadsheet. it will be in the url after `gid=`.
        googleSheets.PullIntoStringTableCollection(mySheetId, tableCollection, columnMappings);
    }

    #endregion

    #region push-project-locales

    [MenuItem("Localization/Google Sheets/Push Project Locales")]
    public static void PushProjectLocales()
    {
        // Setup the connection to Google. You will need a preconfigured SheetsServiceProvider asset.
        var sheetServiceProvider = AssetDatabase.LoadAssetAtPath<SheetsServiceProvider>("Assets/Sheets Service Provider.asset");
        var googleSheets = new GoogleSheets(sheetServiceProvider);
        googleSheets.SpreadSheetId = "My spread sheet id"; // We need to provide the Spreadsheet id. This can be found in the url. See docs for further info.

        // Prepare the data we want to push.
        // You should provide your String Table Collection name here
        var tableCollection = LocalizationEditorSettings.GetStringTableCollection("My Strings");

        // CreateDefaultMapping will create a KeyColumn and a LocaleColumn for each Locale in the project.
        var columnMappings = ColumnMapping.CreateDefaultMapping();
        int mySheetId = 123456; // This is the id of the sheet in the Google Spreadsheet. it will be in the url after `gid=`.

        // Now send the update. We can pass in an optional ProgressBarReporter so that we can see updates in the Editor.
        googleSheets.PushStringTableCollection(mySheetId, tableCollection, columnMappings, new ProgressBarReporter());
    }

    #endregion

    #region pull-project-locales

    [MenuItem("Localization/Google Sheets/Pull Project Locales")]
    public static void PullProjectLocales()
    {
        // Setup the connection to Google. You will need a preconfigured SheetsServiceProvider asset.
        var sheetServiceProvider = AssetDatabase.LoadAssetAtPath<SheetsServiceProvider>("Assets/Sheets Service Provider.asset");
        var googleSheets = new GoogleSheets(sheetServiceProvider);
        googleSheets.SpreadSheetId = "My spread sheet id"; // We need to provide the Spreadsheet id. This can be found in the url. See docs for further info.

        // You should provide your String Table Collection name here
        var tableCollection = LocalizationEditorSettings.GetStringTableCollection("My Strings");

        // CreateDefaultMapping will create a KeyColumn and a LocaleColumn for each Locale in the project.
        // This assumes that the table was created and pushed to using the same column mappings.
        var columnMappings = ColumnMapping.CreateDefaultMapping();
        int mySheetId = 123456; // This it the id of the sheet in the Google Spreadsheet. it will be in the url after `gid=`.

        // Now pull.
        // removeMissingEntries will remove any Keys that we have in the String Table Collection that do not exist in the Pull update.
        // reporter is an optional reporter that can be used to povide feedback in the editor during the Pull.
        googleSheets.PullIntoStringTableCollection(mySheetId, tableCollection, columnMappings, removeMissingEntries: true, reporter: new ProgressBarReporter());
    }

    #endregion

    #region push-with-extension

    [MenuItem("Localization/Google Sheets/Push With Google Extension")]
    public static void PushWithExtension()
    {
        // You should provide your String Table Collection name here
        var tableCollection = LocalizationEditorSettings.GetStringTableCollection("My Strings");
        var googleExtension = tableCollection.Extensions.FirstOrDefault(e => e is GoogleSheetsExtension) as GoogleSheetsExtension;
        if (googleExtension == null)
        {
            Debug.LogError($"String Table Collection {tableCollection.TableCollectionName} Does not contain a Google Sheets Extension.");
            return;
        }

        PushExtension(googleExtension);
    }

    static void PushExtension(GoogleSheetsExtension googleExtension)
    {
        // Setup the connection to Google
        var googleSheets = new GoogleSheets(googleExtension.SheetsServiceProvider);
        googleSheets.SpreadSheetId = googleExtension.SpreadsheetId;

        // Now send the update. We can pass in an optional ProgressBarReporter so that we can updates in the Editor.
        googleSheets.PushStringTableCollection(googleExtension.SheetId, googleExtension.TargetCollection as StringTableCollection, googleExtension.Columns, new ProgressBarReporter());
    }

    #endregion

    #region pull-with-extension

    [MenuItem("Localization/Google Sheets/Pull With Google Extension")]
    public static void PullWithExtension()
    {
        // You should provide your String Table Collection name here
        var tableCollection = LocalizationEditorSettings.GetStringTableCollection("My Strings");
        var googleExtension = tableCollection.Extensions.FirstOrDefault(e => e is GoogleSheetsExtension) as GoogleSheetsExtension;
        if (googleExtension == null)
        {
            Debug.LogError($"String Table Collection {tableCollection.TableCollectionName} Does not contain a Google Sheets Extension.");
            return;
        }

        PullExtension(googleExtension);
    }

    static void PullExtension(GoogleSheetsExtension googleExtension)
    {
        // Setup the connection to Google
        var googleSheets = new GoogleSheets(googleExtension.SheetsServiceProvider);
        googleSheets.SpreadSheetId = googleExtension.SpreadsheetId;

        // Now update the collection. We can pass in an optional ProgressBarReporter so that we can updates in the Editor.
        googleSheets.PullIntoStringTableCollection(googleExtension.SheetId, googleExtension.TargetCollection as StringTableCollection, googleExtension.Columns, reporter: new ProgressBarReporter());
    }

    #endregion

    #region push-all-extensions

    [MenuItem("Localization/Google Sheets/Push All Google Sheets Extensions")]
    public static void PushAllExtensions()
    {
        // Get every String Table Collection
        var stringTableCollections = LocalizationEditorSettings.GetStringTableCollections();

        foreach (var collection in stringTableCollections)
        {
            // Its possible a String Table Collection may have more than one GoogleSheetsExtension.
            // For example if each Locale we pushed/pulled from a different sheet.
            foreach (var extension in collection.Extensions)
            {
                if (extension is GoogleSheetsExtension googleExtension)
                {
                    PushExtension(googleExtension);
                }
            }
        }
    }

    #endregion

    #region pull-all-extensions

    [MenuItem("Localization/Google Sheets/Pull All Google Sheets Extensions")]
    public static void PullAllExtensions()
    {
        // Get every String Table Collection
        var stringTableCollections = LocalizationEditorSettings.GetStringTableCollections();

        foreach (var collection in stringTableCollections)
        {
            // Its possible a String Table Collection may have more than one GoogleSheetsExtension.
            // For example if each Locale we pushed/pulled from a different sheet.
            foreach (var extension in collection.Extensions)
            {
                if (extension is GoogleSheetsExtension googleExtension)
                {
                    PullExtension(googleExtension);
                }
            }
        }
    }

    #endregion
}
