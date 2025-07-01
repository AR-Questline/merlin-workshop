using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.SimpleTools;
using Awaken.TG.Main.Stories.Actors;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.AI.Barks {
    public static class BarksExporter {
        static readonly ActorsRegister ActorsRegister = ActorsRegister.Get;
        const string ProgressTitle = "Export to google sheets";

        public static async void ExportData(IEnumerable<BarkBookmark> data) {
            string completeMessage = "Export to Google Sheets completed";

            // if (!ARGoogleSheets.Initialized) {
            //     EditorUtility.DisplayProgressBar(ProgressTitle, "Authorizing...", 0);
            //     ARGoogleSheets.Initialize();
            // }

            // EditorUtility.DisplayProgressBar(ProgressTitle, "Authorizing...", 0.2f);
            // await UniTask.DelayFrame(1);
            //
            // Dictionary<string, List<RowData>> dataBySheets = GetSheetData(data);
            //
            // var spreadsheetTask = ARGoogleSheets.CreateSpreadsheet("Barks", dataBySheets);
            // while (spreadsheetTask.IsCompleted == false) {
            //     await UniTask.DelayFrame(1);
            // }
            //
            // if (spreadsheetTask.Result == null) {
            //     EditorUtility.DisplayProgressBar(ProgressTitle, "Failed to create spreadsheet", 1);
            //     return;
            // }
            //
            // EditorUtility.DisplayProgressBar(ProgressTitle, "Spreadsheet created", 1);
            // await UniTask.DelayFrame(1);
            //
            // EditorUtility.ClearProgressBar();
            //
            // if (EditorUtility.DisplayDialog("Export to Google Sheets", completeMessage, "Open Google Sheets",
            //         "Close")) {
            //     Application.OpenURL(spreadsheetTask.Result.SpreadsheetUrl);
            // }
        }

        // static Dictionary<string, List<RowData>> GetSheetData(IEnumerable<BarkBookmark> data) {
        //     List<string> sheetNames = new();
        //
        //     foreach (var bookmark in data) {
        //         foreach (var collection in bookmark.barkTextCollections) {
        //             var trimmedTag = collection.tag.TrimTag();
        //             if (!sheetNames.Contains(trimmedTag)) {
        //                 sheetNames.Add(trimmedTag);
        //             }
        //         }
        //     }
        //
        //     Dictionary<string, List<RowData>> dataBySheets = new();
        //     for (int i = 0; i < sheetNames.Count; i++) {
        //         var sheetName = sheetNames[i];
        //         List<RowData> rowDataCollection = GetRowDataCollection(sheetName, data);
        //         dataBySheets.Add(sheetName, rowDataCollection);
        //     }
        //
        //
        //     return dataBySheets;
        // }

        // static List<RowData> GetRowDataCollection(string sheetName, IEnumerable<BarkBookmark> data) {
        //     var result = new List<RowData>();
        //
        //     Dictionary<string, List<string>> phrasesByBookmarks = new();
        //     foreach (var bookmark in data) {
        //         phrasesByBookmarks.TryAdd(bookmark.name, new List<string>());
        //
        //         foreach (var collection in bookmark.barkTextCollections) {
        //             bool tagMatch = collection.tag.TrimTag() == sheetName;
        //             
        //             if (!tagMatch) {
        //                 continue;
        //             }
        //
        //             foreach (var phrase in collection.phrases) {
        //                 phrasesByBookmarks[bookmark.name].Add(phrase);
        //             }
        //         }
        //     }
        //     
        //     foreach (var bookmark in phrasesByBookmarks) {
        //         var rowData = new RowData();
        //         rowData.Values = new List<CellData> {
        //             new () {
        //                 UserEnteredValue = new ExtendedValue {
        //                     StringValue = bookmark.Key
        //                 }
        //             },
        //             new() {
        //                 UserEnteredValue = new ExtendedValue {
        //                     StringValue = string.Join("\n\n", bookmark.Value)
        //                 }
        //             }
        //         };
        //         result.Add(rowData);
        //     }
        //
        //     return result;
        // }

        // static string TrimTag(this string tag) {
        //     if (string.IsNullOrEmpty(tag)) {
        //         return tag;
        //     }
        //
        //     int colonIndex = tag.IndexOf(':');
        //     return colonIndex >= 0 ? tag.Substring(colonIndex + 1, tag.Length - colonIndex - 1) : tag;
        // }
    }
}