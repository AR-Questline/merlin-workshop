using Awaken.TG.Editor.Main.Stories.Drawers;
using Awaken.TG.Main.Stories.Steps;
using UnityEditor;

namespace Awaken.TG.Editor.Main.Stories.Steps {
    [CustomElementEditor(typeof(SEditorMapChange))]
    public class SMapChangeEditor : ElementEditor {
        public SEditorMapChange Target => (SEditorMapChange)target;

        protected override void OnElementGUI() {
            DrawPropertiesExcept(nameof(Target.indexTag));
            EditorGUI.BeginDisabledGroup(Target.useDefaultPortal);
            DrawProperties(nameof(Target.indexTag));
            EditorGUI.EndDisabledGroup();
        }
    }
}