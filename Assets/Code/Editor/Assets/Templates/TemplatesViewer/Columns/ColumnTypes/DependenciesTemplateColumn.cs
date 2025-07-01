using UnityEditor;
using UnityEditor.Search;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.Templates.TemplatesViewer.Columns.ColumnTypes {
    [DefaultTemplateColumnAttribute]
    public class DependenciesTemplateColumn : TemplatesViewerColumn {
        
        static GUIContent s_dependencyIcon;

        public override void DrawCell(Rect cellRect, TemplatesViewerTreeItem item) {
            if (s_dependencyIcon == null) {
                s_dependencyIcon = EditorGUIUtility.IconContent("ViewToolZoom");
            }

            if (GUI.Button(cellRect, s_dependencyIcon)) {
                FindReferences(item.TemplateObject);
            }
        }

        public override object GetSortingObject(TemplatesViewerTreeItem item) {
            return 0;
        }

        private static void FindReferences(Object obj) {
            var path = AssetDatabase.GetAssetPath(obj);
            var searchContext = SearchService.CreateContext(new[] { "dep", "scene", "asset", "adb" }, $"ref=\"{path}\"");
            SearchService.ShowWindow(searchContext, "References", saveFilters: false);
        }
    }
}