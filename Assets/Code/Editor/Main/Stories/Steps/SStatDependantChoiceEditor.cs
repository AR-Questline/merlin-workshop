using Awaken.TG.Editor.Main.Stories.Drawers;
using Awaken.TG.Main.Stories.Steps;
using Awaken.Utility.Editor;
using UnityEngine;
using XNodeEditor;

namespace Awaken.TG.Editor.Main.Stories.Steps {
    [CustomElementEditor(typeof(SEditorStatDependantChoice))]
    public class SStatDependantChoiceEditor : ElementEditor {
        protected override void OnElementGUI() {
            GUIUtils.PushLabelWidth(150);

            const string OverrideLabel = nameof(SEditorStatDependantChoice.overrideLabel);
            DrawPropertiesExcept(OverrideLabel);

            SEditorStatDependantChoice targetElement = Target<SEditorStatDependantChoice>();
            GUILayout.Space(10);
            if (targetElement.IsFlat || targetElement.isVisibleToPlayer) {
                DrawProperties(OverrideLabel);
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (targetElement.requirementType == SStatDependantChoice.RequirementType.Chance) {
                NodeEditorGUILayout.PortField(new GUIContent("On Success"), targetElement.SuccessPort, color: Color.green, options: GUILayout.Width(200));
            }
            GUILayout.EndHorizontal();
            
            GUIUtils.PopLabelWidth();
        }
    }
}