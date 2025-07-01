using Awaken.Utility.UI;
using UnityEngine;

namespace Awaken.TG.Debugging.ModelsDebugs.Inspectors {
    [UnityEngine.Scripting.Preserve]
    public class DefaultUnityInspector : MemberListItemInspector<Object> {
        public override void Draw(
            MembersListItem member, object value, object target, ModelsDebug modelsDebug, string[] searchContext,
            int searchHash) {
            base.Draw(member, value, target, modelsDebug, searchContext, searchHash);
            DefaultInspector.Draw(member, value, target, modelsDebug, searchContext, searchHash);
        }

        public override void DrawValue(MembersListItem member, object value, object target, ModelsDebug modelsDebug) {
            Object unityObject = CastedValue(value);
            using (var changeScope = new TGGUILayout.CheckChangeScope()) {
                Object objectValue = TGGUILayout.ObjectField(member.Name, unityObject, member.Type, true);
                if (changeScope) {
                    member.SetValue(target, objectValue);
                }
            }
        }
    }
}