using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Awaken.TG.Editor.Utility.Paths;
using Awaken.TG.Main.Templates.Specs;
using Awaken.Utility.GameObjects;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using ProgressBar = Awaken.TG.Editor.SimpleTools.ProgressBar;

namespace Awaken.TG.Editor.Debugging.GUIDSearching {
    public class GUIDSearchWindow : OdinEditorWindow {
        const string OtherGUIDToolsGroup = "Other GUID Tools";
        const string OtherGUIDToolsButtonsGroup = OtherGUIDToolsGroup+"/Buttons";
        
        public static readonly Type[] DesiredDockTypes = {typeof(GUIDSearchWindow), typeof(UnusedSearchWindow), typeof(RichEnumSearchWindow), typeof(IdOverrideSearchWindow)};
        
        [ShowInInspector, PropertyOrder(-10)]
        public string LastBake => GUIDCache.Instance?.LastBake;

        public static void OpenWindow() {
            var window = GetWindow<GUIDSearchWindow>(DesiredDockTypes);
            window.Show();
        }

        [MenuItem("TG/Assets/Find by GUID", priority = -100)]
        static void CreateWindow() {
            var window = CreateWindow<GUIDSearchWindow>(DesiredDockTypes);
            window.Show();
        }
        
        [MenuItem("Assets/TG/Find by GUID")]
        static void FindByGUID() {
            var window = GetWindow<GUIDSearchWindow>(DesiredDockTypes);
            window.Show();
            window.SearchGUID();
        }

        [Title("Input")]
        [InlineButton(nameof(PastePhrase), "Paste"), OnValueChanged(nameof(SetTargetObject)), Indent]
        public string phrase;
        
        [ShowIf("@this." + nameof(selectedObject) + " != null"), PropertySpace(SpaceAfter = 5, SpaceBefore = 0), Indent]
        public Object selectedObject;

        [FoldoutGroup("Settings")] public bool ignoreIrrelevant = true;
        [FoldoutGroup("Settings")] public int filesWaves = 50;
        [FoldoutGroup("Settings")] public int threads = 1;
        
        [Title("Output")]
        [ShowInInspector, TableList(IsReadOnly = true, AlwaysExpanded = true), PropertyOrder(1), Space(10), Indent]
        List<SearchResultObject> _foundUsages = new();

        // == Lifecycle
        
        protected override void Initialize() {
            Selection.selectionChanged += UpdateSelection;
            threads = SystemInfo.processorCount * 2;
            GUIDCache.Load();
            UpdateSelection();
        }

        protected override void OnEnable() {
            base.OnEnable();
            GUIDCache.Load();
        }

        protected override void OnDestroy() {
            GUIDCache.Unload();
            Selection.selectionChanged -= UpdateSelection;
        }

        // == Searching Interface

        [HorizontalGroup("Buttons"), PropertySpace(SpaceBefore = 5)]
        [Button(ButtonSizes.Medium, ButtonStyle.CompactBox, Icon = SdfIconType.Search)]
        void SearchGUID() {
            _foundUsages.Clear();
            foreach (string path in GUIDCache.Instance.GetDependent(phrase, ignoreIrrelevant)) {
                var so = new SearchResultObject(path);
                if (so.asset != GUIDCache.Instance) {
                    _foundUsages.Add(so);
                }
            }
            if (selectedObject != null) {
                _foundUsages = _foundUsages.Where(f => f.asset != selectedObject).ToList();
            }
        }

        [HorizontalGroup("Buttons", marginLeft: 14), PropertySpace(SpaceBefore = 5)]
        [Button(ButtonSizes.Medium, ButtonStyle.CompactBox)]
        void SearchPhrase() {
            if (!EditorUtility.DisplayDialog("Search Phrase", "This operation is very expensive (1-2h). Are you sure?", "Yes", "No")) {
                return;
            }
            Search();
        }
        
        [HorizontalGroup("Buttons"), PropertySpace(SpaceBefore = 5)]
        [Button(ButtonSizes.Medium, ButtonStyle.CompactBox)]
        [LabelText("Search SpecID in Scene")]
        void SearchSpecIDInScene() {
            _foundUsages.Clear();
            for (int i = 0; i < SceneManager.sceneCount; i++) {
                foreach (SceneSpec spec in GameObjects.FindComponentsByTypeInScene<SceneSpec>(SceneManager.GetSceneAt(i), true, 1000)) {
                    if (spec.SceneId.FullId == phrase) {
                        _foundUsages.Add(new SearchResultObject(spec.gameObject.PathInSceneHierarchy(), spec));
                    }
                }
            }
        }

