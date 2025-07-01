using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Awaken.TG.Editor.Helpers.Tags;
using Awaken.TG.Editor.SimpleTools;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Debugging;
using Cysharp.Threading.Tasks;
using UnityEditor;

namespace Awaken.TG.Editor.Main.AI.Barks {
    public class BarksImporter {
        const string SheetIgnoreMarker = "_";
        const string ProgressTitle = "Import from google sheets";
        
        readonly List<BarkBookmark> _bookmarks;
        readonly List<string> _currentTags;
        readonly Dictionary<string, List<BarkTextCollection>> _importedData;
        
        public BarksImporter(List<BarkBookmark> bookmarks) {
            _bookmarks = bookmarks;
            _currentTags = TagsCache.Get(TagsCategory.Barks).entries.SelectMany(e => e.values).Select(w => w.value).ToList();
            _importedData = new Dictionary<string, List<BarkTextCollection>>();
        }
        
    public async UniTask ImportData(string link)
    {
        EditorUtility.DisplayProgressBar(ProgressTitle, null, 0);

        // if (!ARGoogleSheets.Initialized)
        // {
        //     EditorUtility.DisplayProgressBar(ProgressTitle, "Authorizing...", 0);
        //     ARGoogleSheets.Initialize();
        // }
        //
        // await UniTask.DelayFrame(1);
        //
        // var handle = ARGoogleSheets.GetSpreadsheetByLink(link);
        // EditorUtility.DisplayProgressBar(ProgressTitle, "Fetching spreadsheet...", 0.2f);
        //
        // while (!handle.IsCompleted)
        // {
        //     await UniTask.DelayFrame(1);
        // }
        //
        // var spreadsheet = handle.Result;
        // if (spreadsheet == null)
        // {
        //     EditorUtility.ClearProgressBar();
        //     EditorUtility.DisplayDialog("Failed to fetch spreadsheet",
        //         "Fetching spreadsheet from given link failed, make sure you have provided proper link.", "OK");
        //     return;
        // }
        //
        // EditorUtility.DisplayProgressBar("Google Sheets Export", "Checking tags data...", 0.4f);
        //
        // CheckForNewTags(spreadsheet.Sheets, out bool cancel);
        // if (cancel)
        // {
        //     EditorUtility.ClearProgressBar();
        //     return;
        // }
        //
        // EditorUtility.DisplayProgressBar(ProgressTitle, "Converting data...", 0.6f);
        // await UniTask.DelayFrame(1);
        //
        // CreateImportedData(spreadsheet);
        // MargeImportedDataToProject();
        //
        // EditorUtility.DisplayProgressBar(ProgressTitle, "Saving assets...", 0.95f);
        // await UniTask.DelayFrame(1);
        //
        // AssetDatabase.SaveAssets();
        // AssetDatabase.Refresh();
        //
        // EditorUtility.DisplayProgressBar(ProgressTitle, "Import completed.", 1);
        // await UniTask.DelayFrame(1);
        // EditorUtility.ClearProgressBar();
    }
        
        // void CheckForNewTags(IList<Sheet> sheets, out bool cancel) {
        //     cancel = false;
        //     var possibleNewTags = new List<string>();
        //
        //     foreach (var sheet in sheets) {
        //         var sheetName = sheet.Properties.Title;
        //         if (sheetName.StartsWith(SheetIgnoreMarker)) {
        //             continue;
        //         }
        //
        //         bool isSheetNameTag = _currentTags.Contains(sheetName);
        //         
        //         if (!isSheetNameTag) {
        //             possibleNewTags.Add(sheetName);
        //         }
        //     }
        //
        //     if (!possibleNewTags.Any()) {
        //         return;
        //     }
        //
        //     StringBuilder sb = new();
        //     sb.AppendLine("It appears that there are new sheets in the google sheets. " +
        //                   "In editor each sheet is treated as a tag. " +
        //                   "New sheets are:");
        //     sb.AppendLine();
        //     possibleNewTags.ForEach(t=>sb.AppendLine(t));
        //     sb.AppendLine();
        //     sb.AppendLine("Do you want to add them to the project as tags now, or ignore those sheets and continue without them?");
        //
        //     int option = EditorUtility.DisplayDialogComplex("Tags miss-match!", sb.ToString(), "Add tags to project",
        //         "Ignore and continue", "Cancel");
        //     switch (option) {
        //         case 0: // add tags
        //             possibleNewTags.ForEach(AddNewTag);
        //             _currentTags.AddRange(possibleNewTags);
        //             break;
        //         case 1: // ignore
        //             break;
        //         default: // cancel
        //             cancel = true;
        //             break;
        //     }
        // }
        
        // void CreateImportedData(Spreadsheet spreadsheet) {
        //     foreach (var sheet in spreadsheet.Sheets) {
        //         HandleSheet(sheet);
        //     }
        // }
        
