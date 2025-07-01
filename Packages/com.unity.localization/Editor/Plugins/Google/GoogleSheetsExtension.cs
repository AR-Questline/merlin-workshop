using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor.Localization.Plugins.Google.Columns;

namespace UnityEditor.Localization.Plugins.Google
{
    /// <summary>
    /// Provides an editor interface to <see cref="GoogleSheets"/>.
    /// </summary>
    /// <example>
    /// This example adds and configures a <see cref="GoogleSheetsExtension"/> to a <see cref="StringTableCollection"/>.
    /// <code source="../../../DocCodeSamples.Tests/GoogleSheetsSamples.cs" region="setup-extension"/>
    /// </example>
    /// <example>
    /// This example uses the data that was configured in the <see cref="GoogleSheetsExtension"/> to perform a Push.
    /// <code source="../../../DocCodeSamples.Tests/GoogleSheetsSamples.cs" region="push-with-extension"/>
    /// </example>
    /// <example>
    /// This example shows how to use the data that was configured in a Google Sheets extension to perform a pull.
    /// <code source="../../../DocCodeSamples.Tests/GoogleSheetsSamples.cs" region="pull-with-extension"/>
    /// </example>
    /// <example>
    /// This example shows how to push every <see cref="StringTableCollection"/> that contains a <see cref="GoogleSheetsExtension"/>.
    /// <code source="../../../DocCodeSamples.Tests/GoogleSheetsSamples.cs" region="push-all-extensions"/>
    /// </example>
    /// <example>
    /// This example shows how to pull every <see cref="StringTableCollection"/> that contains a <see cref="GoogleSheetsExtension"/>.
    /// <code source="../../../DocCodeSamples.Tests/GoogleSheetsSamples.cs" region="pull-all-extensions"/>
    /// </example>
    [Serializable]
    [StringTableCollectionExtension]
    public class GoogleSheetsExtension : CollectionExtension
    {
        [SerializeField]
        SheetsServiceProvider m_SheetsServiceProvider;

        [SerializeField]
        string m_SpreadsheetId;

        [SerializeField]
        int m_SheetId;

        [SerializeReference]
        List<SheetColumn> m_Columns = new List<SheetColumn>();

        [SerializeReference]
        bool m_RemoveMissingPulledKeys = true;

        /// <summary>
        /// The column mappings. Each <see cref="SheetColumn"/> represents a column in a Google sheet. The column mappings are responsible for converting to and from cell data.
        /// </summary>
        public List<SheetColumn> Columns { get => m_Columns; }

        /// <summary>
        /// The SheetsServiceProvider provides the authorization and connection to the Google Sheets service.
        /// </summary>
        public SheetsServiceProvider SheetsServiceProvider { get => m_SheetsServiceProvider; set => m_SheetsServiceProvider = value; }

        /// <summary>
        /// The Id of the Google Sheet. This can be found by examining the url:
        /// https://docs.google.com/spreadsheets/d/<b>>SpreadsheetId</b>/edit#gid=<b>SheetId</b>
        /// Further information can be found <see href="https://developers.google.com/sheets/api/guides/concepts#spreadsheet_id">here.</see>
        /// </summary>
        public string SpreadsheetId { get => m_SpreadsheetId; set => m_SpreadsheetId = value; }

        /// <summary>
        /// The id of a sheet inside of a Google Spreadsheet. Each tab is a separate sheet.
        /// The sheet id can be found in the url after the gid section:
        /// https://docs.google.com/spreadsheets/d/<b>>SpreadsheetId</b>/edit#gid=<b>SheetId</b>
        /// </summary>
        public int SheetId { get => m_SheetId; set => m_SheetId = value; }

        /// <summary>
        /// If this value is set then after <see cref="GoogleSheets.PullIntoStringTableCollection"/> any keys that were not in the sheet will be removed.
        /// This is useful if you want to use a single sheet and will be adding and removing keys however if using multiple sheets then this value should be false to prevent accidental loss of data.
        /// </summary>
        public bool RemoveMissingPulledKeys { get => m_RemoveMissingPulledKeys; set => m_RemoveMissingPulledKeys = value; }
    }
}
