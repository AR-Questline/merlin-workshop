using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Assets.Templates;
using Awaken.TG.Editor.Localizations;
using Awaken.TG.Editor.Main.Stories;
using Awaken.TG.Editor.Main.Stories.Steps;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Stories.Steps.Helpers;
using Awaken.TG.Main.TimeLines.Markers;
using Awaken.TG.Utility.Reflections;
using Awaken.Utility;
using Awaken.Utility.Collections;
using CrazyMinnow.SALSA;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using XNode;
using XNodeEditor;

namespace Awaken.TG.Editor.Utility.StoryGraphs.Converter {
    public static class GraphConverterUtils {
        // === Utility
        
        //MenuItem("TG/Audio/Update All StoryGraphs VoiceOvers")]
        // Temporarily disabled due to current project complexity.
        // At this stage, the process takes an unreasonably long time, and accidental clicks can force a Unity restart.
        public static void UpdateAllStoryGraphsVoiceOvers() {
            UpdateVoiceOvers(true, AllGraphsOfType<StoryGraph>().ToArray());
        }
                
        [MenuItem("Assets/Update Selected StoryGraphs VoiceOvers",  priority = 99990),  MenuItem("TG/Audio/Update Selected StoryGraphs VoiceOvers")]
        public static void UpdateSelectedStoryGraphsVoiceOvers() {
            StoryGraph[] graphs = Selection.objects.OfType<StoryGraph>().ToArray();
            UpdateVoiceOvers(true, graphs);
        }
                        
        [MenuItem("Assets/Update Selected StoryGraphs VoiceOvers", true)]
        public static bool IsSelectedStoryGraph() { 
            return Selection.objects.OfType<StoryGraph>().Any();
        }
        
        [MenuItem("TG/Audio/Update All StoryGraphs VoiceOvers References")]
        public static void UpdateAllStoryGraphsVoiceOversReferences() {
            UpdateVoiceOvers(false, AllGraphsOfType<StoryGraph>().ToArray());
        }
        
        [MenuItem("TG/Audio/Update Selected StoryGraphs VoiceOvers References")]
        public static void UpdateSelectedStoryGraphsVoiceOversReferences() {
            StoryGraph[] graphs = Selection.objects.OfType<StoryGraph>().ToArray();
            UpdateVoiceOvers(false, graphs);
        }

        public static void UpdateVoiceOvers(bool forceGeneration, params StoryGraph[] graphs) {
            int i = 0;
            AssetDatabase.StartAssetEditing();
            try {
                foreach (StoryGraph graph in graphs) {
                    bool cancel = EditorUtility.DisplayCancelableProgressBar("Updating VoiceOvers", $"Converting: {graph.name}, Progress: {i}/{graphs.Length}", i / (float)graphs.Length);
                    if (cancel) {
                        break;
                    }
                    var sTexts = ExtractNodes<ChapterEditorNode>(graph).ExtractElements<ChapterEditorNode, SEditorText>().Select(trio => trio.element);
                    foreach (SEditorText sText in sTexts) {
                        STextEditor.SetupAudio(sText, forceGeneration, true, !forceGeneration);
                    }
                    EditorUtility.SetDirty(graph);
                    i++;
                }
            } finally {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }
        }
        
        //[MenuItem("TG/Graphs/Update gestures and emotions")]
        public static void UpdateAllStoryGraphsGestures() {
            UpdateGestures(AllGraphsOfType<StoryGraph>().ToArray());
        }
        
        public static void UpdateGestures(params StoryGraph[] graphs) {
            foreach (StoryGraph graph in graphs) {
                var sTexts = ExtractNodes<ChapterEditorNode>(graph).ExtractElements<ChapterEditorNode, SEditorText>().Select(trio => trio.element);
                bool anyModified = false;
                foreach (SEditorText sText in sTexts) {
                    anyModified = UpdateGesturesAndEmotionsInSText(sText, anyModified);
                }

                if (anyModified) {
                    EditorUtility.SetDirty(graph);
                }
            }
            
            Awaken.Utility.Debugging.Log.Important?.Info("Gestures and emotions update: done.");
        }

