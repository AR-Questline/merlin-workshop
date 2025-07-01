using System;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.Utility.UI;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Stories.Steps.Helpers {
    [CustomPropertyDrawer(typeof(Context))]
    public class ContextPropertyDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            // label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            // remove indent
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            // Calculate rects
            PropertyDrawerRects rects = new PropertyDrawerRects(position);
            Rect contextTypeRect = rects.AllocateLeft(80);
            Rect templateRect = rects.AllocateLeft(90);
            // get props
            var contextType = property.FindPropertyRelative("type");
            var templateRef = property.FindPropertyRelative("template");
            // draw fields
            EditorGUI.PropertyField(contextTypeRect, contextType, GUIContent.none);
            var contextValues = (ContextType[])Enum.GetValues(typeof(ContextType));
            int contextIndex = contextType.enumValueIndex;
            if (contextIndex >= 0 && contextIndex < contextValues.Length) {
                ContextType type = contextValues[contextIndex];
                if (type == ContextType.Quest) {
                    EditorGUI.PropertyField(templateRect, templateRef, GUIContent.none);
                }
            }
            
            // clean up
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}