using System;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Utils;
using Awaken.Utility.UI;
using UnityEngine;

namespace Awaken.TG.Debugging.ModelsDebugs.Inspectors {
    [UnityEngine.Scripting.Preserve]
    public class WeakModelRefInspector : MemberListItemInspector<IWeakModelRef> {
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
            var modelRef = CastedValue(value);
            var model = GetModel(modelRef);

            if (model == null) {
                TGGUILayout.BeginHorizontal();
                GUILayout.Label(member.Name, LabelStyle);
                GUILayout.Label(modelRef?.ID, LabelStyle);
                TGGUILayout.EndHorizontal();
            } else {
                TGGUILayout.BeginHorizontal();
                GUILayout.Label(member.Name, LabelStyle);
                if (GUILayout.Button(model.ID)) {
                    modelsDebug.SetSelectedId(model.ID);
                }
                TGGUILayout.EndHorizontal();
            }
        }

        protected override bool ValueInContext(object value, Lazy<string> stringValue, string searchPart) {
            var model = GetModel(CastedValue(value));
            return base.ValueInContext(value, model ? new(model.ToString) : new(string.Empty), searchPart);
        }

        static Model GetModel(IWeakModelRef modelRef) {
            Model model = null;
            if (World.Services != null && !string.IsNullOrWhiteSpace(modelRef?.ID)) {
                model = World.ByID<Model>(modelRef.ID);
            }
            return model;
        }
    }
}