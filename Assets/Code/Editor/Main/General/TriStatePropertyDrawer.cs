using Awaken.TG.Main.General;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.General {
    [CustomPropertyDrawer(typeof(TriState))]
    public class TriStatePropertyDrawer : PropertyDrawer {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel (position, GUIUtility.GetControlID (FocusType.Passive), label);

            SerializedProperty state = property.FindPropertyRelative("state");
            int selected = state.enumValueIndex;

            EditorGUI.BeginChangeCheck();
            selected = EditorGUI.Popup(position, selected, state.enumDisplayNames);
            if (EditorGUI.EndChangeCheck()) {
                state.enumValueIndex = selected;
            }
            EditorGUI.EndProperty();
        }
    }
}