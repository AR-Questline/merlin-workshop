using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Utility.Animations.FightingStyles;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Awaken.TG.Editor.Debugging.GUIDSearching {
    public class AlwaysLoadedSearchWindow : GUIDSearchWindowBase {
        static string[] s_alwaysLoadedDirectories = {
            "Assets\\Resources",
        };

        static string[] s_sceneNames = {
            "ApplicationScene.unity",
            "BuildInitialScene.unity",
        };

        static List<string> s_projectSettingsData;
        
        [Title("Input")]
        [InlineButton(nameof(PastePhrase), "Paste"), OnValueChanged(nameof(SetTargetObject)), Indent]
        public string phrase;
        
        [ShowIf("@this." + nameof(selectedObject) + " != null"), PropertySpace(SpaceAfter = 5, SpaceBefore = 0), Indent]
        public Object selectedObject;

        [FoldoutGroup("Settings")] public bool ignoreIrrelevant = true;
        [FoldoutGroup("Settings")] public bool performDeepSearch;
        [FoldoutGroup("Settings")] public int filesWaves = 50;
        [FoldoutGroup("Settings")] public int threads = 1;
        
        [Title("Output")]
        [ShowInInspector, TableList(IsReadOnly = true, AlwaysExpanded = true), PropertyOrder(1), Space(10), Indent]
        List<SearchResultObject> _foundUsages = new();
        
        List<string> _checkedPaths = new();
        
        protected override bool ShowAlwaysLoadedSearchButton => false;
        
        public static void OpenWindow() {
            var window = GetWindow<AlwaysLoadedSearchWindow>(GUIDSearchWindow.DesiredDockTypes);
            window.Show();
        }
        
        [MenuItem("TG/Assets/Find always loaded", priority = -100)]
        static void CreateWindow() {
            var window = CreateWindow<AlwaysLoadedSearchWindow>(GUIDSearchWindow.DesiredDockTypes);
            window.Show();
        }
        
        // == Lifecycle
        
        protected override void Initialize() {
            Selection.selectionChanged += UpdateSelection;
            threads = SystemInfo.processorCount * 2;
            GUIDCache.Load();
            LoadProjectSettingsData();
            UpdateSelection();
        }

        protected override void OnEnable() {
            base.OnEnable();
            GUIDCache.Load();
            LoadProjectSettingsData();
        }

        protected override void OnDestroy() {
            GUIDCache.Unload();
            CleanupProjectSettingsData();
            Selection.selectionChanged -= UpdateSelection;
        }

        // == Searching Interface
        
        [HorizontalGroup("Buttons"), PropertySpace(SpaceBefore = 5)]
        [Button(ButtonSizes.Medium, ButtonStyle.CompactBox, Icon = SdfIconType.Search)]
        void Search() {
            _foundUsages.Clear();
            _checkedPaths.Clear();
            
            foreach ((string path, string objectPath) in GUIDCache.Instance.GetAlwaysLoadedRoot(phrase, ignoreIrrelevant, performDeepSearch, _checkedPaths, null)) {
                var so = new SearchResultObject(path, objectPath);
                if (so.asset != GUIDCache.Instance) {
                    _foundUsages.Add(so);
                }
            }
            if (selectedObject != null) {
                _foundUsages = _foundUsages.Where(f => f.asset != selectedObject).ToList();
            }
            
            var mainPath = AssetDatabase.GUIDToAssetPath(phrase);
            var mainResult = IsAlwaysLoaded(phrase, mainPath);
            if (IsAlwaysLoaded(mainResult)) {
                var so = new SearchResultObject(mainPath, mainResult.ToString());
                if (so.asset != GUIDCache.Instance) {
                    _foundUsages.Add(so);
                }
            }
        }

        public static AlwaysLoadedResult IsAlwaysLoaded(string guid, string path) {
            if (IsInProjectSettings(AssetDatabase.AssetPathToGUID(path))) {
                return AlwaysLoadedResult.InProjectSettings;
            }
            
            foreach (var directory in s_alwaysLoadedDirectories) {
                if (path.StartsWith(directory)) {
                    return AlwaysLoadedResult.AlwaysLoaded;
                }
            }

            if (path.EndsWith(".unity")) {
                foreach (var scene in s_sceneNames) {
                    if (path.EndsWith(scene)) {
                        return AlwaysLoadedResult.AlwaysLoaded;
                    }
                }
                return AlwaysLoadedResult.MostLikelyNot;
            } else if (path.EndsWith(".asset")) {
                if (AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(path) != null) {
                    return AlwaysLoadedResult.AlwaysLoaded;
                }
                if (AssetDatabase.LoadAssetAtPath<StoryGraph>(path) != null) {
                    return AlwaysLoadedResult.MostLikelyNot;
                }
                if (AssetDatabase.LoadAssetAtPath<NpcFightingStyle>(path) != null) {
                    return AlwaysLoadedResult.MostLikelyNot;
                }
            } else if (path.EndsWith(".prefab")) {
                if (AssetDatabase.LoadAssetAtPath<NpcTemplate>(path) != null) {
                    return AlwaysLoadedResult.MostLikelyNot;
                }
                if (AssetDatabase.LoadAssetAtPath<ItemTemplate>(path) != null) {
                    return AlwaysLoadedResult.MostLikelyNot;
                }
            }

            return AlwaysLoadedResult.RequireFurtherChecks;
        }

        static void LoadProjectSettingsData() {
            s_projectSettingsData = new List<string>();
            string[] settingsFiles = System.IO.Directory.GetFiles("ProjectSettings", "*.asset");

            foreach (var file in settingsFiles) {
                var asset = AssetDatabase.LoadAllAssetsAtPath(file);
                foreach (var a in asset) {
                    if (a == null) continue;
                    SerializedObject so = new SerializedObject(a);
                    SerializedProperty prop = so.GetIterator();
                    while (prop.NextVisible(true)) {
                        if (prop.propertyType == SerializedPropertyType.ObjectReference) {
                            var obj = prop.objectReferenceValue;
                            if (obj != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out string guid, out _)) {
                                s_projectSettingsData.Add(guid);
                            }
                        }
                    }
                }
            }
        }

        static void CleanupProjectSettingsData() {
            s_projectSettingsData.Clear();
        }
        
        static bool IsInProjectSettings(string guid) {
            return s_projectSettingsData.Contains(guid);
        } 

        public enum AlwaysLoadedResult : byte {
            InProjectSettings,
            AlwaysLoaded,
            RequireFurtherChecks,
            MostLikelyNot,
        }

        static bool IsAlwaysLoaded(AlwaysLoadedResult result) {
            return result is AlwaysLoadedResult.InProjectSettings or AlwaysLoadedResult.AlwaysLoaded;
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
        public class SearchResultObject : GUIDSearchWindow.SearchResultObject {
#pragma warning disable 169, 414
            [ShowInInspector, ReadOnly] readonly string _objectsPath;
#pragma warning restore 169, 414
            public SearchResultObject(string path, string objectsPath) : base(path) {
                _objectsPath = objectsPath;
            }
        }
    }
}