using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Awaken.TG.Editor.Assets.Templates.TemplatesViewer.Columns.ColumnTypes;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.Templates.TemplatesViewer.Columns {
    public static class AddColumnMenu {
        static IEnumerable<Type> s_columnTypes;
        static IEnumerable<Type> s_defaultColumns;
        
        static AddColumnMenu(){
            LoadTemplateTypes();
        }

        static void LoadTemplateTypes() {
            var columnType = typeof(TemplatesViewerColumn);
            var allTypes = TypeCache.GetTypesDerivedFrom<TemplatesViewerColumn>()
                .Where(p => !p.IsAbstract)
                .OrderBy(t => t.Name);
            s_defaultColumns = allTypes.Where(p => p.GetCustomAttribute<DefaultTemplateColumnAttribute>() != null);
            s_columnTypes = allTypes.Except(s_defaultColumns);
        }
        
        public static void DrawAddColumnMenu(TemplatesViewerCategory owner) {
            if (GUILayout.Button("+")) {
                GenericMenu menu = new GenericMenu();

                foreach (Type columnType in s_columnTypes) {
                    AddMenuItemForType(menu, columnType, owner);
                }
                menu.AddSeparator("");
                foreach (TemplatesViewerCategory otherCategory in owner.Owner.Categories) {
                    DrawCopyColumns(menu, owner, otherCategory);
                }
                menu.AddSeparator("");
                foreach (Type columnType in s_defaultColumns) {
                    AddMenuItemForDefaultType(menu, columnType, owner);
                }
                menu.ShowAsContext();
            }
        }

        static void DrawCopyColumns(GenericMenu menu, TemplatesViewerCategory owner, TemplatesViewerCategory other) {
            foreach (MultiColumnHeaderState.Column column in other.Columns.columns) {
                AddMenuItemForCloneColumn(menu, column, other, owner);
            }
        }
        
        static void AddMenuItemForType(GenericMenu menu, Type type, TemplatesViewerCategory owner) {
            menu.AddItem(new GUIContent(type.Name.Replace("TemplateColumn", "")), false, () => OnTypeSelected(type, owner));
        }
        
        static void AddMenuItemForCloneColumn(GenericMenu menu, MultiColumnHeaderState.Column column, TemplatesViewerCategory other, TemplatesViewerCategory owner) {
            menu.AddItem(new GUIContent($"Copy/{other.Name}/{column.headerContent.text}"), false, () => OnCopySelected(owner, column));
        }
        
        static void AddMenuItemForDefaultType(GenericMenu menu, Type type, TemplatesViewerCategory owner) {
            menu.AddItem(new GUIContent($"Default/{type.Name.Replace("TemplateColumn", "")}"), false, () => OnTypeSelected(type, owner));
        }
        
        static void OnTypeSelected(Type type, TemplatesViewerCategory owner) {
            owner.AddColumn(ColumnsFactory.CreateForType(owner, type));
        }

        static void OnCopySelected(TemplatesViewerCategory owner, MultiColumnHeaderState.Column original) {
            owner.AddColumn(ColumnsFactory.CreateCopy(original, owner));
        }
    }
}