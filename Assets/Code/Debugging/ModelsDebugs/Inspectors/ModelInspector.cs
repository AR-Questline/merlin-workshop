using Awaken.TG.MVC;
using Awaken.Utility.UI;
using UnityEngine;

namespace Awaken.TG.Debugging.ModelsDebugs.Inspectors {
    [UnityEngine.Scripting.Preserve]
    public class ModelInspector : MemberListItemInspector<Model> {
        public override void Draw(MembersListItem member, object value, object target, ModelsDebug modelsDebug, string[] searchContext, int searchHash) {
            if (!IsInContext(member, value, searchContext, searchHash)) {
                return;
            }
            var oldEnable = GUI.enabled;
            GUI.enabled = true;
            DrawValue(member, value, target, modelsDebug);
            GUI.enabled = oldEnable;
        }

        public override void DrawValue(MembersListItem member, object value, object target, ModelsDebug modelsDebug) {
            var model = CastedValue(value);
            TGGUILayout.BeginHorizontal();
            GUILayout.Label(member.Name, LabelStyle);
            if (GUILayout.Button(model.ID)) {
                modelsDebug.SetSelectedId(model.ID);
            }
            TGGUILayout.EndHorizontal();
        }
    }
}