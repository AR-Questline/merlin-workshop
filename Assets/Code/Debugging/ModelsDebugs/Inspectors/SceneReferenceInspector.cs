using Awaken.TG.Assets;
using UnityEngine;

namespace Awaken.TG.Debugging.ModelsDebugs.Inspectors {
    [UnityEngine.Scripting.Preserve]
    public class SceneReferenceInspector : MemberListItemInspector<SceneReference> {
        public override void DrawValue(MembersListItem member, object value, object target, ModelsDebug modelsDebug) {
            var sceneRefValue = CastedValue(value);
            string sceneNameValue = sceneRefValue.Name;
            GUILayout.Label($"<color=lightblue>{member.Name}</color>: <color=yellow>{sceneNameValue}</color>", LabelStyle);
        }
    }
}