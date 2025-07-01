using Awaken.TG.Main.Utility.Animations.ARTransitions;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Utility.Animations {
    [CustomPropertyDrawer(typeof(ARTurningAnimationOverrides))]
    public class ARTurningAnimationOverridesDrawer : PropertyDrawer
    { 
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var entriesProperty = GetEntriesProperty(property);
                
            EditorGUI.BeginProperty(position, label, entriesProperty);
            EditorGUI.PropertyField(position, entriesProperty, label, true);
            EditorGUI.EndProperty();
        }
            
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(GetEntriesProperty(property), label, true);
        }
            
        SerializedProperty GetEntriesProperty(SerializedProperty property) => 
            property.FindPropertyRelative(nameof(ARTurningAnimationOverrides.entries));
    }
}