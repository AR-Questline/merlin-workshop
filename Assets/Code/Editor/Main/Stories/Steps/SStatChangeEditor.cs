using System.Text;
using Awaken.TG.Editor.Main.Stories.Drawers;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Editor.Utility.StoryGraphs;
using Awaken.TG.Main.Heroes.Stats.StatConfig;
using Awaken.TG.Main.Stories.Api;
using Awaken.TG.Main.Stories.Steps;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Stories.Steps {
    [CustomElementEditor(typeof(SEditorStatChange))]
    public class SStatChangeEditor : ElementEditor {

        SEditorStatChange Target => (SEditorStatChange) target;
        
        protected override void OnElementGUI(bool isEditMode) {
            if (isEditMode) {
                DrawEditModeGUI();
                return;
            }

            DrawReadonlyGUI();
        }
        
        void DrawEditModeGUI() {
            DrawPropertiesExcept("isKnown", "isCost", "statValue", "useVariableMultiplier");
            
            SEditorStatChange editorStatChange = Target<SEditorStatChange>();
            if (editorStatChange.definedRange == StatDefinedRange.Custom) {
                DrawProperties("statValue");
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("isKnown"));
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("isCost"));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            DrawProperties("useVariableMultiplier");
            if (Target.useVariableMultiplier) {
                int index = Target.Parent.elements.IndexOf(Target);
                if (Target.Parent.elements.Count <= index + 1 || !(Target.Parent.elements[index + 1] is SEditorVariableReference)) {
                    EditorGUILayout.HelpBox("You must add any SVariable directly below this step to use variables", MessageType.Error);
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
        
        void DrawReadonlyGUI() {
            SEditorStatChange editorStatChange = Target<SEditorStatChange>();
            StringBuilder labelBuilder = new ();
            
            labelBuilder.Append("<color=#808080>Change </color>");
            labelBuilder.Append(editorStatChange.target == StoryRoleTarget.Hero ? "Hero's " : "Custom Location's ");
            
            labelBuilder.Append($"{editorStatChange.affectedStat.Enum.EnumName} ");
            labelBuilder.Append("<color=#808080>by</color> ");
            labelBuilder.Append(editorStatChange.definedRange != StatDefinedRange.Custom ? editorStatChange.definedRange.ToString() : editorStatChange.statValue.value);
            
            int nodeWidth = NodeGUIUtil.GetNodeWidth(editorStatChange.Parent);
            var label = labelBuilder.ToString();
            var style = TGEditorGUIStyles.ReadOnlyTextArea;
            var height = style.CalcHeight(new GUIContent(label), nodeWidth - 100);
            EditorGUILayout.LabelField(label, style, GUILayout.Height(height));
        }
    }
}