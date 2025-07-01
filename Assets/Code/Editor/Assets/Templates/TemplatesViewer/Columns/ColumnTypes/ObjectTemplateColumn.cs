using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.Templates.TemplatesViewer.Columns.ColumnTypes {
    [DefaultTemplateColumnAttribute]
    public class ObjectTemplateColumn : TemplatesViewerColumn {

        public override void DrawCell(Rect cellRect, TemplatesViewerTreeItem item) {
            EditorGUI.ObjectField(cellRect, item.TemplateObject, item.TemplateObject.GetType(), false);
        }

        public override object GetSortingObject(TemplatesViewerTreeItem item) {
            return item.TemplateObject.name;
        }
    }
}