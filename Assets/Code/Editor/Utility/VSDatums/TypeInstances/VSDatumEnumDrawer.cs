using System;
using Awaken.TG.Main.Utility.VSDatums;
using Awaken.TG.Main.Utility.VSDatums.TypeInstances;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.VSDatums.TypeInstances {
    public class VSDatumEnumDrawer<TEnum> : VSDatumTypeInstanceDrawer where TEnum : Enum {
        public static readonly VSDatumTypeInstanceDrawer Instance = new VSDatumEnumDrawer<TEnum>();
        
        public override void Draw(in Rect rect, SerializedProperty property, ref VSDatumValue value, out bool changed) {
            var typeInstance = VSDatumTypeInstanceEnum<TEnum>.Instance;
            var enumValue = typeInstance.GetDatumValue(value);
            var newEnumValue = (TEnum)EditorGUI.EnumPopup(rect, enumValue);
            if (!newEnumValue.Equals(enumValue)) {
                value = typeInstance.ToDatumValue(newEnumValue);
                changed = true;
            } else {
                changed = false;
            }
        }

        public override void DrawInLayout(SerializedProperty property, ref VSDatumValue value, out bool changed) {
            var typeInstance = VSDatumTypeInstanceEnum<TEnum>.Instance;
            var enumValue = typeInstance.GetDatumValue(value);
            var newEnumValue = (TEnum)EditorGUILayout.EnumPopup(enumValue);
            if (!newEnumValue.Equals(enumValue)) {
                value = typeInstance.ToDatumValue(newEnumValue);
                changed = true;
            } else {
                changed = false;
            }
        }
    }
}