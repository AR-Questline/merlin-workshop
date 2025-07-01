using Awaken.Utility.UI;

namespace Awaken.TG.Debugging.ModelsDebugs.Inspectors {
    [UnityEngine.Scripting.Preserve]
    public class BoolInspector : MemberListItemInspector<bool> {
        public override void DrawValue(MembersListItem member, object value, object target, ModelsDebug modelsDebug) {
            var boolValue = CastedValue(value);
            using (var changeScope = new TGGUILayout.CheckChangeScope()) {
                var result = TGGUILayout.Toggle(member.Name, boolValue);
                if (changeScope) {
                    member.SetValue(target, result);
                }
            }
        }
    }
}