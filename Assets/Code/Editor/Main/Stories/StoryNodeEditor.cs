using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Awaken.TG.Editor.Localizations;
using Awaken.TG.Editor.Main.Stories.Drawers;
using Awaken.TG.Editor.Utility.StoryGraphs;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Core.Attributes;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Reflections;
using Awaken.Utility.Collections;
using Awaken.Utility.Editor.SearchableMenu;
using Awaken.Utility.Extensions;
using Awaken.Utility.UI;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;
using Object = UnityEngine.Object;

namespace Awaken.TG.Editor.Main.Stories {
    [CustomNodeEditor(typeof(StoryNode))]
    public class StoryNodeEditor : NodeEditor {
        protected const string FoldString = "↑", UnfoldString = "↓";
        static readonly GUIContent RemoveButton = new("x");
        static readonly GUIContent MoveUpButton = new("\u25b2", "move up");
        static readonly GUIContent MoveDownButton = new("\u25bc", "move down");
        static readonly GUIContent RefreshButton = new("\u21bb", "reset");

        // === Fields & Properties

        static readonly GUIStyle HeaderStyle = new(EditorStyles.helpBox) { richText = true };
        static readonly int MiniButtonSize = 23;

        // steps that can be added/removed only with their nodes
        static Type[] s_nodeBoundSteps = { };

        protected StoryNode Node => (StoryNode)target;
        protected bool _elementReserializeNeeded;
        bool _reserializeInNextFrame;

        List<Type> _continuableElements = new() {
            typeof(SEditorText),
            typeof(SEditorChoice),
            typeof(SEditorRandomPick),
        };

        // === Constructor

        static StoryNodeEditor() {
            HeaderStyle.fontStyle = FontStyle.Bold;
            HeaderStyle.fontSize = EditorStyles.helpBox.fontSize + 2;
        }

        // === Colors

        public override Color GetTint() {
            if (Node.changedTint) {
                return Node.tint;
            }
            
            if (Node.toReview) {
                return new Color(0.753f, 0.114f, 0.812f);
            }

            if (Node.NodeElements.Any(ne => ne is SEditorBookmark)) {
                return new Color(0.667f, 0.584f, 0.455f);
            }

            if (Node.Type == NodeType.SimpleComment) {
                return new Color(0.21875f, 0.34765625f, 0.5f);
            }

            if (Node.Type == NodeType.Choice) {
                return new Color(0.2f, 0.4f, 0.2f);
            }

            if (Node.Type == NodeType.StoryStartChoice) {
                return new Color(0.5f, 0.7f, 0.5f);
            }

            if (Node.NodeElements.Any(n => n is SEditorGraphJump)) {
                return new Color(0.4f, 0.2f, 0.4f);
            }

            if (Node.NodeElements.Any(n => n is SEditorToDo)) {
                return new Color(0.5f, 0.5f, 0.2f);
            }

            if (Node.DynamicPorts.Any(p => p.direction == NodePort.IO.Output) || Node.NodeElements.Any(n=>n is SEditorChangeItemsQuantity)) {
                return new Color(0.2f, 0.2f, 0.4f);
            }

            return base.GetTint();
        }

        // === GUI
        public override float PostNameHeaderWidth => 70;
        
        protected override void PostNameHeaderGUI() {
            // HACK: Color picker blocks shortcuts like Ctrl+C and Ctrl+V, so we need this workaround
            if (Event.current.type is EventType.KeyDown) {
                return;
            }
            
            if (NodeEditorWindow.FarView) {
                return;
            }
            
            if (!Node.changedTint) {
                Node.tint = GetTint();
            }
            
            using var change = new TGGUILayout.CheckChangeScope();
            Node.tint = EditorGUILayout.ColorField(GUIContent.none, Node.tint, false, true, false, GUILayout.Width(25));
            if (change) {
                Node.changedTint = true;
            }

            if (GUILayout.Button(RefreshButton, new GUIStyle(EditorStyles.miniButton), GUILayout.Width(MiniButtonSize), GUILayout.Height(MiniButtonSize))) {
                Node.changedTint = false;
                Node.tint = new(0, 0, 0, 1);
            }
        }

