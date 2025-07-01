using Awaken.TG.Editor.Main.Stories.Drawers;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Editor.Utility.StoryGraphs;
using Awaken.TG.Main.Stories.Steps;
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Awaken.TG.Editor.Main.Stories.Steps {
    [CustomElementEditor(typeof(SEditorStoryStartChoice))]
    public class StoryStartChoiceEditor : ElementEditor {
        protected override void OnElementGUI() {
            SEditorStoryStartChoice choice = Target<SEditorStoryStartChoice>();
            if (choice.Parent == null) {
                return;
            }
            
            int width = NodeGUIUtil.GetNodeWidth(choice.Parent);
            
            GUILayout.BeginVertical();
            DrawProperties("span", "passiveProgress");
            GUILayout.EndVertical();
            
            GUILayout.BeginHorizontal();
            SerializedProperty choiceProperty = _serializedObject.FindProperty("choice");
            SerializedProperty textProperty = choiceProperty.FindPropertyRelative("text");
            SerializedProperty descriptionProperty = choiceProperty.FindPropertyRelative("description");
            NodeGUIUtil.DrawProperty(textProperty, choice.choice.GetType().GetField("text"), width);
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            NodeGUIUtil.DrawProperty(descriptionProperty, choice.choice.GetType().GetField("description"), width);
            NodeEditorGUILayout.PortField(new GUIContent(""), target.TargetPort(), GUILayout.Width(0));
            GUILayout.EndHorizontal();
            
            DrawProperties("techInfo");
        }
    }
}