        static bool UpdateGesturesAndEmotionsInSText(SEditorText sEditorText, bool anyModified) {
            var textConfig = TextConfig.WithText(sEditorText.text);
            string oldGesture = textConfig.GestureKey;
            string oldEmotion = textConfig.EmoteKey;
            
            SerializedProperty serializedProperty = new SerializedObject(sEditorText).FindProperty(nameof(sEditorText.text));
            StringTableCollection stringTableCollection = LocalizationUtils.DetermineStringTable(serializedProperty);
            if (stringTableCollection is null) {
                return anyModified;
            }

            var stringTable = stringTableCollection.GetTable(LocalizationSettings.ProjectLocale.Identifier) as StringTable;
            var locEntry = LocalizationHelper.GetTableEntry(sEditorText.text.ID).entry;
            if (!string.IsNullOrWhiteSpace(oldGesture)) {
                string cleanText = locEntry.LocalizedValue
                    .Replace($" {oldGesture} ", " ")
                    .Replace($" {oldGesture}", "")
                    .Replace($"{oldGesture} ", "")
                    .Replace(oldGesture, "");

                LocalizationUtils.ChangeTextTranslation(sEditorText.text.ID, cleanText, stringTable);
                sEditorText.gestureKey = oldGesture;
                anyModified = true;
            }
            
            if (!string.IsNullOrWhiteSpace(oldEmotion)) {
                string cleanText = locEntry.LocalizedValue
                    .Replace($" {oldEmotion} ", " ")
                    .Replace($" {oldEmotion}", "")
                    .Replace($"{oldEmotion} ", "")
                    .Replace(oldEmotion, "");
                
                LocalizationUtils.ChangeTextTranslation(sEditorText.text.ID, cleanText, stringTable);
                EmotionData emotionData = new(0, 0, ExpressionComponent.ExpressionHandler.RoundTrip, oldEmotion, EmotionState.Enable);
                sEditorText.emotions.Add(emotionData);
                anyModified = true;
            }

            return anyModified;
        }
        
        [MenuItem("TG/Graphs/Update ActorsRefs in Localizations")]
        public static void UpdateAllStoryGraphsActorsReferences() {
            UpdateActors(AllGraphsOfType<StoryGraph>().ToArray());
        }
        
        static void UpdateActors(params StoryGraph[] graphs) {
            int i = 0;
            float graphsLength = graphs.Length;
            AssetDatabase.StartAssetEditing();
            foreach (StoryGraph graph in graphs) {
                EditorUtility.DisplayProgressBar("Updating Actors", $"Converting: {graph.name}, Progress: {i}/{graphsLength}", i/graphsLength);
                var sTexts = ExtractNodes<ChapterEditorNode>(graph).ExtractElements<ChapterEditorNode, SEditorText>().Select(trio => trio.element);
                foreach (SEditorText sText in sTexts) {
                    STextEditor.UpdateSTextActorMetaData(sText);
                }
                i++;
            }
            Awaken.Utility.Debugging.Log.Important?.Info("Finished updating actors references");
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
        }

        // -- Main

        // Obtain stories
        public static IEnumerable<NodeGraph> AllGraphs() {
            IEnumerable<NodeGraph> graphs = TemplatesSearcher.FindAllOfType<NodeGraph>();
            return graphs;
        }
        
        public static IEnumerable<T> AllGraphsOfType<T>() where T : NodeGraph {
            IEnumerable<T> graphs = TemplatesSearcher.FindAllOfType<T>();
            return graphs;
        }

        public static IEnumerable<NodeGraph> FilteredGraphs(Func<NodeGraph, bool> filter) {
            return TemplatesSearcher.FindAllOfType<NodeGraph>().Where(filter);
        }

        /// <summary>
        /// Select all stories which contains 'namePart' in graph name
        /// </summary>
        public static IEnumerable<NodeGraph> WhereNameContains(string namePart) {
            return FilteredGraphs(graph =>
                graph.name.IndexOf(namePart, StringComparison.InvariantCultureIgnoreCase) >= 0);
        }

        public static IEnumerable<NodeGraph> AllGraphsWithNode<T>() where T : Node {
            return FilteredGraphs(HasNode<T>);
        }

        public static IEnumerable<NodeGraph> AllStoriesWithElement<T>() where T : NodeElement {
            return FilteredGraphs(HasElement<T>);
        }
        
