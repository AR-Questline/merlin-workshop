using Awaken.TG.Editor.Main.Stories.Drawers;
using Awaken.TG.Main.Stories.Steps;
using UnityEngine;
using XNodeEditor;

namespace Awaken.TG.Editor.Main.Stories.Steps {
    [CustomElementEditor(typeof(SEditorOpenGemsUI))]
    public class SOpenGemsUIEditor : ElementEditor {

        protected override void OnElementGUI() {
            DrawProperties();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("If Any Relic Attached");
            NodeEditorGUILayout.PortField(new GUIContent(""), target.TargetPort(), GUILayout.Width(0));
            GUILayout.EndHorizontal();
        }
    }
}