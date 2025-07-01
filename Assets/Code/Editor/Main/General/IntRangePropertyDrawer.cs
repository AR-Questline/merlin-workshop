using Awaken.TG.Editor.Helpers;
using Awaken.TG.Main.General;
using Awaken.Utility.UI;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.General
{
    [CustomPropertyDrawer(typeof(IntRange))]
    public class IntRangePropertyDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);    
            // label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);    
            // remove indent
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;    
            // Calculate rects
            const int LabelWidth = 10;
            
            var propSize = (position.width - (LabelWidth + 1 + 1)) / 2f;
            PropertyDrawerRects fullRect = position;
            Rect lowRect = fullRect.AllocateLeft(propSize);
            fullRect.LeaveSpace(1);
            Rect labelRect =  fullRect.AllocateLeft(LabelWidth);
            fullRect.LeaveSpace(1);
            Rect highRect =  (Rect) fullRect;
            // draw fields
            EditorGUI.PropertyField(lowRect, property.FindPropertyRelative("low"), GUIContent.none);
            EditorGUI.LabelField(labelRect, "..");
            EditorGUI.PropertyField(highRect, property.FindPropertyRelative("high"), GUIContent.none);    
            // clean up
            EditorGUI.indentLevel = indent;    
            EditorGUI.EndProperty();
        }
    }
}