        // Obtain nodes and elements
        public static IEnumerable<GraphNodePair<T>> AllNodes<T>() where T : Node {
            return AllGraphs().SelectMany(ExtractNodes<T>);
        }

        public static IEnumerable<GraphNodePair<T>> AllNodesWithElement<T, TElement>()
            where T : StoryNode where TElement : NodeElement {
            return AllGraphs().SelectMany(ExtractNodes<T>).Where(graphNode => HasElement<TElement>(graphNode.node))
                .Distinct();
        }

        /// <summary>
        /// Return all elements of type TElement in nodes of type TNode
        /// </summary>
        /// <remarks>
        /// To obtain all elements of type TElement from all nodes call like AllElements&amp;lt;StoryNode, TElement&amp;gt;
        /// </remarks>
        public static IEnumerable<GraphNodeElementTrio<TNode, TElement>> AllElements<TNode, TElement>()
            where TNode : StoryNode where TElement : NodeElement {
            return AllNodesWithElement<TNode, TElement>().SelectMany(ExtractElement<TNode, TElement>);
        }
        
        /// <summary>
        /// Return all elements implementing TInterface in nodes of type TNode
        /// </summary>
        public static IEnumerable<GraphNodeElementTrio<TNode, NodeElement>> AllElementsWithInterface<TNode, TInterface>() where TNode : StoryNode {
            return AllNodes<TNode>()
                .SelectMany(pair => pair.node.elements)
                .Where(element => element is TInterface)
                .Select(element => GraphNodeElementTrio<TNode, NodeElement>
                    .Construct(new GraphNodePair<TNode>() {graph = element.genericParent.Graph, node = element.genericParent as TNode }, element));
        }
        
        /// <summary>
        /// Return all elements of type TElement in nodes of type TNode ordered by graph name
        /// </summary>
        public static IEnumerable<GraphNodeElementTrio<TNode, TElement>> AllElementsOrderByGraphName<TNode, TElement>()
            where TNode : StoryNode where TElement : NodeElement {
            return AllElements<TNode, TElement>().OrderBy(trio => trio.graph.name);
        }

        // Modify Elements
        /// <summary>
        /// Swap elements (include create new element, register in node and remove odl element)
        /// </summary>
        public static IEnumerable<GraphNodeElementTrio<TNode, TNewElement>>
            SwapElements<TNode, TElement, TNewElement>(
                this IEnumerable<GraphNodeElementTrio<TNode, TElement>> originalElements,
                Action<TElement, TNewElement> copyAction)
            where TNewElement : NodeElement where TNode : StoryNode where TElement : NodeElement {
            return originalElements.Flush().Select(originalElement => {
                var newElementTrio = new GraphNodeElementTrio<TNode, TNewElement>()
                    {graph = originalElement.graph, node = originalElement.node};
                newElementTrio.element =
                    (TNewElement) StoryNodeEditor.CreateElement(originalElement.node, typeof(TNewElement));
                copyAction(originalElement.element, newElementTrio.element);
                SwapElement(originalElement.node, originalElement.element, newElementTrio.element);
                return newElementTrio;
            });
        }

        public static IEnumerable<GraphNodeElementTrio<TNode, TElement>> ChangeElements<TNode, TElement>(
            this IEnumerable<GraphNodeElementTrio<TNode, TElement>> originalElements,
            Action<TElement> changeAction)
            where TNode : StoryNode where TElement : NodeElement {
            return originalElements.Flush().Select(originalElement => {
                changeAction(originalElement.element);
                return originalElement;
            });
        }

        public static IEnumerable<GraphNodePair<TNode>> DeleteElements<TNode, TElement>(
            this IEnumerable<GraphNodeElementTrio<TNode, TElement>> originalElements,
            Func<TElement, bool> predicate)
            where TNode : StoryNode where TElement : NodeElement {
            return originalElements.Flush().Where(originalElement => predicate(originalElement.element)).Select(
                originalElement => {
                    DeleteElement(originalElement.node, originalElement.element);
                    return new GraphNodePair<TNode>() {graph = originalElement.graph, node = originalElement.node};
                });
        }

