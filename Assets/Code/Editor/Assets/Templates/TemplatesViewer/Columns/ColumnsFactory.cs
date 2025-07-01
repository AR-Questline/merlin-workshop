using System;
using Awaken.TG.Editor.Assets.Templates.TemplatesViewer.Columns.ColumnTypes;
using Sirenix.Utilities;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Assets.Templates.TemplatesViewer.Columns {
    public static class ColumnsFactory {

        public static MultiColumnHeaderState.Column CreateForType(TemplatesViewerCategory category, Type type)  {
            return CreateColumn(category, ScriptableObject.CreateInstance(type) as TemplatesViewerColumn, null, 200);
        }

        public static MultiColumnHeaderState.Column[] GetDefault(TemplatesViewerCategory category) {
            return new[] {
                CreateColumn(category, ScriptableObject.CreateInstance<ObjectTemplateColumn>(), "Asset", 200),
                CreateColumn(category, CreateMetadataColumnData("verifiedArt"), "Art", 30),
                CreateColumn(category, CreateMetadataColumnData("verifiedDesign"), "Design", 50),
                CreateColumn(category, CreateMetadataColumnData("notes"), "Notes", 150),
                CreateColumn(category, ScriptableObject.CreateInstance<DependenciesTemplateColumn>(), "Dependencies", 100)
            };
        }

        public static MultiColumnHeaderState.Column CreateCopy(MultiColumnHeaderState.Column original,
            TemplatesViewerCategory category) {
            TemplatesViewerColumn originalData = category.Owner.GetColumn(original.userData);
            TemplatesViewerColumn copyData = Object.Instantiate(originalData);
            
            copyData.Owner = category;
            category.Owner.AddColumn(copyData);
            MultiColumnHeaderState.Column newColumn = CopyColumn(original);
            newColumn.userData = copyData.ID;
            return newColumn;
        }

        static MultiColumnHeaderState.Column CopyColumn(MultiColumnHeaderState.Column original) {
            return new MultiColumnHeaderState.Column() {
                width = original.width,
                autoResize = original.autoResize,
                canSort = original.canSort,
                headerContent = new GUIContent(original.headerContent),
                maxWidth = original.maxWidth,
                minWidth = original.minWidth,
                sortedAscending = original.sortedAscending,
                allowToggleVisibility = original.allowToggleVisibility,
                contextMenuText = original.contextMenuText,
                headerTextAlignment = original.headerTextAlignment,
                sortingArrowAlignment = original.sortingArrowAlignment
            };
        }
        
        static MultiColumnHeaderState.Column CreateColumn(TemplatesViewerCategory category, TemplatesViewerColumn columnData, string name, float width) {
            columnData.Owner = category;
            category.Owner.AddColumn(columnData);
            string columnName = name.IsNullOrWhitespace() ? "Column" + columnData.ID : name;    
            
            var newColumn = new MultiColumnHeaderState.Column {
                headerTextAlignment = TextAlignment.Center,
                headerContent = new GUIContent(columnName),
                contextMenuText = columnName,
                userData = columnData.ID,
                sortedAscending = true,
                sortingArrowAlignment = TextAlignment.Right,
                width = width
            };
            return newColumn;
        }
        
        static MetadataTemplateColumn CreateMetadataColumnData(string type) {
            var newColumnData = ScriptableObject.CreateInstance<MetadataTemplateColumn>();
            newColumnData.MetadataType = type;
            return newColumnData;
        }
    }
}