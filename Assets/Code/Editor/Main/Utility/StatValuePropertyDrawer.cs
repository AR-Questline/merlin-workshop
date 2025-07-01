using Awaken.TG.Editor.Helpers;
using Awaken.TG.Main.Utility;
using Awaken.Utility.UI;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Utility {
    [CustomPropertyDrawer(typeof(StatValue))]
    public class StatValuePropertyDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            // label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            // remove indent
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            // Calculate rects
            PropertyDrawerRects rects = new PropertyDrawerRects(position);
            Rect leftRect = rects.AllocateLeft(50);
            rects.AllocateLeft(10);
            Rect rightRect = rects.AllocateLeft(110);
            // draw fields
            EditorGUI.PropertyField(leftRect, property.FindPropertyRelative("value"), GUIContent.none);
            EditorGUI.PropertyField(rightRect, property.FindPropertyRelative("type"), GUIContent.none);
            // clean up
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();            
        }
    }
}