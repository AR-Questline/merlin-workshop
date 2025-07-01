using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Awaken.TG.Main.Stories.Actors;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEditor;

namespace Awaken.TG.Editor.Main.AI.Barks {
    public static class BookmarksAvailabilityImporter {
        const string ProgressTitle = "Import from google sheets";
        static Dictionary<string, List<string>> s_availableBookmarksByActors = new();

        public static void ImportBookmarksAvailability() {
            GoogleSheetLinkWindow.ShowWindow(OnImportAccepted);
        }

        static async void OnImportAccepted(string url) {
            if (string.IsNullOrWhiteSpace(url)) {
                return;
            }
            await ImportData(url);
        }

        public static async UniTask ImportData(string link) {
            EditorUtility.DisplayProgressBar(ProgressTitle, null, 0);

            // if (!ARGoogleSheets.Initialized) {
            //     EditorUtility.DisplayProgressBar(ProgressTitle, "Authorizing...", 0);
            //     ARGoogleSheets.Initialize();
            // }
            //
            // await UniTask.DelayFrame(1);
            //
            // var handle = ARGoogleSheets.GetSpreadsheetByLink(link);
            // EditorUtility.DisplayProgressBar(ProgressTitle, "Fetching spreadsheet...", 0.2f);
            //
            // while (!handle.IsCompleted) {
            //     await UniTask.DelayFrame(1);
            // }
            //
            // var spreadsheet = handle.Result;
            // if (spreadsheet == null) {
            //     EditorUtility.ClearProgressBar();
            //     EditorUtility.DisplayDialog("Failed to fetch spreadsheet",
            //         "Fetching spreadsheet from given link failed, make sure you have provided proper link.", "OK");
            //     return;
            // }
            //
            // EditorUtility.DisplayProgressBar("Google Sheets Export", "Parsing data...", 0.4f);
            //
            // CreateImportedData(spreadsheet);
            // SetAvailabilityPropertyInActors();
            //
            // EditorUtility.DisplayProgressBar(ProgressTitle, "Saving actors prefab...", 0.99f);
            // await UniTask.DelayFrame(1);
            //
            // EditorUtility.SetDirty(ActorsRegister.Get);
            // AssetDatabase.SaveAssetIfDirty(ActorsRegister.Get);
            //
            // EditorUtility.DisplayProgressBar(ProgressTitle, "Import completed.", 1);
            // await UniTask.DelayFrame(1);
            // EditorUtility.ClearProgressBar();
        }

        // static void CreateImportedData(Spreadsheet spreadsheet) {
        //     var sheet = spreadsheet.Sheets[0];
        //     if (sheet == null) {
        //         Log.Important?.Error("Spreadsheet has no sheets!");
        //         return;
        //     }
        //
        //     var bookmarksInProject = BarksConfig.instance.Bookmarks.ToList();
        //     
        //     var data = sheet.Data;
        //     var rows = data[0].RowData;
        //
        //     
        //     s_availableBookmarksByActors = new ();
        //     
        //     var firstRow = rows[0];
        //     for (int ri = 1; ri < rows.Count; ri++) {
        //         var currentRowValues = rows[ri].Values;
        //         string actorName = currentRowValues[1].FormattedValue; // second column is actor name
        //         if (string.IsNullOrWhiteSpace(actorName)) {
        //             continue;
        //         }
        //         
        //         s_availableBookmarksByActors[actorName] = new List<string>();
        //         
        //         for(int ci = 3; ci < currentRowValues.Count; ci++) { 
        //             var cell = currentRowValues[ci].FormattedValue.Trim();
        //             if (string.IsNullOrEmpty(cell) || cell.ToLower().Trim() == "no") {
        //                 continue;
        //             }
        //             
        //             string bookmarkNameCandidate = firstRow.Values[ci].FormattedValue; // first row is bookmark name
        //             var sanitizedBookmarkNameCandidate = Regex.Replace(bookmarkNameCandidate, @"\s+", "").Trim();
        //             
        //             if (!bookmarksInProject.Contains(sanitizedBookmarkNameCandidate)) {
        //                 continue;
        //             }
        //             
        //             s_availableBookmarksByActors[actorName].Add(sanitizedBookmarkNameCandidate);
        //         }
        //     }
        // }

        // static void SetAvailabilityPropertyInActors() {
        //     ActorsRegister actorsRegister = ActorsRegister.Get;
        //     
        //     foreach(var pair in s_availableBookmarksByActors) {
        //         var actorName = pair.Key;
        //         var availableBookmarks = pair.Value;
        //
        //         var actorSpecs = actorsRegister.AllActors.Where(a => a.name == actorName).ToArray();
        //         if (!actorSpecs.Any()) {
        //             Log.Important?.Error($"Actor {actorName} not found in Actors Register.");
        //             continue;
        //         }
        //
        //         foreach(var actorSpec in actorSpecs) {
        //             actorSpec.AvailableBookmarks = availableBookmarks.ToArray();
        //         }
        //     }
        // }
    }
}