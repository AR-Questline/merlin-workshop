using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Assets.Templates.TemplatesViewer.Columns;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.Templates.TemplatesViewer {
    public class TemplatesViewerWindow : EditorWindow {
        static readonly string s_windowTitle = "Templates Viewer";

        public static readonly IEnumerable<Type> TemplateTypes = TypeCache.GetTypesDerivedFrom<ITemplate>();

        [SerializeField] TreeViewState treeViewState;
        [SerializeField] TemplatesViewerConfig config;
       
        GUIContent _settingsIcon;
        GUIContent _removeIcon;
        GUIContent _refreshIcon;
        TemplatesViewerTree _treeView;
        SearchField _searchField;
        string[] _tabs;

        int TabColumns => Mathf.Max(1, (int) (position.width / 200));
        int TabRows => Mathf.CeilToInt((_tabs.Length * 1f) / TabColumns);
        Rect SearchBarRect => new(4, TabRows * 21 + 3, position.width - 8, 20);
        Rect TreeViewRect => new(4, TabRows * 21 + 23, position.width-8, position.height - TabRows * 21 - 50);
        
        [MenuItem("TG/Design/Templates Viewer")]
        static void OpenWindow() {
            TemplatesViewerWindow window = GetWindow<TemplatesViewerWindow>();
            TemplatesViewerConfig config = TemplatesViewerConfig.LoadOrCreate(TemplatesViewerConfig.DefaultPath);
            window.Init(config);
        }

        void Init (TemplatesViewerConfig config) {
            this.config = config;
            
            minSize = new Vector2(300, 450);
            titleContent = new GUIContent(s_windowTitle);
            
            _settingsIcon = EditorGUIUtility.IconContent("SettingsIcon");
            _removeIcon = EditorGUIUtility.IconContent("TreeEditor.Trash");
            _refreshIcon = EditorGUIUtility.IconContent("RotateTool");
            treeViewState ??= new TreeViewState();
            
            RefreshTabs();
            if (config.SelectedTab < 0 && _tabs.Length > 1) {
                config.SelectedTab = 0;
            }
            RefreshTree();
        }

        public void RefreshTree() {
            if (config.SelectedTab >= 0) {
                var header = GetTabColumns();
            
                _treeView = new TemplatesViewerTree(treeViewState, new DraggableMultiColumnHeader(header), GetTabTemplates(), config);
            }
        }

        void RefreshTabs() {
            _tabs = config.Categories
                .Select(c => c.Name)
                .Concat(new []{"+"})
                .ToArray();
        }

        void OnGUI() {
            GUILayout.BeginVertical();
            DrawTabs();

            if (config.SelectedTab >= 0) {
                if (_treeView == null) {
                    RefreshTree();
                }
                DrawSearchBar();
                _treeView.OnGUI(TreeViewRect);
                DrawBottomButtons();
            }
            GUILayout.EndVertical();
        }

        void DrawSearchBar() {
            _searchField ??= new SearchField();
            _treeView.searchString = _searchField.OnGUI(SearchBarRect, _treeView.searchString);
        }

        void DrawTabs() {
            if (config.SelectedTab >= _tabs.Length) {
                config.SelectedTab = 0;
            }
            int selectedTab = GUILayout.SelectionGrid(config.SelectedTab, _tabs, TabColumns);
            if (selectedTab != config.SelectedTab) {
                if (selectedTab >= _tabs.Length - 1) {
                    var newCategory = new TemplatesViewerCategory();
                    TemplatesCategoryWindow.OpenWindow(newCategory, () => AddNewCategory(newCategory), config);
                } else {
                    config.SelectedTab = selectedTab;
                    RefreshTree();
                }
            }
        }

        void AddNewCategory(TemplatesViewerCategory category) {
            config.AddCategory(category);
            config.SelectedTab = config.Categories.Count() - 1;
            category.Init(config);
            RefreshTabs();
            RefreshTree();
        }

        IEnumerable<ITemplate> GetTabTemplates() {
            var result = new List<ITemplate>();
            if (config.SelectedTab < 0) {
                return result;
            }
            
            foreach (string type in config.CurrentCategory().Types) {
                result.AddRange(GetTemplatesForType(type));
            }

            return result.Where(t => config.CurrentCategory().FilterTemplate(t));
        }

        List<ITemplate> GetTemplatesForType(string typeName) {
            var result = new List<ITemplate>();
            if (TryGetTemplatesType(typeName, out Type type)) {
                TemplatesSearcher.FindAllOfType(type, result);
            }

            return result;
        }

        bool TryGetTemplatesType(string typeName, out Type type) {
            type = TemplateTypes.FirstOrDefault(t => t.Name == typeName);
            return type != null;
        }
        
        MultiColumnHeaderState GetTabColumns() {
            if (config.SelectedTab < 0) {
                return null;
            }

            return config.CurrentCategory().Columns;
        }
        
        void DrawBottomButtons() {
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_settingsIcon)) {
                EditCategoryBtn();
            }
            if (GUILayout.Button(_removeIcon)) {
                RemoveBtn();
            } 
            if (GUILayout.Button(_refreshIcon)) {
                RefreshBtn();
            } 
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Edit columns")) {
                EditColumnsBtn();
            }
            GUILayout.EndHorizontal();
        }

        void RemoveBtn() {
            if (EditorUtility.DisplayDialog("You sure?", "Do you want to delete this category?", "YEP", "NAH")) {
                config.RemoveCategory(config.SelectedTab);
                config.SelectedTab--;
                RefreshTabs();
                RefreshTree();
            }
        }

        void RefreshBtn() {
            config.CurrentCategory().RefreshColumns();
            RefreshTabs();
            RefreshTree();
        }

        void EditCategoryBtn() {
            TemplatesCategoryWindow.OpenWindow(config.CurrentCategory(), () => {
                RefreshTabs();
                RefreshTree();
            }, config);
            GUIUtility.ExitGUI();
        }

        void EditColumnsBtn() {
            TemplateColumnsManagerWindow.OpenWindow(config.CurrentCategory(), this);
            GUIUtility.ExitGUI();
        }
        
    }
}