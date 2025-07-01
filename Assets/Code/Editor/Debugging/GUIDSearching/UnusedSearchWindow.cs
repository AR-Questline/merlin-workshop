using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Debugging.GUIDSearching {
    public class UnusedSearchWindow : OdinEditorWindow {
        const string OtherGUIDToolsGroup = "Other GUID Tools";
        const string OtherGUIDToolsButtonsGroup = OtherGUIDToolsGroup+"/Buttons";
        
        [ShowInInspector, PropertyOrder(-10)]
        public string LastBake => GUIDCache.Instance?.LastBake;
        
        public static void OpenWindow() {
            var window = GetWindow<UnusedSearchWindow>(GUIDSearchWindow.DesiredDockTypes);
            window.Show();
        }

        [MenuItem("Assets/TG/Find Unused Here", priority = -100)]
        static void FindByGUID() {
            var window = GetWindow<UnusedSearchWindow>(GUIDSearchWindow.DesiredDockTypes);
            window.Show();
            window.CheckAllObjectsInCurrentFolder();
        }
        
        // Variables
        
        static readonly MethodInfo GetActiveFolderPath = typeof(ProjectWindowUtil).GetMethod("GetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);

        [ShowInInspector, TableList(IsReadOnly = true, AlwaysExpanded = true), PropertyOrder(1), Space(10), HorizontalGroup(marginLeft: 14, PaddingRight = 0)]
        List<GUIDSearchWindow.SearchResultObject> _foundUnused = new();

        [ShowInInspector, Title("Search for Unused Assets"), Indent]
        int _selectedObjectsCount;

        // === Lifetime
        
        protected override void Initialize() {
            Selection.selectionChanged += UpdateSelection;
        }
        
        // === Buttons

        [HorizontalGroup("Buttons", marginLeft: 14), PropertySpace(SpaceBefore = 5)]
        [LabelText("In Current Folder")]
        [Button(ButtonSizes.Medium, ButtonStyle.CompactBox)]
        void CheckAllObjectsInCurrentFolder() {
            _foundUnused.Clear();
            string pathToCurrentFolder = GetActiveFolderPath.Invoke(null, Array.Empty<object>()) as string;
            
            FindUnusedWithinPath(pathToCurrentFolder);
        }
        
        [HorizontalGroup("Buttons"), PropertySpace(SpaceBefore = 5)]
        [LabelText("In Selected Objects")]
        [Button(ButtonSizes.Medium, ButtonStyle.CompactBox)]
        void CheckSelectionForUnused() {
            _foundUnused.Clear();
            if (Selection.objects.Length == 0) {
                return;
            }
            
            foreach (Object obj in Selection.objects) {
                string assetPath = AssetDatabase.GetAssetPath(obj);
                
                if (assetPath.IsNullOrWhitespace()) continue;
                
                if (!Path.HasExtension(assetPath)) {
                    FindUnusedWithinPath(assetPath);
                } else if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out var guid, out long _) 
                           && !guid.IsNullOrWhitespace() 
                           && GUIDCache.Instance.IsUnused(guid)) {
                    
                    _foundUnused.Add(new(assetPath));
                }
            }
        }
        
        [Button(ButtonSizes.Large), PropertyOrder(1), GUIColor(1f, 0.5f, 0.5f), PropertySpace, DisableIf("@this._foundUnused.Count == 0")]
        void DeleteFoundUnusedFiles() {
            foreach (GUIDSearchWindow.SearchResultObject unused in _foundUnused) {
                AssetDatabase.DeleteAsset(unused.AssetPath);
            }

            // Remove folders that became empty
            foreach (var directory in _foundUnused
                                         .Select(r => Path.GetDirectoryName(r.AssetPath))
                                         .Distinct()
                                         .OrderByDescending(s => Path.GetFullPath(s).Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Length)) {

                if (!directory.IsNullOrWhitespace() && 
                    Directory.GetFiles(directory).Length == 0 && 
                    Directory.GetDirectories(directory).Length == 0) {
                    AssetDatabase.DeleteAsset(directory);
                }
            }
            _foundUnused.Clear();
        }
        
        [BoxGroup(OtherGUIDToolsGroup), HorizontalGroup(OtherGUIDToolsButtonsGroup), PropertyOrder(-1)]
        [Button(ButtonSizes.Small)]
        void OpenGUIDSearchWindow() {
            GUIDSearchWindow.OpenWindow();
        }
        
        [HorizontalGroup(OtherGUIDToolsButtonsGroup), PropertyOrder(-1)]
        [Button(ButtonSizes.Small)]
        void OpenRichEnumSearchWindow() {
            RichEnumSearchWindow.OpenWindow();
        }
        
        [HorizontalGroup(OtherGUIDToolsButtonsGroup), PropertyOrder(-1)]
        [Button(ButtonSizes.Small)]
        void OpenIdOverrideSearchWindow() {
            IdOverrideSearchWindow.OpenWindow();
        }

        // === Helpers
        
        void FindUnusedWithinPath(string pathToCurrentFolder) {
            GUIDCache.Load();
            foreach (string file in GetFiles(pathToCurrentFolder)) {
                string guid = AssetDatabase.GUIDFromAssetPath(file).ToString();
                if (!guid.IsNullOrWhitespace() && GUIDCache.Instance.IsUnused(guid)) {
                    _foundUnused.Add(new GUIDSearchWindow.SearchResultObject(file));
                }
            }
            GUIDCache.Unload();
        }

        /// <summary>
        /// Recursively gather all files under the given path including all its subfolders.
        /// </summary>
        static IEnumerable<string> GetFiles(string path) {
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(path);
            while (queue.Count > 0) {
                path = queue.Dequeue();
                try {
                    foreach (string subDir in Directory.GetDirectories(path)) {
                        queue.Enqueue(subDir);
                    }
                } catch (Exception ex) {
                    Log.Important?.Error(ex.Message);
                }

                string[] files = null;
                try {
                    files = Directory.GetFiles(path);
                } catch (Exception ex) {
                    Log.Important?.Error(ex.Message);
                }

                if (files != null) {
                    for (int i = 0; i < files.Length; i++) {
                        yield return files[i];
                    }
                }
            }
        }

        void UpdateSelection() {
            _selectedObjectsCount = Selection.count;
        }
    }
}