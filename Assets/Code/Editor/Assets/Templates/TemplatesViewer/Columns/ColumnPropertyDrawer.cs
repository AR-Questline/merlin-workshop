using System;
using System.Collections;
using System.Reflection;
using Awaken.TG.Editor.Assets.Templates.TemplatesViewer.Columns.ColumnTypes;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Assets.Templates.TemplatesViewer.Columns {
    
    public enum ColumnDrawerType {
        Drawer,
        Unity,
        Text
    }
    
    public static class ColumnPropertyDrawer {

        public const float DefaultRowHeight = 20;
        public const float RowOffset = 3;
        public static void DrawProperty(Rect cellRect, Object obj, PropertyDefinition propertyDefinition) {

            if (propertyDefinition.DrawerType == ColumnDrawerType.Text) {
                DrawAsText(cellRect, obj, propertyDefinition.PropertyName);
            } else {
                DrawPropertyTypes(cellRect, obj, propertyDefinition);
            }
        }

        static void DrawAsText(Rect cellRect, Object obj, string propertyName) {
            FieldInfo field = obj.GetType().GetField(propertyName);
            if (field != null) {
                object fieldValue = field.GetValue(obj);

                if (fieldValue != null) {
                    if (fieldValue is ICollection collection) {
                        const string separator = ", ";
                        string result = string.Empty;
                        foreach (object subObject in collection) {
                            result += subObject + separator;
                        }

                        int lastSeparator = result.LastIndexOf(separator, StringComparison.Ordinal);
                        if (lastSeparator >= 0) {
                            result = result[..lastSeparator];
                        }
                        EditorGUI.LabelField(cellRect, result);
                    } else {
                        EditorGUI.LabelField(cellRect, fieldValue.ToString());
                    }
                }
            }
        }

        static void DrawPropertyTypes(Rect cellRect, Object obj, PropertyDefinition propertyDefinition) {
            SerializedObject serializedObject = new SerializedObject(obj);

            SerializedProperty property = serializedObject.FindProperty(propertyDefinition.PropertyName);
            if (property == null) {
                EditorGUI.LabelField(cellRect, $"Property \"{propertyDefinition.PropertyName}\" not found");
            } else {
                DrawPropertyForType(cellRect, property, serializedObject, propertyDefinition.DrawerType);
            }
        }

        public static float GetPropertyHeight(Object obj, PropertyDefinition propertyDefinition) {
            if (propertyDefinition.DrawerType != ColumnDrawerType.Text) {
                SerializedObject serializedObject = new SerializedObject(obj);
                SerializedProperty property = serializedObject.FindProperty(propertyDefinition.PropertyName);
                if (property != null) {
                    return GetSerializedPropertyHeight(property);
                }
            }
            return DefaultRowHeight;
        }
        
        static float GetSerializedPropertyHeight(SerializedProperty property) {
            float result = 0;
            if (property.isArray && property.propertyType != SerializedPropertyType.String) {
                for (int i = 0; i < property.arraySize; i++) {
                    SerializedProperty subProperty = property.GetArrayElementAtIndex(i);
                    result += GetSerializedPropertyHeight(subProperty);
                }
                return result;
            } else {
                return EditorGUI.GetPropertyHeight(property, true)+RowOffset;
            }
        }


        static void DrawPropertyForType(Rect cellRect, SerializedProperty property, SerializedObject serializedObject, ColumnDrawerType drawerType) {
            if (property.isArray && property.propertyType != SerializedPropertyType.String) {
                float currentHeight = RowOffset;
                for (int i = 0; i < property.arraySize; i++) {
                    SerializedProperty subProperty = property.GetArrayElementAtIndex(i);
                    float height = GetSerializedPropertyHeight(subProperty);
                    DrawPropertyForType(new Rect(cellRect.x, cellRect.y + currentHeight, cellRect.width, height), 
                        subProperty,serializedObject,drawerType);
                    currentHeight += height;
                }
            } else {
                switch (drawerType) {
                    case ColumnDrawerType.Unity:
                        DrawPropertyField(property, cellRect);
                        serializedObject.ApplyModifiedProperties();
                        break;
                    case ColumnDrawerType.Drawer:
                        EditorGUI.PropertyField(cellRect, property, GUIContent.none, true);
                        serializedObject.ApplyModifiedProperties();
                        break;
                }
            }
        }


        static void DrawPropertyField(SerializedProperty property, Rect cellRect) {
            if (property.isArray && property.propertyType != SerializedPropertyType.String) {
                for (int i = 0; i < property.arraySize; i++) {
                    SerializedProperty subProperty = property.GetArrayElementAtIndex(i);
                    DrawPropertyField(subProperty, new Rect(cellRect.x, cellRect.y + i*DefaultRowHeight, cellRect.width, DefaultRowHeight));
                }

                return;
            }
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    property.intValue = EditorGUI.IntField(cellRect, property.intValue);
                    break;
                case SerializedPropertyType.Float:
                    property.floatValue = EditorGUI.FloatField(cellRect, property.floatValue);
                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = EditorGUI.Toggle(cellRect, property.boolValue);
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = EditorGUI.TextField(cellRect, property.stringValue);
                    break;
                case SerializedPropertyType.Color:
                    property.colorValue = EditorGUI.ColorField(cellRect, property.colorValue);
                    break;
                case SerializedPropertyType.ObjectReference:
                case SerializedPropertyType.Generic:
                    EditorGUI.ObjectField(cellRect, property);
                    break;
                case SerializedPropertyType.LayerMask:
                    property.intValue = EditorGUI.MaskField(cellRect, property.intValue, property.enumNames);
                    break;
                case SerializedPropertyType.Enum:
                    property.enumValueIndex = EditorGUI.Popup(cellRect, property.enumValueIndex, property.enumNames);
                    break;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = EditorGUI.Vector2Field(cellRect, property.name, property.vector2Value);
                    break;
                case SerializedPropertyType.Vector3:
                    property.vector3Value = EditorGUI.Vector3Field(cellRect, property.name, property.vector3Value);
                    break;
                case SerializedPropertyType.Vector4:
                    property.vector4Value = EditorGUI.Vector4Field(cellRect, property.name, property.vector4Value);
                    break;
                case SerializedPropertyType.Rect:
                    property.rectValue = EditorGUI.RectField(cellRect, property.rectValue);
                    break;
                case SerializedPropertyType.AnimationCurve:
                    property.animationCurveValue = EditorGUI.CurveField(cellRect, property.animationCurveValue);
                    break;
                case SerializedPropertyType.Bounds:
                    property.boundsValue = EditorGUI.BoundsField(cellRect, property.boundsValue);
                    break;
                case SerializedPropertyType.Vector2Int:
                    property.vector2IntValue = EditorGUI.Vector2IntField(cellRect, property.name, property.vector2IntValue);
                    break;
                case SerializedPropertyType.Vector3Int:
                    property.vector3IntValue = EditorGUI.Vector3IntField(cellRect, property.name, property.vector3IntValue);
                    break;
                case SerializedPropertyType.RectInt:
                    property.rectIntValue = EditorGUI.RectIntField(cellRect, property.rectIntValue);
                    break;
                case SerializedPropertyType.BoundsInt:
                    property.boundsIntValue = EditorGUI.BoundsIntField(cellRect, property.boundsIntValue);
                    break;
                default:
                    EditorGUI.LabelField(cellRect, "Chosen field type is not supported, contact with your favourite programmer");
                    break;
            }
        }
    }
    
    [Serializable]
    public class PropertyDefinition {
        [SerializeField] string propertyName;
        [SerializeField] string componentName;
        [SerializeField] ColumnDrawerType drawerType;

        public string PropertyName {
            get => propertyName;
            set => propertyName = value;
        }

        public string ComponentName {
            get => componentName;
            set => componentName = value;
        }

        public ColumnDrawerType DrawerType {
            get => drawerType;
            set => drawerType = value;
        }
    }
}