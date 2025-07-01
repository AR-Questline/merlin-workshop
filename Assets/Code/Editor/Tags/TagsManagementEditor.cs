using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Editor.Helpers.Tags;
using Awaken.Utility.Editor.UTK;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Awaken.TG.Editor.Tags {
    public class TagsManagementSave : AssetModificationProcessor {
        static string[] OnWillSaveAssets(string[] paths) {
            TagsCache.SaveAll();
            return paths;
        }
    }
    
    public class TagsManagementEditor : EditorWindowPresenter<TagsManagementEditor> {
        [SerializeField, Required] VisualTreeAsset tagsTablePrototype;
        VisualElement _scrollContainer;
        ToolbarSearchField _searchField;
        Toolbar _toolbar;
        EditorAutosaveTick _autosave;
        List<TagsSection> _sections;
        
        public TagsManagementEditor() {
            WindowName = "Tags Management";
        }

        [MenuItem("TG/Design/Tags Management")]
        public static void ShowWindow() {
            GetWindow();
        }

        public override void CreateGUI() {
            // always call base.CreateGUI() first to properly setup the window
            base.CreateGUI();
            SetupTagsSections();
            SetupToolbar();
            _autosave = new EditorAutosaveTick(TagsCache.SaveAll);
        }

        protected override void CacheVisualElements(VisualElement windowRoot) {
            _scrollContainer = windowRoot.Q<VisualElement>("all-tags-container");
            _toolbar = windowRoot.Q<Toolbar>();
            _searchField = _toolbar.Q<ToolbarSearchField>();
        }
        
        void SetupTagsSections() {
            _sections = new List<TagsSection>();
            
            foreach (var cache in TagsCache.LoadAll()) {
                TagsSection tagsSection = new(tagsTablePrototype.Instantiate().Q<MultiColumnTreeView>("tags-table"), cache);
                _sections.Add(tagsSection);
                _searchField.RegisterValueChangedCallback(tagsSection.Filter);
            }

            foreach (var foldout in _sections.OrderByDescending(valueTuple => valueTuple.Table.GetTreeCount()).Select(valueTuple => valueTuple.Foldout)) {
                _scrollContainer.Add(foldout);
            }
        }

        void SetupToolbar() {
            Button refreshButton = _toolbar.Q<Button>("refresh-button");
            refreshButton.clicked += () => {
                _scrollContainer.Clear();
                _searchField.value = string.Empty;
                _sections = null;
                AssetDatabase.Refresh();
                TagsCache.ResetCache();
                SetupTagsSections();
            };

            Button expandButton = _toolbar.Q<Button>("expand-all");
            expandButton.clicked += () => SetExpandCollapse(true);

            Button collapseButton = _toolbar.Q<Button>("collapse-all");
            collapseButton.clicked += () => SetExpandCollapse(false);
        }
            
        void SetExpandCollapse(bool expand) {
            _sections.ForEach(section => {
                section.Foldout.value = expand;
                if (expand) {
                    section.Table.ExpandAll();
                } else {
                    section.Table.CollapseAll();
                }
            });
        }
        
        void OnDestroy() {
            _autosave?.Dispose();
        }
    }
}
