using Awaken.TG.Editor.Helpers.Tags;
using Awaken.TG.Main.Skills;
using Awaken.TG.Main.Utility.VSDatums;
using Awaken.TG.Utility.Attributes.Tags;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility.VSDatums.TypeInstances {
    public class VSDatumTagsDrawer : VSDatumTypeInstanceDrawer {
        public static readonly VSDatumTypeInstanceDrawer FlagsDrawer = new VSDatumTagsDrawer(TagsCategory.Flag);
        readonly TagsCategory _category;

        VSDatumTagsDrawer(TagsCategory category) {
            this._category = category;
        }
        
        public override void Draw(in Rect rect, SerializedProperty property, ref VSDatumValue value, out bool changed) {
            TagsEditing.Show(property.FindPropertyRelative(nameof(SkillDatum.value) + ".stringValue"), _category);
            changed = false;
        }

        public override void DrawInLayout(SerializedProperty property, ref VSDatumValue value, out bool changed) {
            TagsEditing.Show(property.FindPropertyRelative(nameof(SkillDatum.value) + ".stringValue"), _category);
            changed = false;
        }
    }
}