using Awaken.TG.Editor.Helpers;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Utility.VSDatums;
using Awaken.Utility.UI;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.VSDatums {
    [CustomPropertyDrawer(typeof(SkillDatum))]
    public class SkillDatumDrawer : PropertyDrawer {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (property.boxedValue is not SkillDatum datum) {
                return;
            }
            var rects = new PropertyDrawerRects(position);
            EditorGUI.BeginChangeCheck();
            datum.name = EditorGUI.TextField(rects.AllocateLeftNormalized(0.3f), datum.name);
            var nameChanged = EditorGUI.EndChangeCheck();
            rects.AllocateLeft(2f);
            VSDatumTypeDrawer.Draw(rects.AllocateLeftNormalized(0.4f), datum.type, out datum.type, out var typeChanged);
            if (typeChanged) {
                datum.value = default;
            }
            rects.AllocateLeft(2f);
            VSDatumValueDrawer.Draw((Rect) rects, property, datum.type, ref datum.value, out bool valueChanged);
            
            if (nameChanged || valueChanged || typeChanged) {
                property.boxedValue = datum;
                GUI.changed = true;
            }
        }
    }
}