using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Awaken.Utility.Collections;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Assets.Templates.TemplatesViewer.Columns.ColumnTypes {
    public class GetComponentTemplateColumn : TemplatesViewerColumn {
        
        const string ComponentObject = "Component object";
        static IEnumerable<Type> s_monoBehaviours = TypeCache.GetTypesDerivedFrom<MonoBehaviour>();

        [SerializeField] List<PropertyDefinition> propertyDefinitions = new();
        
        string[] _componentFields = Array.Empty<string>();
        Dictionary<TemplatesViewerTreeItem, Dictionary<string, Component>> _cache = new();

        public override void DrawCell(Rect cellRect, TemplatesViewerTreeItem item) {
            int typeIndex = Owner.Types.IndexOf(item.Template.GetType().Name);
            if (typeIndex >= 0 && typeIndex < propertyDefinitions.Count 
                               && TryGetComponent(item, propertyDefinitions[typeIndex].ComponentName, out Component component)) {
                DrawProperty(cellRect, component, typeIndex);
            }
        }

        void DrawProperty(Rect cellRect, Component component, int typeIndex) {
            string propertyName = propertyDefinitions[typeIndex].PropertyName;
            if (propertyName != ComponentObject) {
                ColumnPropertyDrawer.DrawProperty(cellRect, component, propertyDefinitions[typeIndex]);
            } else {
                EditorGUI.ObjectField(cellRect, component, component.GetType(), false);
            }
        }

        bool TryGetComponent(TemplatesViewerTreeItem item, string type, out Component component) {
            if (item.Template is MonoBehaviour go) {
                if (_cache.TryGetValue(item, out Dictionary<string, Component> components)) {
                    if (components.TryGetValue(type, out component)) {
                        return component != null;
                    } 
                } else {
                    _cache[item] = new Dictionary<string, Component>();
                }
                component = go.GetComponent(type);
                _cache[item][type] = component;
                return component != null;
            }

            component = null;
            return false;
        }

        public override void OnGUI() {
            base.OnGUI();
            EditorGUILayout.LabelField("Components for types:");
            for (int i = 0; i < Owner.Types.Count; i++) {
                EditorGUILayout.LabelField(Owner.Types[i]);
                DrawForType(i);
            }
        }

        void DrawForType(int index) {
            CheckListSize(index);
            
            if (ComponentNameChanged(index)) {
                _componentFields = GetFields(propertyDefinitions[index].ComponentName);
            }

            int currentIndex = _componentFields.IndexOf(propertyDefinitions[index].PropertyName);
            int newIndex = EditorGUILayout.Popup(currentIndex, _componentFields);
            if (newIndex >= 0) {
                propertyDefinitions[index].PropertyName = _componentFields[newIndex];
            }
            
            propertyDefinitions[index].DrawerType = (ColumnDrawerType)EditorGUILayout.EnumPopup("Drawer type", propertyDefinitions[index].DrawerType);
        }

        bool ComponentNameChanged(int index) {
            EditorGUI.BeginChangeCheck();
            propertyDefinitions[index].ComponentName = EditorGUILayout.TextField("Component name", propertyDefinitions[index].ComponentName);
            return EditorGUI.EndChangeCheck();
        }
        
        string[] GetFields(string typeName) {
            Type type = s_monoBehaviours.FirstOrDefault(t => t.Name == typeName);

            if (type == null) {
                return Array.Empty<string>();
            }
            
            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            return new []{ComponentObject}
                .Concat(type.GetFields(bindingFlags).Select(f => f.Name))
                .ToArray();
        }
        
        void CheckListSize(int index) {
            while (propertyDefinitions.Count <= index) {
                propertyDefinitions.Add(new PropertyDefinition());
            }
        }
        
        public override object GetSortingObject(TemplatesViewerTreeItem item) {
            int typeIndex = Owner.Types.IndexOf(item.Template.GetType().Name);
            if (typeIndex >= 0 && typeIndex < propertyDefinitions.Count && item.Template is MonoBehaviour go) {
                var component = go.GetComponent(propertyDefinitions[typeIndex].ComponentName);
                if (component != null) {
                    if (propertyDefinitions[typeIndex].PropertyName == ComponentObject) {
                        return component;
                    }
                    FieldInfo field = component.GetType().GetField(propertyDefinitions[typeIndex].PropertyName);
                    if (field != null) {
                        return field.GetValue(component);
                    }
                }
            }
            return null;
        }

        public override void Refresh() {
            base.Refresh();
            _cache.Clear();
        }
        
        public override float GetRowHeight(TemplatesViewerTreeItem item) {
            int typeIndex = Owner.Types.IndexOf(item.Template.GetType().Name);
            if (typeIndex >= 0 && typeIndex < propertyDefinitions.Count) {
                if(TryGetComponent(item, propertyDefinitions[typeIndex].ComponentName, out Component component))
                {
                    return ColumnPropertyDrawer.GetPropertyHeight(component, propertyDefinitions[typeIndex]);
                }
            }

            return base.GetRowHeight(item);
        }
    }
}