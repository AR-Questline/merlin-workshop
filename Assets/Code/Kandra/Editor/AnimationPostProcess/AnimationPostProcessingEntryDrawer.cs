using UnityEditor;
using UnityEngine;

namespace Awaken.Kandra.Editor.AnimationPostProcess {
    [CustomPropertyDrawer(typeof(Kandra.AnimationPostProcess.AnimationPostProcessing.Entry))]
    public class AnimationPostProcessingEntryDrawer : PropertyDrawer {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var presetProp = property.FindPropertyRelative("preset");
            var weightProp = property.FindPropertyRelative("weight");

            var halfWidth = position.width / 2f;
            var halfHeightPadding = halfWidth-2;

            var presetRect = new Rect(position.x, position.y, halfHeightPadding, position.height);
            var weightRect = new Rect(position.x + halfWidth + 2, position.y, halfHeightPadding, position.height);

            EditorGUI.PropertyField(presetRect, presetProp, GUIContent.none);
            EditorGUI.PropertyField(weightRect, weightProp, GUIContent.none);
        }
    }
}