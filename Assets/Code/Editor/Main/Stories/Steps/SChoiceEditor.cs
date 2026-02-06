using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Localizations;
using Awaken.TG.Editor.Main.Stories.Drawers;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Editor.Utility.StoryGraphs;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.Utility.Debugging;
using Awaken.Utility.Editor;
using Awaken.Utility.Times;
using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;

namespace Awaken.TG.Editor.Main.Stories.Steps {
    [CustomElementEditor(typeof(SEditorChoice))]
    public class SChoiceEditor : ElementEditor {
        protected override void OnElementGUI(bool isEditMode) {
            if (isEditMode) {
                DrawEditModeGUI();
                return;
            }

            DrawReadonlyGUI();
        }

        void DrawEditModeGUI() {
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

        void DrawReadonlyGUI() {
            if (!NodeEditorWindow.FarView) {
                var exitChoice = target as SEditorChoicesExit;
                if (exitChoice != null) {
                    EditorGUILayout.HelpBox("Will auto trigger when no other choices BEFORE this one are available", MessageType.Warning);
                }

                SEditorChoice sChoice = Target<SEditorChoice>();

                SerializedProperty choiceProperty = _serializedObject.FindProperty("choice");
                SerializedProperty textProperty = choiceProperty.FindPropertyRelative("text");

                GUILayout.BeginHorizontal();
                if (exitChoice == null || !exitChoice.hiddenFromPlayer) {
                    int nodeWidth = NodeGUIUtil.GetNodeWidth(sChoice.Parent);

                    SerializedProperty oncePerProperty = _serializedObject.FindProperty("span");
                    var oncePer = ((TimeSpans)oncePerProperty.intValue).ToString();
                    var textField = sChoice.choice.GetType().GetField("text");
                    LocStringData textData = LocStringGUIUtils.GetData(textProperty, textField, nodeWidth);

                    var label = $"{textData.textString} \n<color=#707070>once per: {oncePer}</color>";
                    var style = TGEditorGUIStyles.ReadOnlyTextArea;
                    var height = style.CalcHeight(new GUIContent(label), nodeWidth);
                    EditorGUILayout.LabelField(label, style, GUILayout.Height(height));
                } else {
                    GUILayout.FlexibleSpace();
                }
            } else {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
            }

            NodeEditorGUILayout.PortField(new GUIContent(""), target.TargetPort(), GUILayout.Width(0));
            GUILayout.EndHorizontal();
        }

        // === Editor Utils ===
        const float VerticalOffset = 300f;
        
        /// <summary>
        /// Checks if the given object is a ChoiceNode.
        /// </summary>
        public static bool IsChoiceNode(Object o) {
            return o is StoryNode storyNode && storyNode.elements.All((p) => p is SEditorChoice) && storyNode.elements.Count > 0;
        }

        /// <summary>
        /// Checks if the given object is a ChoiceHub (a StoryNode with multiple SEditorChoice elements).
        /// </summary>
        public static bool IsChoiceHub(Object o) {
            return o is StoryNode storyNode && storyNode.elements.All((p) => p is SEditorChoice) && storyNode.elements.Count > 1;
        }

        /// <summary>
        /// Splits a choice hub StoryNode into multiple ChapterEditorNodes, each containing one choice.
        /// </summary>
        public static void SplitChoices(StoryNode sourceNode) {
            var graph = sourceNode.Graph;
            var choiceElements = sourceNode!.elements.OfType<SEditorChoice>().ToList();
            Vector2 offset = Vector2.up * VerticalOffset;

            List<ChapterEditorNode> chaptersCreated = new();
            for (int i = 1; i < choiceElements.Count; i++) {
                SEditorChoice choiceToCopy = choiceElements[i];
                var newNodePosition = sourceNode.position + offset * i;
                ChapterEditorNode newChapter = CreateNewChapterNode(graph, sourceNode, newNodePosition);
                CreateAndCopyChoiceElement(newChapter, choiceToCopy);
                
                StoryNodeEditor.RemoveElement(sourceNode, choiceToCopy);
                chaptersCreated.Add(newChapter);
            }

            LinkChaptersSequentially(sourceNode, chaptersCreated);
            AssetDatabase.SaveAssets();
        }
        
        public static void MergeChoices(StoryNode[] nodesToMerge) {
            if (!nodesToMerge.All(IsChoiceNode)) {
                Log.Important?.Error("Tried to merge non-choice nodes into a choice hub.");
                return;
            }
            
            var (topMostNode, bottomMostNode) = FindTopmostAndBottommostNodes(nodesToMerge);

            foreach (var node in nodesToMerge) {
                if (node == topMostNode) {
                    continue;
                }
                
                var choiceElements = node.elements.OfType<SEditorChoice>().ToList();
                foreach (var choiceToCopy in choiceElements) {
                    CreateAndCopyChoiceElement(topMostNode, choiceToCopy);
                    StoryNodeEditor.RemoveElement(node, choiceToCopy);
                }
                
                if (node == bottomMostNode) {
                    var bottomContinuationPort = bottomMostNode.GetPort(NodePort.FieldNameCompressed.Continuation);
                    var topContinuationPort = topMostNode.GetPort(NodePort.FieldNameCompressed.Continuation);
                    
                    if (bottomContinuationPort.IsConnected) {
                        topContinuationPort.Connect(bottomContinuationPort.Connection);
                    }
                }
                
                StoryGraphEditorUtils.RemoveNodeFromGraph(node.Graph, node);
            }
            AssetDatabase.SaveAssets();
        }
        

        // === Helpers ===
        static ChapterEditorNode CreateNewChapterNode(StoryGraph graph, StoryNode sourceNode, Vector2 position) {
            var newChapter = (ChapterEditorNode)NodeGraphEditor.CreateNode(typeof(ChapterEditorNode), graph);
            newChapter.name = "Choice";
            newChapter.position = position;
            newChapter.changedTint = true;
            newChapter.tint = sourceNode.tint;
            return newChapter;
        }
        
        static void CreateAndCopyChoiceElement(StoryNode chapterTarget, SEditorChoice choiceToCopy) {
            var newElement = (SEditorChoice)StoryNodeEditor.CreateElement(chapterTarget, typeof(SEditorChoice));
            CopyChoiceElement(choiceToCopy, newElement);
            CopyPortConnections(choiceToCopy, newElement);
            LocalizationUtils.CopyLocalizationData(choiceToCopy.Text.ID, newElement.Text.ID);
        }

        static void CopyChoiceElement(SEditorChoice choiceToCopy, SEditorChoice newElement) {
            var chapterTarget = newElement.Parent;
            var graphTarget = chapterTarget.graph;

            newElement.choice = new SingleChoice() {
                targetChapter = choiceToCopy.choice.targetChapter,
                isMainChoice = choiceToCopy.choice.isMainChoice,
                text = new LocString {
                    ID = GetNewLocTextId(newElement, graphTarget),
                }
            };
            graphTarget.StringTable.AddEntry(newElement.choice.text.ID, "Loc copy failed");

            newElement.audioClip = choiceToCopy.audioClip;
            newElement.spanFlag = choiceToCopy.spanFlag;
            newElement.span = choiceToCopy.span;
            newElement.choiceIcon = choiceToCopy.choiceIcon;
            newElement.genericParent = chapterTarget;
            newElement.hideFlags = HideFlags.HideInHierarchy;
        }
        
        static void CopyPortConnections(SEditorChoice source, SEditorChoice target) {
            if (source.TargetPort().IsConnected) {
                target.TargetPort().Connect(source.TargetPort().Connection);
            }

            if (source.ConditionPort().IsConnected) {
                foreach (var connection in source.ConditionPort().connections) {
                    target.ConditionPort().Connect(connection.Port);
                }
            }
        }

        static string GetNewLocTextId(SEditorChoice choiceElement, NodeGraph graph) {
            SerializedObject serializedObj = new(choiceElement);
            var singleChoiceProperty = serializedObj.FindProperty(nameof(choiceElement.choice));
            string localizationPrefix = graph.LocalizationPrefix;
            LocalizationUtils.ValidateTerm(singleChoiceProperty.FindPropertyRelative(nameof(choiceElement.choice.text)), localizationPrefix, out string newLocId);
            return newLocId;
        }
        
        static void LinkChaptersSequentially(StoryNode sourceNode, List<ChapterEditorNode> chaptersCreated) {
            var originExitConnection = sourceNode.GetPort(NodePort.FieldNameCompressed.Continuation).Connection;

            for (int i = 0; i <= chaptersCreated.Count; i++) {
                NodePort connectFrom, connectTo;
                if (i == 0) {
                    connectFrom = sourceNode.GetPort(NodePort.FieldNameCompressed.Continuation);
                    connectTo = chaptersCreated[i].GetPort(NodePort.FieldNameCompressed.Link);
                } else if (i == chaptersCreated.Count) {
                    connectFrom = chaptersCreated[i - 1].GetPort(NodePort.FieldNameCompressed.Continuation);
                    connectTo = originExitConnection;
                } else {
                    connectFrom = chaptersCreated[i - 1].GetPort(NodePort.FieldNameCompressed.Continuation);
                    connectTo = chaptersCreated[i].GetPort(NodePort.FieldNameCompressed.Link);
                }

                connectFrom.Connect(connectTo);
            }
        }
        
        static (StoryNode topmost, StoryNode bottommost) FindTopmostAndBottommostNodes(StoryNode[] storyNodes) {
            if (storyNodes == null || storyNodes.Length == 0) {
                return (null, null);
            }
            
            StoryNode topmostNode = storyNodes[0];
            StoryNode bottommostNode = storyNodes[0];
            
            for (int i = 1; i < storyNodes.Length; i++) {
                if (storyNodes[i].position.y < topmostNode.position.y) {
                    topmostNode = storyNodes[i];
                }
                if (storyNodes[i].position.y > bottommostNode.position.y) {
                    bottommostNode = storyNodes[i];
                }
            }
            
            return (topmostNode, bottommostNode);
        }
    }
}