        public override void OnBodyGUI() {
            serializedObject.Update();

            _elementReserializeNeeded = false;

            BeforeElements();
            DrawDebugExecuteButton();
            if (!Node.Folded) {
                DrawElements();
            }

            AfterElements();

            // reserialize
            if (_elementReserializeNeeded) {
                string path = AssetDatabase.GetAssetPath(Node);
                AssetDatabase.ForceReserializeAssets(new List<string> { path });

                serializedObject.Update();
                SerializedProperty stepsProp = serializedObject.FindProperty("elements");
                stepsProp.arraySize = Node.NodeElements.Count();
                int i = 0;
                foreach (NodeElement element in Node.NodeElements) {
                    stepsProp.GetArrayElementAtIndex(i).objectReferenceValue = element;
                    i++;
                }
            }

            if (_reserializeInNextFrame) {
                _elementReserializeNeeded = true;
                _reserializeInNextFrame = false;
            }

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void BeforeElements() {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Node.toReview = EditorGUILayout.Toggle("To review", Node.toReview);
            GUILayout.EndHorizontal();

            bool isFolded = Node.Folded;
            if (GUILayout.Button(isFolded ? UnfoldString : FoldString)) {
                Node.Folded = !isFolded;
            }
        }

        void DrawDebugExecuteButton() {
            if (Application.isPlaying && Node is ChapterEditorNode editorChapter && GUILayout.Button("Go to")) {
                foreach (var story in World.All<Story>()) {
                    if (story.Guid == editorChapter.Graph.GUID) {
                        story.Clear();
                        foreach (var chapter in story.Graph.chapters) {
                            if (chapter.EditorNode == editorChapter) {
                                story.JumpTo(chapter);
                                return;
                            }
                        }
                        return;
                    }
                }
                var oldStory = World.AllInOrder<Story>().LastOrDefault(s => s.InvolveHero);
                if (oldStory != null) {
                    StoryUtils.EndStory(oldStory);
                }
                var newstory = Story.StartStory(StoryConfig.Base(StoryBookmark.ToInitialChapter(new(Node.Graph.GUID)), typeof(VDialogue)));
                newstory.Clear();
                foreach (var chapter in newstory.Graph.chapters) {
                    if (chapter.EditorNode == editorChapter) {
                        newstory.JumpTo(chapter);
                        return;
                    }
                }
            }
        }

        protected virtual void DrawElements() {
            EditorGUIUtility.labelWidth = 84;
            // draw UI for all the steps, in order
            var nodeElements = Node.NodeElements.ToList();

            if (nodeElements.Count == 0) {
                DrawAddElementButton(-1);
                return;
            }
            
            for (int i = 0; i < nodeElements.Count; i++) {
                NodeElement element = nodeElements[i];
                NodeElement nextElement = i < nodeElements.Count - 1 ? nodeElements[i + 1] : null;
                DrawElementGUI(element);

                if (element.GetType() != nextElement?.GetType()) {
                    DrawAddElementButton(i);
                }

                if (i < nodeElements.Count - 1) {
                    EditorGUILayout.Space(14);
                }
            }
        }

        protected virtual void DrawAddElementButton(int index) {
            GUILayout.Label("", GUILayout.Height(5));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            Color old = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.93f, 0.26f);

            // draw "add any step" button
            if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(24))) {
                SearchableMenuPresenter wnd = ScriptableObject.CreateInstance<SearchableMenuPresenter>();
                foreach (Type elementType in AvailableElementTypes(Node)) {
                    string name = ElementName(elementType, ignorePath: false);
                    wnd.AddEntry(name, () => {
                        CreateElement(Node, elementType, index + 1);
                        _elementReserializeNeeded = true;
                    });
                }
                wnd.ShowAtCursorPosition();
            }

