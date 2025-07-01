using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Helpers;
using Awaken.TG.Editor.Helpers.Tags;
using Awaken.TG.Editor.Localizations;
using Awaken.TG.Editor.Main.Fmod;
using Awaken.TG.Editor.Main.Stories.Steps;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Editor.Utility.Screenshotting;
using Awaken.TG.Editor.Utility.StoryGraphs;
using Awaken.TG.Editor.Utility.StoryGraphs.Converter;
using Awaken.TG.Editor.Utility.StoryGraphs.Toolset;
using Awaken.TG.Editor.Utility.StoryGraphs.Toolset.CustomWindow;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Conditions.Core;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes.List;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Enums;
using Awaken.Utility.UI;
using JetBrains.Annotations;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Vendor.xNode.Scripts.Editor.GenericMenus;
using XNode;
using XNodeEditor;

namespace Awaken.TG.Editor.Main.Stories {
    [UsedImplicitly, CustomNodeGraphEditor(typeof(StoryGraph), "StoryGraph.settings")]
    public class StoryGraphEditor : NodeGraphEditor {
        static Texture2D[] s_backgrounds;
        public bool wasRepaintedForScreenshot;

        // -- Target
        SerializedObject _serializedObject;
        StoryGraph Target => target as StoryGraph;
        public override float BlackBoardWidth => base.BlackBoardWidth + 35;

        [MenuItem("TG/Graphs/Open running Story Graph")]
        public static void OpenRunningStoryGraph() {
            StoryGraph graph = World.AllInOrder<Story>().LastOrDefault()?.EDITOR_Graph;
            if (graph != null) {
                NodeEditorWindow.Open(graph);
            } else {
                Log.Important?.Warning("No compatible story opened");
            }
        }

        [MenuItem("TG/Graphs/Refresh used sound banks on all Story Graphs")]
        public static void RefreshUsedSoundBanksOnAllStoryGraphs() => StoryGraphFmodEditorUtils.RefreshUsedSoundBanksOnAllStoryGraphs();

        public override void OnOpen() {
            StoryGraph template = (StoryGraph)target;
            if (!template.nodes.Any(n => n is StoryStartEditorNode)) {
                CreateNode(typeof(StoryStartEditorNode), Vector2.zero);
            }

            NodeEditorWindow.current.titleContent = new GUIContent($"{template.name} - story");
        }

        public override void OnUpdate() {
            LocStringGUIUtils.ClearLocalizationCache();
            if (Application.isPlaying && World.All<Story>().Any(s => s.EDITOR_Graph?.GUID == Target.GUID)) {
                // Repaint when debugging story graph
                NodeEditorWindow.current.Repaint();
            }
        }

        public override void OnGUI() {
            wasRepaintedForScreenshot = true;
            // focus on current step
            Event e = Event.current;
            // Ctrl+Q
            if (e.type is EventType.KeyDown && e.keyCode == KeyCode.Q && e.modifiers.HasFlag(EventModifiers.Control)) {
                StoryGraph graph = (StoryGraph)NodeEditorWindow.current.graph;
                Node node = graph.LastExecutedStep?.genericParent;
                if (node != null) {
                    NodeEditorWindow.current.CenterOnNode(node);
                }
            }

            // Cache new object if old is not valid
            if (_serializedObject == null) {
                _serializedObject = new SerializedObject(Target);
            }

            // Get outside changes
            _serializedObject.Update();
            // draw blackboard
            DrawBlackBoard();
            // Apply inside changes
            _serializedObject.ApplyModifiedProperties();
        }