        // Modify Nodes
        /// <summary>
        /// Swap nodes (include create new node, register in graph and remove old node)
        /// </summary>
        public static IEnumerable<GraphNodePair<TNewNode>> SwapNodes<TNode, TNewNode>(
            this IEnumerable<GraphNodePair<TNode>> originalNodes, Action<TNode, TNewNode> copyAction)
            where TNode : StoryNode where TNewNode : StoryNode {
            return originalNodes.Flush().Select(originalNode => {
                var newNodePair = new GraphNodePair<TNewNode>() {graph = originalNode.graph};
                newNodePair.node = (TNewNode) NodeGraphEditor.CreateNode(typeof(TNewNode), newNodePair.graph);
                StoryGraphEditor.ConfigNewNode(null)(newNodePair.node);
                copyAction(originalNode.node, newNodePair.node);
                SwapNodes(originalNode.graph, originalNode.node, newNodePair.node);
                return newNodePair;
            });
        }

        public static IEnumerable<GraphNodePair<TNode>> ConvertNode<TNode>(
            this IEnumerable<GraphNodePair<TNode>> nodes, Action<TNode> convertAction)
            where TNode : Node {
            return nodes.Flush().Select(trio => {
                convertAction(trio.node);
                return trio;
            });
        }

        public static IEnumerable<GraphNodeElementTrio<TNode, TNewElement>> AddElement<TNode, TNewElement>(
            this IEnumerable<GraphNodePair<TNode>> nodes, Action<TNewElement> configAction)
            where TNode : StoryNode where TNewElement : NodeElement {
            return nodes.Flush().Select(originalNode => {
                var newElementTrio = new GraphNodeElementTrio<TNode, TNewElement>()
                    {graph = originalNode.graph, node = originalNode.node};
                newElementTrio.element =
                    (TNewElement) StoryNodeEditor.CreateElement(originalNode.node, typeof(TNewElement));
                configAction(newElementTrio.element);
                return newElementTrio;
            });
        }

        public static IEnumerable<GraphNodeElementTrio<TNode, TElement>> ConvertElement<TNode, TElement>(
            this IEnumerable<GraphNodeElementTrio<TNode, TElement>> nodes, Action<TElement> convertAction)
            where TNode : StoryNode where TElement : NodeElement {
            return nodes.Flush().Select(trio => {
                convertAction(trio.element);
                return trio;
            });
        }

        // -- Debug
        public static IEnumerable<NodeGraph> Log(this IEnumerable<NodeGraph> graphs) {
            var storyGraphs = graphs.ToList();
            storyGraphs.ForEach(graph => Awaken.Utility.Debugging.Log.Important?.Info(graph.name));
            return storyGraphs;
        }

        public static IEnumerable<GraphNodePair<T>> Log<T>(this IEnumerable<GraphNodePair<T>> graphNodePairs)
            where T : Node {
            var nodePairs = graphNodePairs.ToList();
            nodePairs.ForEach(graphNodePair =>
                Awaken.Utility.Debugging.Log.Important?.Info($"{graphNodePair.graph.name} - {graphNodePair.node.name}"));
            return nodePairs;
        }

        public static IEnumerable<GraphNodeElementTrio<TNode, TElement>> Log<TNode, TElement>(
            this IEnumerable<GraphNodeElementTrio<TNode, TElement>> graphNodeElementTrio)
            where TNode : StoryNode where TElement : NodeElement {
            var elementTrios = graphNodeElementTrio.ToList();
            elementTrios.ForEach(elementTrio =>
                Awaken.Utility.Debugging.Log.Important?.Info($"{elementTrio.graph.name} - {elementTrio.node.name} - {elementTrio.element.name}"));
            return elementTrios;
        }

        // -- Helpers

        public static bool HasNode<T>(NodeGraph story) where T : Node {
            return story.nodes.Any(node => node is T);
        }

        public static bool HasElement<T>(NodeGraph story) where T : NodeElement {
            return story.nodes.Any(node => {
                if (node is StoryNode storyNode) {
                    return HasElement<T>(storyNode);
                }

                return false;
            });
        }

        public static bool HasElement<T>(GraphNodePair<StoryNode> graphNode) where T : NodeElement {
            return HasElement<T>(graphNode.node);
        }

        public static bool HasElement<T>(StoryNode node) where T : NodeElement {
            return node.elements.Any(elem => elem is T);
        }

        public static IEnumerable<GraphNodePair<T>> ExtractNodes<T>(NodeGraph graph) where T : Node {
            return graph.nodes.Where(node => node is T)
                .Select(node => new GraphNodePair<T>() {graph = graph, node = (T) node});
        }