            // draw "continue elements block" button
            if (index >= 0 && index < Node.elements.Count) {
                Type lastElementType = Node.elements[index].GetType();
                if (_continuableElements.Contains(lastElementType)) {
                    GUIContent content = new ("\u25bc", $"Add {ElementName(lastElementType)}");
                    if (GUILayout.Button(content, EditorStyles.miniButton, GUILayout.Width(24))) {
                        CreateElement(Node, lastElementType, index + 1);
                        _reserializeInNextFrame = true;
                    }
                }
            }

            GUI.backgroundColor = old;

            GUILayout.EndHorizontal();
        }

        protected virtual void AfterElements() { }

        // === Drawing
        void DrawElementGUI(NodeElement element) {
            if (element == null) {
                RemoveElement(Node, element);
                AssetDatabase.SaveAssets();
                return;
            }

            if (element.genericParent == null) {
                // element lost in space, need to destroy it
                Object.DestroyImmediate(element, true);
                AssetDatabase.SaveAssets();
                return;
            }

            // draw step header
            DrawStepHeader(element);

            if (!_elementReserializeNeeded && !_reserializeInNextFrame) {
                // draw step body
                ElementDrawer.DrawElement(element);
            }
        }

        void DrawStepHeader(NodeElement element) {
            Rect rect = EditorGUILayout.GetControlRect(false, 23, EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
            GUI.Label(rect, ElementName(element.GetType()), HeaderStyle);
            if (element is EditorStep || element is SEditorStoryStartChoice) {
                // condition button/port
                Vector2 pos = new Vector2(rect.xMin - 14, rect.yMin + 6.5f);
                NodeGUIUtil.SmallPortField(pos, element.ConditionPort(), !element.ConditionNodes().Any());
            }

            if (!s_nodeBoundSteps.Any(st => st.IsInstanceOfType(element))) {
                // deleting steps
                rect = DrawRemoveStep(element, rect);
                // moving steps
                rect = DrawMoveDownStep(element, rect);
                rect = DrawMoveUpStep(element, rect);
            }
        }

        Rect DrawMoveUpStep(NodeElement element, Rect rect) {
            Rect buttonRect = new Rect(rect.xMin + rect.width - MiniButtonSize, rect.yMin, MiniButtonSize, MiniButtonSize);
            if (GUI.Button(buttonRect, MoveUpButton, EditorStyles.miniButton)) {
                int index = Node.elements.IndexOf(element);
                if (index > 0) {
                    Node.elements.Swap(index, index - 1);
                    _reserializeInNextFrame = true;
                }
            }

            return new Rect(rect.xMin, rect.yMin, rect.width - MiniButtonSize, MiniButtonSize);
        }

        Rect DrawMoveDownStep(NodeElement element, Rect rect) {
            Rect buttonRect = new Rect(rect.xMin + rect.width - MiniButtonSize, rect.yMin, MiniButtonSize, MiniButtonSize);
            if (GUI.Button(buttonRect, MoveDownButton, EditorStyles.miniButton)) {
                int index = Node.elements.IndexOf(element);
                if (index < Node.elements.Count - 1) {
                    Node.elements.Swap(index, index + 1);
                    _reserializeInNextFrame = true;
                }
            }

            return new Rect(rect.xMin, rect.yMin, rect.width - MiniButtonSize, MiniButtonSize);
        }

        Rect DrawRemoveStep(NodeElement element, Rect rect) {
            Rect buttonRect = new Rect(rect.xMin + rect.width - MiniButtonSize, rect.yMin, MiniButtonSize, MiniButtonSize);
            if (GUI.Button(buttonRect, RemoveButton, EditorStyles.miniButton)) {
                RemoveElement(Node, element);
                _reserializeInNextFrame = true;
            }

            return new Rect(rect.xMin, rect.yMin, rect.width - MiniButtonSize, MiniButtonSize);
        }

        public override void Copy(Node srcNode, Node dstNode) {
            base.Copy(srcNode, dstNode);
            CopyStatic(srcNode, dstNode);
        }

        public static void CopyStatic(Node srcNode, Node dstNode) {
            if (!(srcNode is StoryNode sourceNode) || !(dstNode is StoryNode destinationNode)) {
                return;
            }

            destinationNode.elements.Clear();
            foreach (NodeElement nodeElement in sourceNode.NodeElements) {
                var newElement = Object.Instantiate(nodeElement);
                newElement.hideFlags = HideFlags.HideInHierarchy;
                newElement.genericParent = destinationNode;
                newElement.CopyPortAssignments();
                newElement.ResetCache();
                destinationNode.elements.Add(newElement);
                AssetDatabase.AddObjectToAsset(newElement, destinationNode);
                UpdateGraphLocalizationTerms(dstNode, newElement);
            }
        }

        // === Helpers

        static void UpdateGraphLocalizationTerms(Node target, Object o) {
            foreach (var loc in LocalizationUtils.GetLocalizedProperties(o, false)) {
                LocalizationUtils.CopyTermDataToNewId((StoryGraph)target.graph, loc.FieldPath, loc.LocProperty, o, assetsSave: false);
            }
        }

        public static NodeElement CreateElement(StoryNode node, Type stepType, int index = -1) {
            NodeElement added = (NodeElement)ScriptableObject.CreateInstance(stepType);
            Undo.RegisterCreatedObjectUndo(added, $"Creating {stepType.Name}");
            Undo.RegisterCompleteObjectUndo(added, "Changing element");

            added.hideFlags = HideFlags.HideInHierarchy;
            added.name = ElementName(stepType);

            StoryGraphUndoUtil.Record(node);

            AssetDatabase.AddObjectToAsset(added, node);
            if (index == -1) {
                index = node.elements.Count;
            }

            node.elements.Insert(index, added);
            added.genericParent = node;
            added.OnAdded(node);
            return added;
        }

        public static void RemoveElement(StoryNode node, NodeElement element) {
            StoryGraphUndoUtil.Record(node);
            if (node.elements.Contains(element)) {
                node.elements.Remove(element);
            }

            if (element != null) {
                LocalizationUtils.RemoveAllLocalizedTerms(element, element.genericParent.Graph.StringTable);
                StoryGraphUndoUtil.Record(element);
                element.OnRemoved(node);
                Object.DestroyImmediate(element, true);
            }
        }

        static IEnumerable<Type> AvailableElementTypes(StoryNode node) {
            Type generic = node.GenericType;
            ElementTypeComparer comparer = new();
            return ReflectionExtension.SubClassesWithBaseOf(generic)
                .Where(type => !type.IsAbstract)
                .Where(type => !s_nodeBoundSteps.Any(st => st.IsAssignableFrom(type)))
                .OrderBy(type => type.GetCustomAttribute<ElementAttribute>()?.name, comparer);
        }

        class ElementTypeComparer : IComparer<string> {
            public int Compare(string stringA, string stringB) {
                stringA ??= string.Empty;
                stringB ??= string.Empty;
                string[] valueA = stringA.Split('/');
                string[] valueB = stringB.Split('/');

                if (valueA.Length <= 1 && valueB.Length > 1) {
                    return -1;
                }

                if (valueB.Length <= 1 && valueA.Length > 1) {
                    return 1;
                }

                return string.Compare(stringA, stringB, StringComparison.Ordinal);
            }
        }

        static OnDemandCache<Type, string> s_elementNameIgnorePath = new (t => ElementNameFactory(t));
        static OnDemandCache<Type, string> s_elementNameNotIgnorePath = new (t => ElementNameFactory(t, false));

        static string ElementName(Type stepType, bool ignorePath = true) {
            return ignorePath ? s_elementNameIgnorePath[stepType] : s_elementNameNotIgnorePath[stepType];
        }

        static string ElementNameFactory(Type stepType, bool ignorePath = true) {
            ElementAttribute attr = stepType.GetCustomAttribute<ElementAttribute>();
            if (attr != null) {
                if (ignorePath) {
                    int slashIndex = attr.name.LastIndexOf('/');
                    return attr.name.Substring(slashIndex + 1);
                } else {
                    return attr.name;
                }
            } else {
                return stepType.Name;
            }
        }
    }
}