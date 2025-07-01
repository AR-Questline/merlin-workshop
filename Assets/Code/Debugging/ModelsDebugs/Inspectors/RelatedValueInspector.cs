using Awaken.TG.MVC;
using Awaken.Utility.UI;
using UnityEngine;

namespace Awaken.TG.Debugging.ModelsDebugs.Inspectors {
    [UnityEngine.Scripting.Preserve]
    public class RelatedValueInspector : MemberListItemInspector<IRelatedValue> {
        public override void DrawValue(MembersListItem member, object value, object target, ModelsDebug modelsDebug) {
            var model = CastedValue(value).Related;
            if (model == null) {
                GUILayout.Label(YellowNull, LabelStyle);
            } else {
                TGGUILayout.BeginHorizontal();
                GUILayout.Label(model.GetType().Name, LabelStyle);
                if (GUILayout.Button(model.ID)) {
                    modelsDebug.SetSelectedId(model.ID);
                }

                TGGUILayout.EndHorizontal();
            }
        }
    }

    public interface IRelatedValue {
        IModel Related { get; }
    }
}