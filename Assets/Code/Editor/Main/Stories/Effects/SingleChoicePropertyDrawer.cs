using Awaken.TG.Editor.Helpers;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.Utility.UI;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Stories.Effects
{
    [CustomPropertyDrawer(typeof(SingleChoice))]
    public class SingleChoicePropertyDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            // label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            // remove indent
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            // Calculate rects
            PropertyDrawerRects rects = new PropertyDrawerRects(position);
            Rect textRect = rects.AllocateLeft(170);
            Rect labelRect = rects.AllocateLeft(15);
            Rect chapterRect = rects.AllocateLeft(120);
            // get props
            var text = property.FindPropertyRelative("text");
            var chapter = property.FindPropertyRelative("targetChapter");
            // draw fields
            EditorGUI.PropertyField(textRect, text, GUIContent.none);
            EditorGUI.LabelField(labelRect, "\u25b6");
            EditorGUI.PropertyField(chapterRect, chapter, GUIContent.none);
            // clean up
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}
