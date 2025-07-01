using Awaken.Utility.Enums;
using UnityEngine;

namespace Awaken.TG.Debugging.ModelsDebugs.Inspectors {
    [UnityEngine.Scripting.Preserve]
    public class RichEnumInspector : MemberListItemInspector<RichEnum> {
        public override void DrawValue(MembersListItem member, object value, object target, ModelsDebug modelsDebug) {
            var richEnum = CastedValue(value);
#if UNITY_EDITOR
            var richEnumText = $"<color=lightblue>{member.Name}</color>: <color=yellow>{richEnum.EnumName}</color> ({richEnum.GetType()}) {richEnum.InspectorCategory}";
#else
            var richEnumText = $"<color=lightblue>{member.Name}</color>: <color=yellow>{richEnum.EnumName}</color> ({richEnum.GetType()})";
#endif
            GUILayout.Label(richEnumText, LabelStyle);
        }
    }
}
