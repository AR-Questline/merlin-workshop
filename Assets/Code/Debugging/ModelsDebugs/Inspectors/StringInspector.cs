using Awaken.Utility.UI;

namespace Awaken.TG.Debugging.ModelsDebugs.Inspectors {
    [UnityEngine.Scripting.Preserve]
    public class StringInspector : MemberListItemInspector<string> {
        public override void DrawValue(MembersListItem member, object value, object target, ModelsDebug modelsDebug) {
            var stringValue = CastedValue(value);
            using (var checkScope = new TGGUILayout.CheckChangeScope()) {
                var result = TGGUILayout.DelayedTextField(member.Name, stringValue);
                if (checkScope) {
                    member.SetValue(target, result);
                }
            }
        }
    }
}