        public static IEnumerable<GraphNodePair<TNode>> ExtractNodes<TNode, TElement>(
            this IEnumerable<GraphNodeElementTrio<TNode, TElement>> graphTrios)
            where TNode : Node where TElement : NodeElement {
            return graphTrios.Select(trio => new GraphNodePair<TNode>() {graph = trio.graph, node = trio.node})
                .Distinct().Flush();
        }

        public static IEnumerable<GraphNodeElementTrio<TNode, TElement>> ExtractElements<TNode, TElement>(
            this IEnumerable<GraphNodePair<TNode>> graphNodes)
            where TNode : StoryNode where TElement : NodeElement {
            return graphNodes.SelectMany(ExtractElement<TNode, TElement>).Flush();
        }

        public static IEnumerable<GraphNodeElementTrio<TNode, TElement>>
            ExtractElement<TNode, TElement>(GraphNodePair<TNode> graphNode)
            where TNode : StoryNode where TElement : NodeElement {
            return graphNode.node.elements.Where(element => element is TElement)
                .Select(element => GraphNodeElementTrio<TNode, TElement>.Construct(graphNode, (TElement) element));
        }

        /// <summary>
        /// Used to flush all changes to collection
        /// </summary>
        public static IEnumerable<T> Flush<T>(this IEnumerable<T> requiredTransactions) {
            return requiredTransactions.ToList();
        }

        public static void SwapElement(StoryNode node, NodeElement oldElement, NodeElement newElement) {
            // StoryNodeEditor.CreateElement automatic append new element after create, but we want replace
            if (node.elements.Contains(newElement)) {
                node.elements.Remove(newElement);
            }

            int oldElementIndex = node.elements.IndexOf(oldElement);
            node.elements[oldElementIndex] = newElement;

            StoryNodeEditor.RemoveElement(node, oldElement);
        }

        public static void DeleteElement(StoryNode node, NodeElement element) {
            StoryNodeEditor.RemoveElement(node, element);
        }

        public static void SwapNodes(NodeGraph graph, StoryNode oldNode, StoryNode newNode) {
            if (graph.nodes.Contains(newNode)) {
                graph.nodes.Remove(newNode);
            }

            int oldElementIndex = graph.nodes.IndexOf(oldNode);
            graph.nodes[oldElementIndex] = newNode;

            newNode.elements = oldNode.elements;

            AssetDatabase.RemoveObjectFromAsset(oldNode);
        }

        public static void Save<T>(this IEnumerable<GraphNodePair<T>> graphNodePairs) where T : Node {
            graphNodePairs.Flush().Distinct().ForEach(graphNodePair => Save(graphNodePair.graph));
        }

        public static void Save<TNode, TElement>(
            this IEnumerable<GraphNodeElementTrio<TNode, TElement>> graphNodeElementTrios)
            where TNode : Node where TElement : NodeElement {
            graphNodeElementTrios.Flush().Distinct()
                .ForEach(graphNodeElementTrio => Save(graphNodeElementTrio.graph));
        }

        public static void Run<TNode, TElement>(
            this IEnumerable<GraphNodeElementTrio<TNode, TElement>> graphNodeElementTrios)
            where TNode : Node where TElement : NodeElement {
            graphNodeElementTrios.Flush();
        }

        public static void Save(this IEnumerable<NodeGraph> graphs) {
            graphs.Flush().Distinct().ForEach(graph => Save(graph));
        }

