using Awaken.TG.Main.Stories.Core;
using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;

namespace Awaken.TG.Editor.Main.Stories {
    [CustomNodeEditor(typeof(FightNode))]
    public class FightNodeEditor : StoryNodeEditor {
        protected override void BeforeElements() {
            base.BeforeElements();
            // draw entry link
            NodeEditorGUILayout.PortField(new GUIContent(""), Node.GetPort(NodePort.FieldNameCompressed.Link));
        }

        protected override void AfterElements() {
            // draw output SUCCESS
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            NodeEditorGUILayout.PortField(new GUIContent("Success"), Node.GetPort(NodePort.FieldNameCompressed.Continuation));
            EditorGUILayout.EndHorizontal();
            // draw output FAILURE
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            NodeEditorGUILayout.PortField(new GUIContent("Failure"), Node.GetPort(NodePort.FieldNameCompressed.Failure));
            EditorGUILayout.EndHorizontal();
        }
    }
}