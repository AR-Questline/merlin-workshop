using Awaken.TG.Main.General;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.General {
    [CustomPropertyDrawer(typeof(ConditionalInt))]
    public class ConditionalIntPropertyDrawer : PropertyDrawer {
        const int ToggleSpace = 25;
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            // label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            // remove indent
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            // Calculate rects
            Rect enableRect = new Rect(position.x, position.y, ToggleSpace, position.height);
            Rect valueRect = new Rect(position.x + ToggleSpace, position.y, position.width-ToggleSpace, position.height);
            // draw fields
            var enableProperty = property.FindPropertyRelative(nameof(ConditionalInt.enable));
            EditorGUI.PropertyField(enableRect, enableProperty, GUIContent.none);
            if (enableProperty.boolValue) {
                EditorGUI.PropertyField(valueRect, property.FindPropertyRelative(nameof(ConditionalInt.value)), GUIContent.none);
            }
            // clean up
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}