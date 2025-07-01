using Awaken.Utility.UI;

namespace Awaken.TG.Debugging.ModelsDebugs.Inspectors {
    [UnityEngine.Scripting.Preserve]
    public class FloatInspector : MemberListItemInspector<float> {
        public override void DrawValue(MembersListItem member, object value, object target, ModelsDebug modelsDebug) {
            var floatValue = CastedValue(value);
            using (var checkScope = new TGGUILayout.CheckChangeScope()) {
                var result = TGGUILayout.DelayedFloatField(member.Name, floatValue);
                if (checkScope) {
                    member.SetValue(target, result);
                }
            }
        }
    }
}