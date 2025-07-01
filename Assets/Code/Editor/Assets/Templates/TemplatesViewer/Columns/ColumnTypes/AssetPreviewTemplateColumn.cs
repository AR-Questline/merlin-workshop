using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Awaken.TG.Assets;
using Awaken.Utility.Collections;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.Templates.TemplatesViewer.Columns.ColumnTypes {
    public class AssetPreviewTemplateColumn : TemplatesViewerColumn {

        const string NotFoundKey = "Not found";
        
        static readonly Type[] PreviewTypes = new[] {
            typeof(SpriteReference),
            typeof(ShareableSpriteReference),
            typeof(ARAssetReference),
            typeof(ShareableARAssetReference)
        };
        
        [SerializeField] List<string> propertyTypes = new();
        [SerializeField] int maxPreviewSize = 64;
        
        public override void DrawCell(Rect cellRect, TemplatesViewerTreeItem item) {
            if (TryGetProperty(item, out SerializedProperty property)){
                DrawPreview(cellRect, property);
            }        
        }

        bool TryGetProperty(TemplatesViewerTreeItem item, out SerializedProperty property) {
            property = null;
            int typeIndex = Owner.Types.IndexOf(item.Template.GetType().Name);
            if (typeIndex >= 0 && typeIndex < propertyTypes.Count) {
                string propertyName = propertyTypes[typeIndex];
                if (propertyName != NotFoundKey) {
                    property = item.SerializedObject.FindProperty(propertyName);
                    return property != null;
                }
            }

            return false;
        }

        void DrawPreview(Rect cellRect, SerializedProperty property) {
            // Texture2D preview = ShowAssetPreviewDrawer.GetPreviewTexture(property);
            // if (preview != null) {
            //     float width = cellRect.width > maxPreviewSize ? maxPreviewSize : cellRect.width;
            //     width = width > preview.width ? preview.width : width;
            //     float height = cellRect.height > maxPreviewSize ? maxPreviewSize : cellRect.height;
            //     height = height > preview.height ? preview.height : height;
            //     cellRect = new Rect(cellRect.position, new Vector2(width, height));
            //     EditorGUI.DrawPreviewTexture(cellRect, preview);
            // }
        }

        public override void OnGUI() {
            base.OnGUI();
            maxPreviewSize = EditorGUILayout.IntField("Max preview size", maxPreviewSize);
            EditorGUILayout.LabelField("Preview properties for types:");
            for (int i = 0; i < Owner.Types.Count; i++) {
                DrawForType(i);
            }
        }
        
        void DrawForType(int index) {
            CheckListSize(index);

            propertyTypes[index] = EditorGUILayout.TextField(Owner.Types[index], propertyTypes[index]);

            if (propertyTypes[index].IsNullOrWhitespace()) {
                propertyTypes[index] = FindDefaultProperty(index);
            }
        }
        
        void CheckListSize(int index) {
            while (propertyTypes.Count <= index) {
                propertyTypes.Add(string.Empty);
            }
        }

        string FindDefaultProperty(int index) {
            string categoryName = Owner.Types[index];
            Type type = TemplatesViewerWindow.TemplateTypes.FirstOrDefault(t => t.Name == categoryName);

            if (type != null) {
                const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
                var fields = type.GetFields(bindingFlags);
                foreach (FieldInfo fieldInfo in fields) {
                    if (PreviewTypes.Contains(fieldInfo.FieldType)) {
                        return fieldInfo.Name;
                    }
                }
            }

            return NotFoundKey;
        }

        public override float GetRowHeight(TemplatesViewerTreeItem item) {
            if (TryGetProperty(item, out SerializedProperty property)){
                // Texture2D preview = ShowAssetPreviewDrawer.GetPreviewTexture(property);
                // if (preview != null) {
                //     return preview.height > maxPreviewSize ? maxPreviewSize : preview.height;
                // }
            }
            return base.GetRowHeight(item);
        }

        public override object GetSortingObject(TemplatesViewerTreeItem item) {
            // if (TryGetProperty(item, out SerializedProperty property)) {
            //     Texture2D preview = ShowAssetPreviewDrawer.GetPreviewTexture(property);
            //     if (preview == null) return 0;
            //     return 1;
            // }

            return 0;
        }
    }
}