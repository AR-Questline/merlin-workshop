using System;
using Awaken.TG.Editor.Assets.Templates.TemplatesViewer.Columns.ColumnTypes;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.Templates.TemplatesViewer.Columns {
    public class TemplateColumnsManagerWindow : EditorWindow {
        static readonly Vector2 WindowSize = new Vector2(300, 650);
        TemplatesViewerCategory _category;
        TemplatesViewerWindow _parent;
        Vector2 _scrollPos;

        public static void OpenWindow(TemplatesViewerCategory category, TemplatesViewerWindow parent) {
            TemplateColumnsManagerWindow window = GetWindow<TemplateColumnsManagerWindow>();
            window.Init(category, parent);
        }

        void Init(TemplatesViewerCategory category, TemplatesViewerWindow parent) {
            minSize = WindowSize;
            titleContent = new GUIContent("Columns Manager");
            _category = category;
            _parent = parent;
            
            ShowModal();
        }
        
        void OnGUI() {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            DrawColumnsDefinitions();
            AddColumnMenu.DrawAddColumnMenu(_category);

            EditorGUILayout.EndScrollView();
        }

        void OnDestroy() {
            _parent.RefreshTree();
        }

        void DrawColumnsDefinitions() {
            foreach (MultiColumnHeaderState.Column column in _category.Columns.columns) {
                DrawColumnDefinition(_category.Owner.GetColumn(column.userData), column);
            }
        }

        void DrawColumnDefinition(TemplatesViewerColumn columnData, MultiColumnHeaderState.Column column) {
            GUILayout.BeginVertical("HelpBox");
            EditorGUILayout.BeginHorizontal();
            columnData.Foldout = EditorGUILayout.Foldout(columnData.Foldout, $"{column.headerContent.text} ({columnData.GetType().Name})");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X")) {
                _category.RemoveColumn(column);
            }
            EditorGUILayout.EndHorizontal();
            if(columnData.Foldout) {
                EditorGUILayout.BeginVertical();
                EditorGUI.indentLevel++;
                DrawColumnData(columnData, column);
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }
        

        void DrawColumnData(TemplatesViewerColumn columnData, MultiColumnHeaderState.Column column) {
            GUILayout.BeginVertical("HelpBox");
            column.headerContent.text = EditorGUILayout.TextField("Title", column.headerContent.text);
            column.contextMenuText = column.headerContent.text;
            GUILayout.EndVertical();

            columnData.OnGUI();
        }

    }
}