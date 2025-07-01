using Awaken.TG.Editor.Main.Stories.Drawers;
using Awaken.TG.Main.Heroes.Stats.StatConfig;
using Awaken.TG.Main.Stories.Steps;
using UnityEditor;

namespace Awaken.TG.Editor.Main.Stories.Steps {
    [CustomElementEditor(typeof(SEditorStatChange))]
    public class SStatChangeEditor : ElementEditor {

        SEditorStatChange Target => (SEditorStatChange) target;
        
        protected override void OnElementGUI() {
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
    }
}