        // void HandleSheet(Sheet sheet) {
        //     if (sheet.Properties.Title.StartsWith(SheetIgnoreMarker)) {
        //         return;
        //     }
        //
        //     foreach (GridData gridData in sheet.Data) {
        //         HandleGridData(gridData, sheet.Properties.Title);
        //     }
        // }
        // void HandleGridData(GridData gridData, string currentSheetName) {    
        //     if (gridData == null) {
        //         return;
        //     }
        //
        //     foreach (RowData rowData in gridData.RowData) {
        //         HandleRowData(rowData, currentSheetName);
        //     }
        // }
        // void HandleRowData(RowData rowData, string currentSheetName) {
        //     if (rowData?.Values == null || rowData.Values.Count == 0 || rowData.Values.Count < 2 ||
        //         rowData.Values[0].FormattedValue == null) {
        //         return;
        //     }
        //
        //     var key = Regex.Replace(rowData.Values[0].FormattedValue, @"\s+", ""); // first column is the key (bookmark name)
        //     var value = rowData.Values[1]?.FormattedValue?.Trim() ?? string.Empty; // second column is the value (phrases)
        //     var phrases = Regex.Split(value, @"\r?\n", RegexOptions.None).Where(s=>s!=string.Empty).ToArray(); // phrases are separated by double new line
        //
        //     foreach (var bookmark in _bookmarks) {
        //         bool isKeyBookmark = string.Equals(key, bookmark.name, StringComparison.CurrentCultureIgnoreCase);
        //         if (isKeyBookmark) {
        //             var newCollection = new BarkTextCollection();
        //             newCollection.phrases.AddRange(phrases);
        //
        //             if (TryGetTagFromSheetName(currentSheetName, out string fullTag)) {
        //                 newCollection.tag = fullTag;
        //             } else {
        //                 continue;
        //             }
        //             
        //             if (_importedData.TryGetValue(bookmark.name, out var importedCollections)) {
        //                 if(importedCollections.Any(c => AreCollectionsEqual(c, newCollection))) {
        //                     continue;
        //                 }
        //             }
        //
        //             _importedData.TryAdd(bookmark.name, new List<BarkTextCollection>());
        //             _importedData[bookmark.name].Add(newCollection);
        //         }
        //     }
        // }
        
        void MargeImportedDataToProject() {
            foreach (var bookmark in _bookmarks) {
                // get imported data for current bookmark
                if (!_importedData.TryGetValue(bookmark.name, out var importedBarkTextCollection)) {
                    continue;
                }

                // remove unused collections
                for (int i = bookmark.barkTextCollections.Count - 1; i >= 0; i--) {
                    var savedCollection = bookmark.barkTextCollections[i];
                    bool shouldBeRemoved = true;
                    foreach (var collection in importedBarkTextCollection) {
                        if(!string.IsNullOrEmpty(savedCollection.tag) && collection.tag == savedCollection.tag && savedCollection.phrases.Count > 0) {
                            shouldBeRemoved = false;
                            break;
                        }
                    }

                    if (shouldBeRemoved) {
                        bookmark.barkTextCollections.RemoveAt(i);
                        EditorUtility.SetDirty(bookmark);
                    }
                }

                // add new imported collections
                foreach (BarkTextCollection newCollection in importedBarkTextCollection) {
                    // do not add collection if it (somehow) has no tag
                    if (string.IsNullOrEmpty(newCollection.tag)) {
                        continue;
                    }

                    var alreadyExistingCollection = bookmark.barkTextCollections.FirstOrDefault(c => c.tag == newCollection.tag);
                    
                    if(alreadyExistingCollection != null) {
                        if(newCollection.phrases.Count == 0) {
                            bookmark.barkTextCollections.Remove(alreadyExistingCollection);
                        } else {
                            alreadyExistingCollection.phrases = newCollection.phrases;
                        }
                    } else if(newCollection.phrases.Count > 0) {
                        bookmark.barkTextCollections.Add(newCollection);
                    }
                    
                    EditorUtility.SetDirty(bookmark);
                }
            }
        }

        bool TryGetTagFromSheetName(string sheetName, out string fullTag) {
            fullTag = string.Empty;
            bool isSheetNameTag = _currentTags.Contains(sheetName);
            if (isSheetNameTag) {
                fullTag = TagsCategory.Barks + ":" + sheetName;
                return true;
            }

            return false;
        }
        
        static bool AreCollectionsEqual(BarkTextCollection a, BarkTextCollection b) {
            return a.phrases.SequenceEqual(b.phrases) && a.tag == b.tag;
        }

        static void AddNewTag(string tagValue) {
            TagsEditing.SaveTag(TagsCategory.Barks + ":" + tagValue, TagsCategory.Barks);
        }
    }
}