        [BoxGroup(OtherGUIDToolsGroup), HorizontalGroup(OtherGUIDToolsButtonsGroup), PropertyOrder(-1)]
        [Button(ButtonSizes.Small)]
        void OpenUnusedSearchWindow() {
            UnusedSearchWindow.OpenWindow();
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
        
        void Search() {
            using var progressBar = ProgressBar.Create($"Searching phrase {phrase}");
            
            _foundUsages.Clear();

            var validPaths = GUIDSearchUtils.GetValidPaths();

            ParallelComputeData<string, string> guidSearching = new() {
                progressBar = progressBar.TakePart(0.6f, "Checking files for phrase"),
                waveCount = filesWaves,
                threadCount = threads,
                func = FindPhrase(phrase),
            };
            if (!GUIDSearchUtils.TryComputeParallel(guidSearching, validPaths, out List<string> pathsWithGuid)) {
                EditorUtility.ClearProgressBar();
                return;
            }

            var part = progressBar.TakePart(0.1f, "Saving results");
            for (int i = 0; i < pathsWithGuid.Count; i++) {
                part.Display((float)i/pathsWithGuid.Count);
                var searchObject = new SearchResultObject(pathsWithGuid[i]);
                if (searchObject.asset != selectedObject) {
                    _foundUsages.Add(searchObject);
                }
            }
        }

        // == Find files to search

        public static (string[], bool) FilterFiles(int waves, int threads, string[] allFiles) {
            return RunInWaves(waves, threads, allFiles, FindValidFiles, "Searching phrase", "Obtaining paths");
        }

        static IEnumerable<string> FindValidFiles(IEnumerable<string> allFiles) {
            return allFiles.Where(GUIDSearchUtils.IsValidPath);
        }

        // == Find files with GUID

        static Func<IEnumerable<string>, IEnumerable<string>> FindPhrase(string phrase) {
            return pathsToSearch => {
                List<string> founded = new List<string>();
                foreach (string path in pathsToSearch) {
                    var file = File.ReadAllText(path);
                    if (file.Contains(phrase)) {
                        founded.Add(path);
                    }
                }
                return founded;
            };
        }

        // === GUID/Searchbox operations
        
        void PastePhrase() {
            phrase = GUIUtility.systemCopyBuffer;
            SetTargetObject();
        }

        void SetTargetObject() {
            var assetPath = AssetDatabase.GUIDToAssetPath(phrase);
            selectedObject = AssetDatabase.LoadMainAssetAtPath(assetPath);
        }

        void UpdateSelection() {
            selectedObject = Selection.activeObject;
            UpdateGUID();
        }

        void UpdateGUID() {
            if (selectedObject == null) {
                phrase = "(Null)";
            } else {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(selectedObject, out phrase, out long _);
            }
        }

        // === Helper class
        public class SearchResultObject {
#pragma warning disable 169, 414
            [InlineEditor, TableColumnWidth(500), ShowInInspector, ReadOnly] public Object asset;
            [ShowInInspector, ReadOnly] readonly string _assetPath;
            [TableColumnWidth(10), ShowInInspector, ReadOnly] bool _fromMeta;
#pragma warning restore 169, 414
            
            public string AssetPath => _assetPath;

            public SearchResultObject(string path) {
                if (path.EndsWith("meta")) {
                    path = path[..^5];
                    _fromMeta = true;
                }
                _assetPath = path.StartsWith("Assets") ? path : PathUtils.FilesystemToAssetPath(path);
                asset = AssetDatabase.LoadAssetAtPath<Object>(_assetPath);
            }

            public SearchResultObject(string path, Object asset) {
                _assetPath = path;
                this.asset = asset;
            }
        }
        
        // == Helpers
        static (string[], bool) RunInWaves(int waves, int threads, string[] pathsToFeed, Func<IEnumerable<string>, IEnumerable<string>> func, string progressTitle, string progressDesc) {
            var computeData = new ParallelComputeData<string, string>() {
                progressBar = ProgressBar.Create(progressTitle, progressDesc),
                waveCount = waves,
                threadCount = threads,
                func = func,
            };
            bool cancelled = !GUIDSearchUtils.TryComputeParallel(computeData, pathsToFeed, out List<string> validFiles);

            return (validFiles.ToArray(), cancelled);
        }
    }
}