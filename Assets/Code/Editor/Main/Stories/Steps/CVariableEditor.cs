using Awaken.TG.Editor.Main.Stories.Drawers;
using Awaken.TG.Main.Stories.Conditions;
using UnityEditor;

namespace Awaken.TG.Editor.Main.Stories.Steps {
    [CustomElementEditor(typeof(CEditorVariable))]
    public class CVariableEditor : ElementEditor {
        protected override void OnElementGUI() {
            DrawProperties();

            CEditorVariable cEditorVar = Target<CEditorVariable>();
            EditorGUILayout.LabelField(cEditorVar.Summary());
        }
    }
}