using Awaken.TG.Main.Stories.Execution;
using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;

namespace Awaken.TG.Editor.Main.Stories {
    [CustomNodeEditor(typeof(StoryStartEditorNode))]
    public class StoryStartEditor : StoryNodeEditor {
        StoryStartEditorNode StoryStart => (StoryStartEditorNode) target;
        bool EnableChoices => StoryStart.enableChoices;

        protected override void BeforeElements() {
            if (!EnableChoices) {
                NodeEditorGUILayout.PortField(Node.GetPort(NodePort.FieldNameCompressed.Chapter));
            } else {
                NodeEditorGUILayout.PortField(new GUIContent(""), Node.GetPort(NodePort.FieldNameCompressed.Link));
            }
            
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 110;
            EditorGUILayout.BeginHorizontal();
            StoryStart.involveAI = EditorGUILayout.Toggle("Involve NPC", StoryStart.involveAI, GUILayout.Width(120));
            GUILayout.Space(10);
            EditorGUIUtility.labelWidth = 80;
            StoryStart.involveHero = EditorGUILayout.Toggle("Involve Hero", StoryStart.involveHero, GUILayout.Width(90));
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = 110;
            NodeEditorGUILayout.PropertyField(serializedObject.FindProperty("enableChoices"));
            EditorGUIUtility.labelWidth = labelWidth;
        }
        
        protected override void DrawElements() {
            if (EnableChoices) {
                base.DrawElements();
            }
        }

        protected override void DrawAddElementButton(int index) {
            if (EnableChoices) {
                base.DrawAddElementButton(index);
            }
        }
    }
}