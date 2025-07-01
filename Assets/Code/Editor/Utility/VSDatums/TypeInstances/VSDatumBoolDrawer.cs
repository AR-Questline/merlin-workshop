using Awaken.TG.Main.Utility.VSDatums;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.VSDatums.TypeInstances {
    public class VSDatumBoolDrawer : VSDatumTypeInstanceDrawer {
        public static readonly VSDatumTypeInstanceDrawer Instance = new VSDatumBoolDrawer();
        
        public override void Draw(in Rect rect, SerializedProperty property, ref VSDatumValue value, out bool changed) {
            var newBoolValue = EditorGUI.Toggle(rect, value.Bool);
            if (newBoolValue != value.Bool) {
                value = new VSDatumValue { Bool = newBoolValue };
                changed = true;
            } else {
                changed = false;
            }
        }

        public override void DrawInLayout(SerializedProperty property, ref VSDatumValue value, out bool changed) {
            var newBoolValue = EditorGUILayout.Toggle(value.Bool);
            if (newBoolValue != value.Bool) {
                value = new VSDatumValue { Bool = newBoolValue };
                changed = true;
            } else {
                changed = false;
            }
        }
    }
}