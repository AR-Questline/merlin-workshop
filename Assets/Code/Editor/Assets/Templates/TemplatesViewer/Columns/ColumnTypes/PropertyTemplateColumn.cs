using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Awaken.Utility.Collections;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.Templates.TemplatesViewer.Columns.ColumnTypes {

    
    public class PropertyTemplateColumn : TemplatesViewerColumn {
        static Dictionary<string, string[]> s_templatesFieldsMap = new();

        [SerializeField] List<PropertyDefinition> propertyDefinitions = new();

        public override void DrawCell(Rect cellRect, TemplatesViewerTreeItem item) {

            int typeIndex = Owner.Types.IndexOf(item.Template.GetType().Name);
            if (typeIndex >= 0 && typeIndex < propertyDefinitions.Count) {
                PropertyDefinition propertyDefinition = propertyDefinitions[typeIndex];
                ColumnPropertyDrawer.DrawProperty(cellRect, item.TemplateObject, propertyDefinition);
            }
        }

        public override void OnGUI() {
            base.OnGUI();
            EditorGUILayout.LabelField("Properties for types:");
            for (int i = 0; i < Owner.Types.Count; i++) {
                DrawForType(i);
            }
        }

        void DrawForType(int index) {
            CheckListSize(index);
            string[] fields = GetFields(Owner.Types[index]);

            int currentIndex = fields.IndexOf(propertyDefinitions[index].PropertyName);
            int newIndex = EditorGUILayout.Popup(Owner.Types[index], currentIndex, fields);
            if (newIndex >= 0) {
                propertyDefinitions[index].PropertyName = fields[newIndex];
            }

            EditorGUI.indentLevel++;
            propertyDefinitions[index].DrawerType = (ColumnDrawerType)EditorGUILayout.EnumPopup("Drawer type", propertyDefinitions[index].DrawerType);
            EditorGUI.indentLevel--;
        }

        public static string[] GetFields(string typeName) {
            if (!s_templatesFieldsMap.ContainsKey(typeName)) {
                s_templatesFieldsMap[typeName] = GetFieldsForMap(typeName);
            }

            return s_templatesFieldsMap[typeName];
        }

        static string[] GetFieldsForMap(string typeName) {
            Type type = TemplatesViewerWindow.TemplateTypes.FirstOrDefault(t => t.Name == typeName);

            if (type == null) {
                return Array.Empty<string>();
            }
            
            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            return type.GetFields(bindingFlags)
                .Select(f => f.Name)
                .ToArray();
        }

        void CheckListSize(int index) {
            while (propertyDefinitions.Count <= index) {
                propertyDefinitions.Add(new PropertyDefinition());
            }
        }
        
        public override object GetSortingObject(TemplatesViewerTreeItem item) {
            int typeIndex = Owner.Types.IndexOf(item.Template.GetType().Name);
            if (typeIndex >= 0 && typeIndex < propertyDefinitions.Count) {
                FieldInfo field = item.Template.GetType().GetField(propertyDefinitions[typeIndex].PropertyName);
                if (field != null) {
                    return field.GetValue(item.Template);
                }
            }
            return null;
        }

        public override float GetRowHeight(TemplatesViewerTreeItem item) {
            int typeIndex = Owner.Types.IndexOf(item.Template.GetType().Name);
            if (typeIndex >= 0 && typeIndex < propertyDefinitions.Count) {
                return ColumnPropertyDrawer.GetPropertyHeight(item.TemplateObject, propertyDefinitions[typeIndex]);
            }

            return base.GetRowHeight(item);
        }
    }
}