        // === Blackboard
        protected override void OnBlackBoardScrollable() {
            // draw top-left toggles
            float cachedWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 180;
            Target.sharedBetweenMultipleNPCs = EditorGUILayout.Toggle("Shared between multiple NPCs", Target.sharedBetweenMultipleNPCs);
            EditorGUIUtility.labelWidth = cachedWidth;

            EditorGUILayout.Space(15);

            var property = _serializedObject.FindProperty("variables");
            int i = 0;
            GUIStyle style = new();

            s_backgrounds ??= CreateBackgrounds();

            property.DrawArray(child => {
                style.normal.background = s_backgrounds[i % 2];
                i++;

                EditorGUILayout.BeginHorizontal(style);
                EditorGUILayout.PropertyField(child.FindPropertyRelative(nameof(VariableDefine.name)));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal(style);
                EditorGUILayout.LabelField("Value:", GUILayout.Width(65));
                EditorGUILayout.PropertyField(child.FindPropertyRelative(nameof(VariableDefine.defaultValue)));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginVertical(style);
                var tagsContextProp = child.FindPropertyRelative(nameof(VariableDefine.context));
                TagsEditing.Show(tagsContextProp, TagsCategory.Context, (int)BlackBoardWidth - 2);
                var listContextProp = child.FindPropertyRelative(nameof(VariableDefine.contexts));
                ListEditing.Show(listContextProp, ListEditOption.Buttons);
                EditorGUILayout.EndVertical();
            });

            property = _serializedObject.FindProperty(nameof(StoryGraph.variableReferences));
            EditorGUILayout.PropertyField(property);

            EditorGUILayout.Space(15);
            DrawAllowedActors();
            EditorGUILayout.Space(15);

            if (GUILayout.Button("Refresh used sound banks")) {
                StoryGraphFmodEditorUtils.RefreshStoryGraphsUsedSoundBanks(Target);
            }
        }

