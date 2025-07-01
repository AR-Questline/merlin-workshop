using Awaken.Utility.UI;

namespace Awaken.TG.Debugging.ModelsDebugs.Inspectors {
    [UnityEngine.Scripting.Preserve]
    public class IntInspector : MemberListItemInspector<int> {
        public override void DrawValue(MembersListItem member, object value, object target, ModelsDebug modelsDebug) {
            var intValue = CastedValue(value);
            using (var checkScope = new TGGUILayout.CheckChangeScope()) {
                var result = TGGUILayout.DelayedIntField(member.Name, intValue);
                if (checkScope) {
                    member.SetValue(target, result);
                }
            }
        }
    }
}