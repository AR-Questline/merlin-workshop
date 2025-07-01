using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.Utility;
using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;

namespace Awaken.TG.Editor.Main.Stories {
    [CustomNodeEditor(typeof(ConditionsEditorNode))]
    public class ConditionsNodeEditor : StoryNodeEditor {
        protected override void BeforeElements() {
            base.BeforeElements();
            // draw entry link
            NodeEditorGUILayout.PortField(new GUIContent(""), Node.GetPort(NodePort.FieldNameCompressed.Inputs));
        }

        protected override void AfterElements() {
            EditorGUILayout.Space(2f);
            // draw output TRUE
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            NodeEditorGUILayout.PortField(new GUIContent("When true"), Node.GetPort(NodePort.FieldNameCompressed.TrueOutput), ARColor.DarkerGrey);
            EditorGUILayout.EndHorizontal();
            // Some space
            EditorGUILayout.Space(2f);
            // draw output FALSE
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            NodeEditorGUILayout.PortField(new GUIContent("When false"), Node.GetPort(NodePort.FieldNameCompressed.FalseOutput), ARColor.DarkerGrey);
            EditorGUILayout.EndHorizontal();
        }
    }
}