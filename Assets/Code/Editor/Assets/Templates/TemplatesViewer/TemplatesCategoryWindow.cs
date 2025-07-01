using System;
using System.Linq;
using Awaken.TG.Main.Templates;
using Pathfinding.Util;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.Templates.TemplatesViewer {
    public class TemplatesCategoryWindow : EditorWindow {
        
        static readonly Vector2 WindowSize = new Vector2(300, 450);

        static string[] s_templateTypes = TypeCache.GetTypesDerivedFrom<ITemplate>()
            .Select(t => t.Name)
            .OrderBy(t => t)
            .ToArray();

        TemplatesViewerCategory _category;
        bool _wasNameEdited;
        Action _onEditDone;
        TemplatesViewerConfig _config;

        public static void OpenWindow(TemplatesViewerCategory category, Action onEditDone, TemplatesViewerConfig config) {
            TemplatesCategoryWindow window = GetWindow<TemplatesCategoryWindow>();
            window.Init(category, onEditDone, config);
        }

        void Init(TemplatesViewerCategory category, Action onEditDone, TemplatesViewerConfig config) {
            minSize = WindowSize;
            titleContent = new GUIContent("New Templates Category");
            _category = category;
            _onEditDone = onEditDone;
            _config = config;
            
            ShowModal();
        }

        void OnGUI() {
            GUILayout.BeginVertical();
            DrawTypes();
            DrawName();
            DrawNameFilter();
            DrawTags();
            DrawSaveBtn();
            GUILayout.EndVertical();
        }

        void DrawTypes() {
            for (int i = 0; i < _category.Types.Count; i++) {
                GUILayout.BeginHorizontal();
                DrawType(i);
                if (GUILayout.Button("X", GUILayout.Width(18), GUILayout.Height(18))) {
                    _category.Types.RemoveAt(i);
                    i--;
                }
                GUILayout.EndHorizontal();
            }
            DrawAddTypeMenu();
        }

        void DrawType(int typeIndex) {
            int index = Array.IndexOf(s_templateTypes, _category.Types[typeIndex]);
            int newIndex = EditorGUILayout.Popup(index, s_templateTypes);
            if (newIndex != index && newIndex >= 0) {
                _category.Types[typeIndex] = s_templateTypes[newIndex];
            }
        }
        
        public void DrawAddTypeMenu() {
            if (GUILayout.Button("+"))
            {
                var menu = new GenericMenu();

                foreach (string type in s_templateTypes) {
                    AddMenuItemForType(menu, type);
                }
                menu.ShowAsContext();
            }
        }
        
        void AddMenuItemForType(GenericMenu menu, string type)
        {
            menu.AddItem(new GUIContent(type), false, () => AddType(type));
        }

        void AddType(string type) {
            _category.Types.Add(type);
            if (!_wasNameEdited && (_category.Name.IsNullOrWhitespace() || _category.Types.Count == 1)) {
                _category.Name = type;
            }
        }


        void DrawName() {
            GUILayout.Space(20);
            string newName = EditorGUILayout.TextField("Category name", _category.Name);
            if (newName != _category.Name) {
                _category.Name = newName;
                _wasNameEdited = true;
            }
            GUILayout.Space(20);
        }

        void DrawNameFilter() {
            _category.NameFilter = EditorGUILayout.TextField("Name filter", _category.NameFilter);
        }

        void DrawTags() {
            EditorGUILayout.LabelField("Tag filters");
            for (int i = 0; i < _category.TagFilters.Count; i++) {
                _category.TagFilters[i] = EditorGUILayout.TextField(_category.TagFilters[i]);
                if (_category.TagFilters[i].IsNullOrWhitespace()) {
                    _category.TagFilters.RemoveAt(i);
                    i--;
                    GUI.FocusControl(null);
                }
            }
            string newTag = EditorGUILayout.TextField(string.Empty);
            if (!newTag.IsNullOrWhitespace()) {
                _category.TagFilters.Add(newTag);
            }
        }

        void DrawSaveBtn() {
            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Save") && Validate()) {
                Close();
                _onEditDone?.Invoke();
                EditorUtility.SetDirty(_config);
            }
            GUILayout.EndHorizontal();
        }

        bool Validate() {
            if (_category.Types.Count == 0) {
                return ShowValidationError("Types list is empty!");
            }

            foreach (string type in _category.Types) {
                if (type.IsNullOrWhitespace()) {
                    return ShowValidationError("Types contains empty entries!");
                }
            }

            if (_category.Name.IsNullOrWhitespace()) {
                return ShowValidationError("Name is empty!");
            }

            return true;
        }

        bool ShowValidationError(string message) {
            EditorUtility.DisplayDialog("Invalid entry", message, "Ok");
            return false;
        }
    }
}