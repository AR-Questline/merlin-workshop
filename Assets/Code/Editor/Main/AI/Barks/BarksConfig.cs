using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Stories.Actors;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Awaken.TG.Editor.Main.AI.Barks {
    public class BarksConfig : ScriptableSingleton<BarksConfig> {
        [SerializeField]
        List<BarkBookmark> bookmarks = new();
        
        static BarksConfig s_cache;
        
        public IEnumerable<string> Bookmarks => bookmarks.Select(b => b.name);

        public HashSet<string> GetTagsInUse() {
            HashSet<string> tagsInUse = new();
            foreach (var bookmark in bookmarks) {
                foreach(var barkTextCollection in bookmark.barkTextCollections) {
                    tagsInUse.Add(barkTextCollection.tag);
                }
            }
            return tagsInUse;
        }
        
        public Dictionary<string, List<string>> GetBarkPhrasesByBookmarks(ActorSpec actorSpec) {
            var tags = actorSpec.tags;
            var result = new Dictionary<string, List<string>>(bookmarks.Count);
            
            foreach(var bookmark in bookmarks) {
                var barks = new List<string>();
                foreach(var barkTextCollection in bookmark.barkTextCollections) {
                    bool isTagMatch = tags.Contains(barkTextCollection.tag);
                    
                    if (isTagMatch) {
                        barks.AddRange(barkTextCollection.phrases);
                    }
                }

                if (barks.Count > 0) {
                    result.Add(bookmark.name, barks);
                }
            }

            return result;
        }
        
        public void TrySyncGraphs() {
            var actorsPrefab = ActorsRegister.Get.gameObject;
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            string prefabName = actorsPrefab.name;
            
            if (prefabStage != null && prefabStage.assetPath == AssetDatabase.GetAssetPath(actorsPrefab) && prefabStage.scene.isDirty) {
                EditorUtility.DisplayDialog(
                    "Unsaved Changes",
                    $"It seems there is an opened prefab '{prefabName}' in prefab mode with unsaved changes. Please save the changes before syncing.",
                    "OK"
                );
                return;
            }
            
            SyncGraphs();
        }
        
        public void SyncGraphs() {
            var barkStoryGraphsSyncer = new BarkStoryGraphsSyncer(this);
            barkStoryGraphsSyncer.Sync();
            Log.Minor?.Info("Barks graphs synced.");
        }
        
        public void ImportFromGoogleSheet() {
            GoogleSheetLinkWindow.ShowWindow(OnImportAccepted);
        }
        
        public void ExportToGoogleSheet() {
            BarksExporter.ExportData(bookmarks);
        }
        
        async void OnImportAccepted(string link) {
            BarksImporter importer = new(bookmarks);
            await importer.ImportData(link);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }
}