        public static void Save(NodeGraph graph) {
            if (graph == null) {
                Awaken.Utility.Debugging.Log.Important?.Info("Empty graph saving");
                return;
            }

            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // === Functions

        /// <summary>
        /// Start element swap actions builder
        /// Log starting of action
        /// </summary>
        public static Action<NodeElement, NodeElement> StartElementSwap() {
            return (a, b) => Awaken.Utility.Debugging.Log.Important?.Info($"Start action {a.name} => {b.name}");
        }

        /// <summary>
        /// Copy field from one element to other element, with possible conversion
        /// </summary>
        public static Action<NodeElement, NodeElement> CopyField(this Action<NodeElement, NodeElement> targetAction,
            string sourceFieldName, string targetFieldName, Func<object, object> fieldConverter = null) {
            return (oldElement, newElement) => {
                targetAction(oldElement, newElement);
                ReflectionExtension.CopyField(oldElement, sourceFieldName, newElement, targetFieldName,
                    fieldConverter);
            };
        }

        public static object StringToLocString(object string1) {
            return (LocString) (string) string1;
        }

        public static Action<NodeElement, NodeElement> CopyTargetPort(
            this Action<NodeElement, NodeElement> targetAction) {
            return (oldElement, newElement) => {
                targetAction(oldElement, newElement);
                if (oldElement.HasTargetPort()) {
                    var newTargetPort = newElement.TargetPort();
                    var oldTargetPort = oldElement.TargetPort();
                    newTargetPort.CopyPort(oldTargetPort);
                }
            };
        }

        public static Action<NodeElement, NodeElement> CopyConditionPort(
            this Action<NodeElement, NodeElement> targetAction) {
            return (oldElement, newElement) => {
                targetAction(oldElement, newElement);
                if (oldElement.HasConditionPort()) {
                    var newTargetPort = newElement.ConditionPort();
                    var oldTargetPort = oldElement.ConditionPort();
                    newTargetPort.CopyPort(oldTargetPort);
                }
            };
        }

        /// <summary>
        /// Set field to known value
        /// </summary>
        public static Action<NodeElement, NodeElement> SetField(this Action<NodeElement, NodeElement> targetAction,
            string targetFieldName, object value) {
            return (oldElement, element) => {
                targetAction(oldElement, element);
                ReflectionExtension.SetField(element, targetFieldName, value);
            };
        }

        /// <summary>
        /// Seal action builder and specify types
        /// </summary>
        /// <typeparam name="TElement">Old element type</typeparam>
        /// <typeparam name="TNewElement">New element type</typeparam>
        public static Action<TElement, TNewElement> FromTo<TElement, TNewElement>(
            this Action<NodeElement, NodeElement> targetAction)
            where TNewElement : NodeElement where TElement : NodeElement {
            return targetAction;
        }

        /// <summary>
        /// Start element add action builder
        /// Log starting of action
        /// </summary>
        public static Action<NodeElement> StartElementAdd() {
            return element => Awaken.Utility.Debugging.Log.Important?.Info($"Start adding element {element.name}");
        }

        /// <summary>
        /// Seal acton builder and specify type
        /// </summary>
        /// <typeparam name="TElement">New element type</typeparam>
        public static Action<TElement> ElementOfType<TElement>(this Action<NodeElement> targetAction)
            where TElement : NodeElement {
            return targetAction;
        }

        /// <summary>
        /// Start node swap actions builder
        /// Log starting of action
        /// </summary>
        public static Action<Node, Node> StartNodeSwap() {
            return (a, b) => Awaken.Utility.Debugging.Log.Important?.Info($"Start action {a.name} => {b.name}");
        }

        /// <summary>
        /// Copy field from one node to other node, with possible conversion
        /// </summary>
        public static Action<Node, Node> CopyField(this Action<Node, Node> targetAction, string sourceFieldName,
            string targetFieldName, Func<object, object> fieldConverter = null) {
            return (oldNode, newNode) => {
                targetAction(oldNode, newNode);
                ReflectionExtension.CopyField(oldNode, sourceFieldName, newNode, targetFieldName, fieldConverter);
            };
        }

        /// <summary>
        /// Copy ports from old node to new node
        /// Maintain all direction 
        /// </summary>
        public static Action<Node, Node> CopyPorts(this Action<Node, Node> targetAction) {
            return (oldNode, newNode) => {
                targetAction(oldNode, newNode);
                var oldNodeList = new List<Node>() {oldNode};
                var newNodeList = new List<Node>() {newNode};
                var oldConnectedNodes = oldNode.Ports.SelectMany(port => port.GetConnections())
                    .Select(connection => connection.node).Distinct().ToList();

                var oldConnectionPorts = oldConnectedNodes.SelectMany(node => node.Ports).Distinct();

                oldConnectionPorts.ForEach(port => {
                    port.Redirect(oldNodeList, newNodeList);
                    port.VerifyConnections();
                });

                ReflectionExtension.CopyField(oldNode, "ports", newNode, "ports");

                newNode.Ports.ForEach(port => {
                    ReflectionExtension.SetField(port, "_node", newNode);
                    port.VerifyConnections();
                });

            };
        }

        /// <summary>
        /// Copy old node position to new node
        /// </summary>
        /// <param name="targetAction"></param>
        /// <returns></returns>
        public static Action<Node, Node> CopyPosition(this Action<Node, Node> targetAction) {
            return (oldNode, newNode) => {
                targetAction(oldNode, newNode);
                ReflectionExtension.CopyField(oldNode, "position", newNode, "position");
            };
        }

        public static Action<Node, Node> PerformAction(this Action<Node, Node> targetAction, Action<Node, Node> action) {
            return (oldNode, newNode) => {
                targetAction(oldNode, newNode);
                action(oldNode, newNode);
            };
        }

        /// <summary>
        /// Seal action builder and specify types
        /// </summary>
        /// <typeparam name="TNode">Old node type</typeparam>
        /// <typeparam name="TNewNode">New node type</typeparam>
        public static Action<TNode, TNewNode> FromTo<TNode, TNewNode>(this Action<Node, Node> targetAction)
            where TNewNode : Node where TNode : Node {
            return targetAction;
        }
    }

