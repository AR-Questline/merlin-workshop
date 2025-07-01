using Awaken.TG.Editor.Main.Stories.Drawers;
using Awaken.TG.Main.Heroes.Stats.StatConfig;
using Awaken.TG.Main.Stories.Steps;
using UnityEditor;

namespace Awaken.TG.Editor.Main.Stories.Steps {
    [CustomElementEditor(typeof(SEditorVariableSetFromItemsQuantity))]
    public class SVariableSetFromItemsQuantityEditor : ElementEditor {

        SEditorVariableSetFromItemsQuantity Target => (SEditorVariableSetFromItemsQuantity) target;
        
        protected override void OnElementGUI() {
            DrawProperties();
            int index = Target.Parent.elements.IndexOf(Target);
            EditorGUILayout.BeginVertical();
            if (index == 0 || !(Target.Parent.elements[index - 1] is SEditorChangeItemsQuantity)) {
                EditorGUILayout.HelpBox("You must add SChangeItemsQuantity directly above this step to use it", MessageType.Error);
            }
            EditorGUILayout.EndVertical();
        }
    }
}