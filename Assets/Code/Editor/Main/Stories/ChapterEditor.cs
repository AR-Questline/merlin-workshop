using Awaken.TG.Main.Stories.Core;
using UnityEngine;
using XNode;
using XNodeEditor;

namespace Awaken.TG.Editor.Main.Stories {
    [CustomNodeEditor(typeof(ChapterEditorNode))]
    public class ChapterEditor : StoryNodeEditor {
        protected override void BeforeElements() {
            base.BeforeElements();
            // draw entry link
            NodeEditorGUILayout.PortField(new GUIContent(""), Node.GetPort(NodePort.FieldNameCompressed.Link));
        }

        protected override void AfterElements() {
            // draw output port
            NodeEditorGUILayout.PortField(new GUIContent(""), Node.GetPort(NodePort.FieldNameCompressed.Continuation));
        }
    }
}