    // === Data structures
    /// <summary>
    /// Struct container for graph - node pair
    /// Be aware this is struct so will be pass by value not reference
    /// This characteristic is used in some places
    /// </summary>
    /// <remarks>
    /// This struct is not valid to be used in hash collections
    /// </remarks>
    public struct GraphNodePair<TNode> where TNode : Node {
        public NodeGraph graph;
        public TNode node;

        public GraphNodePair<T> As<T>() where T : Node {
            return new GraphNodePair<T>() {graph = graph, node = (T) (object) node};
        }

        public override bool Equals(object obj) {
            if (obj is GraphNodePair<TNode> other) {
                return Equals(other);
            }

            return false;
        }

        public bool Equals(GraphNodePair<TNode> other) {
            return graph == other.graph && node == other.node;
        }

        public override int GetHashCode() {
            unchecked {
                return ((graph != null ? graph.GetHashCode() : 0) * 397) ^
                       EqualityComparer<TNode>.Default.GetHashCode(node);
            }
        }

        public static bool operator ==(GraphNodePair<TNode> first, GraphNodePair<TNode> second) {
            return Equals(first, second);
        }

        public static bool operator !=(GraphNodePair<TNode> first, GraphNodePair<TNode> second) {
            return !(first == second);
        }
    }

    /// <summary>
    /// Struct container for graph - node - element trio
    /// Be aware this is struct so will be pass by value not reference
    /// This characteristic is used in some places
    /// </summary>
    /// <remarks>
    /// This struct is not valid to be used in hash collections
    /// </remarks>
    public struct GraphNodeElementTrio<TNode, TElement> where TNode : Node where TElement : NodeElement {
        public NodeGraph graph;
        public TNode node;
        public TElement element;

        public static GraphNodeElementTrio<TNode, TElement> Construct(GraphNodePair<TNode> graphNode,
            TElement element) {
            GraphNodeElementTrio<TNode, TElement> trio = new GraphNodeElementTrio<TNode, TElement>()
                {graph = graphNode.graph, node = graphNode.node};
            trio.element = element;
            return trio;
        }

        public override bool Equals(object obj) {
            if (obj is GraphNodeElementTrio<TNode, TElement> other) {
                return Equals(other);
            }

            return false;
        }

        public bool Equals(GraphNodeElementTrio<TNode, TElement> other) {
            return graph == other.graph && node == other.node && element == other.element;
        }

        public override int GetHashCode() {
            unchecked {
                int hashCode = (graph != null ? graph.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ EqualityComparer<TNode>.Default.GetHashCode(node);
                hashCode = (hashCode * 397) ^ EqualityComparer<TElement>.Default.GetHashCode(element);
                return hashCode;
            }
        }

        public static bool operator ==(GraphNodeElementTrio<TNode, TElement> first,
            GraphNodeElementTrio<TNode, TElement> second) {
            return Equals(first, second);
        }

        public static bool operator !=(GraphNodeElementTrio<TNode, TElement> first,
            GraphNodeElementTrio<TNode, TElement> second) {
            return !(first == second);
        }
    }
}