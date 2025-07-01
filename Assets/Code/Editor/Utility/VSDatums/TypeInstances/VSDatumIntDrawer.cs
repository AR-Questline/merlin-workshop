using Awaken.TG.Main.Utility.VSDatums;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.VSDatums.TypeInstances {
    public class VSDatumIntDrawer : VSDatumTypeInstanceDrawer {
        public static readonly VSDatumTypeInstanceDrawer Instance = new VSDatumIntDrawer();
        
        public override void Draw(in Rect rect, SerializedProperty property, ref VSDatumValue value, out bool changed) {
            var newIntValue = EditorGUI.IntField(rect, value.Int);
            if (newIntValue != value.Int) {
                value = new VSDatumValue { Int = newIntValue };
                changed = true;
            } else {
                changed = false;
            }
        }

        public override void DrawInLayout(SerializedProperty property, ref VSDatumValue value, out bool changed) {
            var newIntValue = EditorGUILayout.IntField(value.Int);
            if (newIntValue != value.Int) {
                value = new VSDatumValue { Int = newIntValue };
                changed = true;
            } else {
                changed = false;
            }
        }
    }
}