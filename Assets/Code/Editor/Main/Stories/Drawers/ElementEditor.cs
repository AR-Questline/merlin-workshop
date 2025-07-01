using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Main.Stories.Steps.Helpers;
using Awaken.TG.Editor.Utility.StoryGraphs;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Debugging;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vendor.xNode.Scripts.Attributes;

namespace Awaken.TG.Editor.Main.Stories.Drawers {
    public class ElementEditor {
        public NodeElement target;
        protected T Target<T>() where T : NodeElement => (T) target;
        protected T ParentNode<T>() where T : StoryNode => (T) target.genericParent;

        protected SerializedObject _serializedObject;

        public void StartElementGUI(NodeElement element) {
            target = element;
            _serializedObject = new SerializedObject(element);
            AssignUniqueFlagForOncePer();
            OnStartGUI();
        }

        public void DrawElementGUI() {
            _serializedObject.Update();
            DrawDebugInfo();
            OnElementGUI();
            _serializedObject.ApplyModifiedProperties();
        }

        protected virtual void OnStartGUI() {}

        protected virtual void OnElementGUI() {
            DrawProperties();
        }

        // === Operations

        void DrawDebugInfo() {
            if (Application.isPlaying) {
                GUIStyle style = EditorStyles.helpBox;
                Color defaultColor = GUI.color;

                DebugResult debugResult = DebugResult.FindResult(Element(_serializedObject));
                GUI.color = debugResult.Color;
                EditorGUILayout.LabelField(debugResult.DisplayName, style);
                GUI.color = defaultColor;
            }
        }

        protected void DrawProperties() {
            DrawPropertiesExcept(new List<string>());
        }

        protected void DrawProperties(params string[] propertiesToDraw) {
            NodeGUIUtil.DrawGivenProperties(_serializedObject, Element(_serializedObject).genericParent, propertiesToDraw);
        }

        protected void DrawPropertiesExcept(string omitted) => DrawPropertiesExcept(new List<string> {omitted});
        protected void DrawPropertiesExcept(params string[] omitted) => DrawPropertiesExcept(omitted.ToList());
        protected void DrawPropertiesExcept(ICollection<string> omitted) {
            NodeGUIUtil.DrawNodePropertiesExcept(_serializedObject, Element(_serializedObject).genericParent, omitted);
        }

        protected void DrawTextCounter(int textLenght, int maxChars, int? nodeWidth = null) {
            EditorGUI.indentLevel--;
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            string colorString = textLenght > maxChars ? "#AA0000" : "#FFFFFF";
            string chars = $"<color={colorString}>{textLenght:D3}</color>";
            GUIStyle richTextLabel = new(GUI.skin.label) {
                richText = true
            };
            EditorGUILayout.LabelField($"{chars}/{SEditorText.MaxCharsPerLine}", richTextLabel, GUILayout.Width(36));
            GUILayout.EndHorizontal();
            EditorGUI.indentLevel++;
        }
        
        // === Helpers
        
        void AssignUniqueFlagForOncePer() {
            if (target is IOncePer oncePer) {
                if (string.IsNullOrEmpty(oncePer.SpanFlag) || StoryStepsUtil.DuplicatedName(target, oncePer.SpanFlag)) {
                    StoryStepsUtil.AssignName(target, n => oncePer.SpanFlag = n);
                }
            }
        }

        NodeElement Element(SerializedObject serialized) => ((NodeElement) serialized.targetObject);
    }
}