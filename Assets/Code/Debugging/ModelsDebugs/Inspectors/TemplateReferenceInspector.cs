using Awaken.TG.Main.Templates;
using Awaken.Utility.UI;
using UnityEngine;

namespace Awaken.TG.Debugging.ModelsDebugs.Inspectors {
    [UnityEngine.Scripting.Preserve]
    public class TemplateReferenceInspector : MemberListItemInspector<TemplateReference> {
        public override void DrawValue(MembersListItem member, object value, object target, ModelsDebug modelsDebug) {
            var templateReference = CastedValue(value);
            TGGUILayout.BeginHorizontal();

            if (GUILayout.Button(templateReference.GUID)) {
                GUIUtility.systemCopyBuffer = templateReference.GUID;
            }
            
            try {
                GUILayout.Label(TemplatesUtil.Load<Object>(templateReference.GUID)?.name);
            } catch { 
                // ignored
            }

            TGGUILayout.EndHorizontal();
        }
    }
}