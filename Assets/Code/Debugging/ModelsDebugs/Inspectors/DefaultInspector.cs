using Awaken.Utility.Collections;
using UnityEngine;

namespace Awaken.TG.Debugging.ModelsDebugs.Inspectors {
    public class DefaultInspector : MemberListItemInspector<object> {
        readonly OnDemandCache<int, bool> _collapses = new(_ => true);

        public override void Draw(MembersListItem member, object value, object target, ModelsDebug modelsDebug,
            string[] searchContext, int searchHash) {
            if (!IsInContext(member, value, searchContext, searchHash)) {
                return;
            }
            var oldEnable = GUI.enabled;
            GUI.enabled = member.Writeable;
            if (value != null) {
                GUI.enabled = true;
                var collapsedKey = (member.Name.GetHashCode()*397) ^ value.GetHashCode();
                var collapsed = _collapses[collapsedKey];
                if (GUILayout.Button((collapsed ? "\u25B6" : "\u25BC") + $" <color=lightblue>{member.Name}</color>: <color=yellow>{value}</color>", LabelStyle)) {
                    collapsed = !collapsed;
                    _collapses[collapsedKey] = collapsed;
                }
                GUI.enabled = member.Writeable;

                if (collapsed) {
                    return;
                }

                modelsDebug.DrawObject(value, searchContext, searchHash, false);
            } else {
                GUILayout.Label($"<color=lightblue>{member.Name}</color>:{YellowNull}", LabelStyle);
            }
            GUI.enabled = oldEnable;
        }

        public override void DrawValue(MembersListItem member, object value, object target, ModelsDebug modelsDebug) {
            GUILayout.Label($"<color=lightblue>{member.Name}</color>: <color=yellow>{value}</color>", LabelStyle);
        }
    }
}