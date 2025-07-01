using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Collections;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.Templates.TemplatesViewer.Columns.ColumnTypes {
    [DefaultTemplateColumnAttribute]
    public class MetadataTemplateColumn : TemplatesViewerColumn {

        static Dictionary<string, FieldInfo> s_metadataFields;
        static string[] s_metadataTypes;
        
        [SerializeField] string metadataType;

        public string MetadataType {
            get => metadataType;
            set => metadataType = value;
        }

        static MetadataTemplateColumn() {
            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            s_metadataFields = typeof(TemplateMetadata)
                .GetFields(bindingFlags)
                .ToDictionary(f => f.Name);
            s_metadataTypes = s_metadataFields.Keys.ToArray();
        }
        
        public override void DrawCell(Rect cellRect, TemplatesViewerTreeItem item) {
            if (!metadataType.IsNullOrWhitespace()) {
                FieldInfo field = s_metadataFields[metadataType];
                if (TryDrawMetadataField(cellRect, field, item.Template.Metadata, out object obj)) {
                    field.SetValue(item.Template.Metadata, obj);
                    EditorUtility.SetDirty(item.TemplateObject);
                }
            }        
        }
        
        bool TryDrawMetadataField(Rect cellRect, FieldInfo field, TemplateMetadata metadata, out object result) {
            if (typeof(bool).IsAssignableFrom(field.FieldType)) {
                var value = (bool)field.GetValue(metadata);
                result = EditorGUI.Toggle(cellRect,  value);
                return (bool)result != value;
            }

            if (typeof(string).IsAssignableFrom(field.FieldType)) {
                var value = field.GetValue(metadata);
                result = EditorGUI.TextField(cellRect, (string) value);
                return result != value;
            }

            result = null;
            EditorGUILayout.LabelField("Not supported metadata type");
            return false;
        }

        public override void OnGUI() {
            int index = s_metadataTypes.IndexOf(metadataType);
            int newIndex = EditorGUILayout.Popup("Metadata type", index, s_metadataTypes);
            if (newIndex != index && newIndex >= 0) {
                metadataType = s_metadataTypes[newIndex];
            }
        }

        public override object GetSortingObject(TemplatesViewerTreeItem item) {
            FieldInfo field = s_metadataFields[metadataType];
            return field.GetValue(item.Template.Metadata);
        }
    }
}