using Awaken.TG.Editor.Main.Stories.Drawers;
using Awaken.TG.Editor.Utility.StoryGraphs;
using Awaken.TG.Main.Stories.Steps;
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Awaken.TG.Editor.Main.Stories.Steps {
    [CustomElementEditor(typeof(SEditorPopupChoice))]
    public class SPopupChoiceEditor : ElementEditor {

        protected override void OnElementGUI() {
            DrawPropertiesExcept("choice");
            SEditorPopupChoice sPopupChoice = Target<SEditorPopupChoice>();
            int width = NodeGUIUtil.GetNodeWidth(sPopupChoice.Parent);

            SerializedProperty choiceProperty = _serializedObject.FindProperty("choice");
            SerializedProperty textProperty = choiceProperty.FindPropertyRelative("text");

            GUILayout.BeginHorizontal();
            NodeGUIUtil.DrawProperty(textProperty, sPopupChoice.choice.GetType().GetField("text"), width);
            NodeEditorGUILayout.PortField(new GUIContent(""), target.TargetPort(), GUILayout.Width(0));
            GUILayout.EndHorizontal();
            
            DrawProperties("techInfo");
        }
    }
}