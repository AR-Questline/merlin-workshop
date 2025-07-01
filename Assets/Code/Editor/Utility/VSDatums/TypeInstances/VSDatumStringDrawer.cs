using Awaken.TG.Main.Utility.VSDatums;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.VSDatums.TypeInstances {
    public class VSDatumStringDrawer : VSDatumTypeInstanceDrawer {
        public static readonly VSDatumTypeInstanceDrawer Instance = new VSDatumStringDrawer();
        public override void Draw(in Rect rect, SerializedProperty property, ref VSDatumValue value, out bool changed) {
            var newStringValue = EditorGUI.TextField(rect, value.String);
            if (!string.Equals(newStringValue, value.String)) {
                value = new VSDatumValue { String = newStringValue };
                changed = true;
            } else {
                changed = false;
            }
        }

        public override void DrawInLayout(SerializedProperty property, ref VSDatumValue value, out bool changed) {
            var newStringValue = EditorGUILayout.TextField(value.String);
            if (!string.Equals(newStringValue, value.String)) {
                value = new VSDatumValue { String = newStringValue };
                changed = true;
            } else {
                changed = false;
            }
        }
    }
}