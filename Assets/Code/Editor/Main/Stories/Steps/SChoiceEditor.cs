using Awaken.TG.Editor.Main.Stories.Drawers;
using Awaken.TG.Editor.Utility.StoryGraphs;
using Awaken.TG.Main.Stories.Steps;
using Awaken.Utility.Editor;
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Awaken.TG.Editor.Main.Stories.Steps {
    [CustomElementEditor(typeof(SEditorChoice))]
    public class SChoiceEditor : ElementEditor {

        protected override void OnElementGUI() {
            var exitChoice = target as SEditorChoicesExit;
            if (exitChoice != null) {
                EditorGUILayout.HelpBox("Will auto trigger when no other choices BEFORE this one are available", MessageType.Warning);
            }

            GUIUtils.PushLabelWidth(120);
            DrawPropertiesExcept("choice", "audioClip", "playSound", "techInfo", "span", "choiceIcon", "passiveProgress");
            GUIUtils.PopLabelWidth();

            SEditorChoice sChoice = Target<SEditorChoice>();
            int width = NodeGUIUtil.GetNodeWidth(sChoice.Parent);

            GUILayout.BeginHorizontal();
            GUIUtils.PushFieldWidth(45);
            DrawProperties("span");
            GUIUtils.PopFieldWidth();
            
            GUILayout.Space(20);
            
            GUIUtils.PushLabelWidth(45);
            sChoice.choice.isMainChoice = EditorGUILayout.Toggle("Main:", sChoice.choice.isMainChoice, GUILayout.Width(65));
            GUIUtils.PopLabelWidth();
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            DrawProperties("choiceIcon");
            GUILayout.EndHorizontal();

            SerializedProperty choiceProperty = _serializedObject.FindProperty("choice");
            SerializedProperty textProperty = choiceProperty.FindPropertyRelative("text");

            GUILayout.BeginHorizontal();
            if (exitChoice == null || !exitChoice.hiddenFromPlayer) {
                NodeGUIUtil.DrawProperty(textProperty, sChoice.choice.GetType().GetField("text"), width);
            } else {
                GUILayout.FlexibleSpace();
            }

            NodeEditorGUILayout.PortField(new GUIContent(""), target.TargetPort(), GUILayout.Width(0));
            GUILayout.EndHorizontal();

            DrawProperties("techInfo");
        }
    }
}