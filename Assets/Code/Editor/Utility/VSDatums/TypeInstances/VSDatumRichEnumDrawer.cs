using Awaken.TG.Editor.Utility.RichEnumReference;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.Main.Utility.VSDatums;
using Awaken.Utility.Enums;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.VSDatums.TypeInstances {
    public class VSDatumRichEnumDrawer<T> : VSDatumTypeInstanceDrawer where T : RichEnum {
        public static VSDatumTypeInstanceDrawer Instance { get; } = new VSDatumRichEnumDrawer<T>();

        string _search;
        readonly RichEnumExtendsAttribute _setting = new(typeof(T));
        
        public override void Draw(in Rect rect, SerializedProperty property, ref VSDatumValue value, out bool changed) {
            changed = false;
            SerializedProperty stringVal = property.FindPropertyRelative(nameof(SkillDatum.value) + ".stringValue");
            RichEnumReferencePropertyDrawer.DrawSelectionControl(
                rect, 
                GUIContent.none, 
                stringVal.stringValue, 
                ref _search,
                _setting,
                richEnum => {
                    stringVal.stringValue = TG.Main.Utility.RichEnums.RichEnumReference.GetEnumRef(richEnum);
                });
        }

        public override void DrawInLayout(SerializedProperty property, ref VSDatumValue value, out bool changed) {
            changed = false;
            SerializedProperty stringVal = property.FindPropertyRelative(nameof(SkillDatum.value) + ".stringValue");
            RichEnumReferencePropertyDrawer.DrawSelectionControl(
                EditorGUILayout.GetControlRect(), 
                GUIContent.none, 
                stringVal.stringValue, 
                ref _search,
                _setting,
                richEnum => {
                    stringVal.stringValue = TG.Main.Utility.RichEnums.RichEnumReference.GetEnumRef(richEnum);
                });
        }
    }
}