        void DrawAllowedActors() {
            var style = new GUIStyle();
            var i = 0;
            var actorsRegister = AssetDatabase.LoadAssetAtPath<GameObject>(ActorsRegister.Path).GetComponent<ActorsRegister>();
            var property = _serializedObject.FindProperty(nameof(StoryGraph.allowedActors));
            property.DrawArray(child => {
                style.normal.background = s_backgrounds[i % 2];
                i++;

                var guidProperty = child.FindPropertyRelative(nameof(ActorRef.guid));
                var path = actorsRegister.Editor_GetPathFromGUID(guidProperty.stringValue);
                EditorGUILayout.BeginHorizontal(style);
                var label = new GUIContent(path.Split('/').Last(), guidProperty.stringValue);
                EditorGUILayout.LabelField(label, GUILayout.Width(BlackBoardWidth * 0.35f));
                EditorGUILayout.PropertyField(child, GUIContent.none, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();
            });

            DrawAutofillActors(property);
        }

        void DrawAutofillActors(SerializedProperty allowedActorsProperty) {
            var autofillActorsProperty = _serializedObject.FindProperty(nameof(StoryGraph.autofillActors));
            if (allowedActorsProperty.arraySize == 1 && !IsHeroOrNoneFirstActor(allowedActorsProperty)) {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(autofillActorsProperty);
                var hasChange = EditorGUI.EndChangeCheck();
                if (hasChange && autofillActorsProperty.boolValue) {
                    FillSTextWithActors();
                }
            } else {
                autofillActorsProperty.boolValue = false;
            }

            bool IsHeroOrNoneFirstActor(SerializedProperty actors) {
                var firstActorProp = actors.GetArrayElementAtIndex(0);
                var actorGuidProp = firstActorProp.FindPropertyRelative(nameof(ActorRef.guid));
                var actorGuid = actorGuidProp.stringValue;
                return ActorRefUtils.IsHeroGuid(actorGuid) || ActorRefUtils.IsNoneGuid(actorGuid);
            }
        }

        static Texture2D[] CreateBackgrounds() {
            Texture2D background1 = new Texture2D(1, 1);
            Color evenColor = Color.white * 0.8f;
            background1.SetPixel(0, 0, evenColor);
            background1.Apply();

            Texture2D background2 = new Texture2D(1, 1);
            Color oddColor = Color.white * 0.5f;
            background2.SetPixel(0, 0, oddColor);
            background2.Apply();

            Texture2D[] textures = { background1, background2 };
            return textures;
        }

        protected override void OnBlackBoardBottom() {
            var oldEnabled = GUI.enabled;

            using (new DisableGUIScope(!Target.nodes.OfType<StoryNode>().Any(node => node.toReview))) {
                if (GUILayout.Button("Mark all nodes as reviewed")) {
                    Target.nodes.OfType<StoryNode>().ForEach(node => node.toReview = false);
                }
            }

            if (GUILayout.Button("Open Story Graph Toolset window")) {
                // StoryGraphToolsetEditor.OpenWindow();
            }

            if (GUILayout.Button("Update all voice overs")) {
                GraphConverterUtils.UpdateVoiceOvers(Target);
            }

            if (GUILayout.Button("Take screenshot")) {
                StoryGraphScreenshotter.TakeScreenshot(this);
            }

            if (GUILayout.Button("List all nodes to review from all story graphs")) {
                StoryNodeTasksFinder tool = new();
                // StoryGraphPopupToolEditor.OpenWindowAndExecute(tool, "Nodes to review");
            }

            using (new DisableGUIScope(!(oldEnabled && Application.isPlaying))) {
                if (GUILayout.Button("Start Story")) {
                    StoryGraphInspectorEditor.StartStory(Target);
                }

                if (GUILayout.Button("End Story")) {
                    StoryGraphInspectorEditor.EndStory(Target);
                }
            }
        }

        // === Adding nodes
        public override void AddContextMenuItems(INodeGenericMenu menu, Type compatibleType = null, NodePort.IO direction = NodePort.IO.Input) {
            NodePort draggedOutput = window.draggedOutput;

            foreach (NodeType nodeType in RichEnum.AllValuesOfType<NodeType>()) {
                CustomMenuItem(menu, nodeType, draggedOutput);
            }

            base.AddContextMenuItems(menu, compatibleType, direction);
        }

        void CustomMenuItem(INodeGenericMenu menu, NodeType nodeType, NodePort draggedOutput) {
            Vector2 pos = NodeEditorWindow.current.WindowToGridPosition(Event.current.mousePosition);
            if (nodeType.Type == null) {
                menu.AddSeparator(nodeType.Name);
            } else {
                menu.AddItem(new GUIContent($"{nodeType.Name}"), false, () => { CreateNode(nodeType.Type, pos, ConfigNewNode(nodeType, draggedOutput)); });
            }
        }

        public static Action<Node> ConfigNewNode(NodeType nodeType, NodePort draggedOutput = null) {
            return node => {
                StoryNode storyNode = (StoryNode)node;
                Type storyNodeType = storyNode.GetType();
                nodeType = nodeType ?? RichEnum.AllValuesOfType<NodeType>().FirstOrDefault(nType => nType.Type == storyNodeType);
                if (nodeType != null) {
                    storyNode.name = nodeType.Name.Split("/").Last();
                    foreach (var stepType in nodeType.InitialSteps) {
                        StoryNodeEditor.CreateElement(storyNode, stepType);
                    }

                    storyNode.nodeType = new RichEnumReference(nodeType);
                }

                // Try connect created node to current dragged output
                if (draggedOutput != null) {
                    foreach (NodePort input in node.Inputs) {
                        if (draggedOutput.CanConnectTo(input)) {
                            draggedOutput.Connect(input);
                            break;
                        }
                    }
                }
            };
        }

        // === Removing nodes
        public override void RemoveNode(Node node) {
            LocalizationTools.RemoveAllStringTableEntriesFromNode(node);
            if (node is StoryNode storyNode) {
                foreach (NodeElement element in storyNode.elements.ToList()) {
                    StoryNodeEditor.RemoveElement(storyNode, element);
                }
            }

            base.RemoveNode(node);
        }

        // === Operations
        void FillSTextWithActors() {
            _serializedObject.ApplyModifiedProperties();

            var talker = Target.allowedActors[0];
            var listener = DefinedActor.Hero.ActorRef;
            var undoName = $"Autofill texts actors - {Target.name} with {talker.guid} -> Hero";

            var texts = Target.nodes.OfType<StoryNode>().SelectMany(n => n.elements.OfType<SEditorText>()).ToArray();
            foreach (var text in texts) {
                var changeTalker = text.actorRef.IsNone();
                var changeListener = text.targetActorRef.IsNone();
                if (changeTalker || changeListener) {
                    Undo.RegisterCompleteObjectUndo(text, undoName);
                }

                if (changeTalker) {
                    text.actorRef = talker;
                    STextEditor.UpdateSTextActorMetaData(text);
                }

                if (changeListener) {
                    text.targetActorRef = listener;
                }
            }

            _serializedObject.Update();
        }

        // === Preferences
        public override NodeEditorPreferences.Settings GetDefaultPreferences() {
            return new NodeEditorPreferences.Settings() {
                gridLineColor = new Color32(61, 61, 61, 255),
                portTooltips = false,
                typeColors = new Dictionary<string, Color>() {
                    { typeof(ConditionsEditorNode[]).PrettyName(), new Color(1f, 0.3f, 0.3f) },
                    { typeof(ConditionsEditorNode).PrettyName(), new Color(1f, 0.3f, 0.3f) },
                    { typeof(ChapterEditorNode).PrettyName(), new Color(0.7f, 0.7f, 1f) },
                    { typeof(StoryNode).PrettyName(), new Color(0.7f, 0.7f, 1f) },
                    { typeof(StoryNode[]).PrettyName(), new Color(0.7f, 0.7f, 1f) },
                    { typeof(Node).PrettyName(), new Color(0.7f, 0.7f, 1f) },
                    { typeof(Node[]).PrettyName(), new Color(0.6f, 0.6f, 1f) }
                },
            };
        }

        [OnOpenAsset(0)]
        public static bool OnOpen(int instanceID, int line) {
            NodeElement nodeElement = EditorUtility.InstanceIDToObject(instanceID) as NodeElement;
            StoryNode node = nodeElement?.genericParent;
            if (node != null && node.graph != null) {
                NodeEditorWindow.Open(node.graph).CenterOnNode(node);
                Selection.objects = new UnityEngine.Object[] { node };
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// This class does exactly the same thing as StoryGraphEditor.RemoveNode, but it's static, so it can be called from other classes.
    /// </summary>
    public static class StoryGraphEditorUtils {
        public static void RemoveNodeFromGraph(StoryGraph target, Node node) {
            LocalizationTools.RemoveAllStringTableEntriesFromNode(node);
            if (node is StoryNode storyNode) {
                foreach (NodeElement element in storyNode.elements.ToList()) {
                    StoryNodeEditor.RemoveElement(storyNode, element);
                }
            }
            
            if (!CanRemove(node)) return;

            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(node, out var guid, out long fileId);
            // Remove the node
            Undo.RecordObject(node, "Delete Node");
            Undo.RecordObject(target, "Delete Node");
            foreach (var port in node.Ports) {
                foreach (var conn in port.GetConnections().Where(n => n != null).Distinct()) {
                    Undo.RecordObject(conn.node, "Delete Node");
                }
            }
            target.RemoveNode(node);
            target.removedNodes.Add(fileId);
            Undo.DestroyObjectImmediate(node);
            if (NodeEditorPreferences.GetSettings().autoSave) AssetDatabase.SaveAssets();
            return;

            bool CanRemove(Node node) {
                // Check graph attributes to see if this node is required
                Type graphType = target.GetType();
                NodeGraph.RequireNodeAttribute[] attribs = Array.ConvertAll(
                    graphType.GetCustomAttributes(typeof(NodeGraph.RequireNodeAttribute), true), x => x as NodeGraph.RequireNodeAttribute);
                if (attribs.Any(x => x.Requires(node.GetType()))) {
                    if (target.nodes.Count(x => x.GetType() == node.GetType()) <= 1) {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}