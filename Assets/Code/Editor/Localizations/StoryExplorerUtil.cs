using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Steps;
using UnityEngine;

namespace Awaken.TG.Editor.Localizations {
    public static class StoryExplorerUtil {
        static readonly HashSet<StoryNode> NodesHistory = new();

        public static IEnumerable<NodeElement> ExtractElements(StoryBookmark bookmark) {
            if (!bookmark.IsValid) {
                yield break;
            }

            NodesHistory.Clear();
            StoryNode storyNode = null;
            try {
                storyNode = bookmark.EDITOR_Chapter as StoryNode;
            } catch (Exception e) {
                Debug.LogException(e);
            }

            if (storyNode != null) {
                foreach (var node in ExploreNode(storyNode, NodesHistory)) {
                    yield return node;
                }
            }

            NodesHistory.Clear();
        }

        public static IEnumerable<NodeElement> ExploreNode(StoryNode node, HashSet<StoryNode> history) {
            if (node == null || !history.Add(node)) {
                yield break;
            }

            foreach (var element in node.elements) {
                if (element == null) {
                    continue;
                }

                yield return element;

                ElementExtractor extractor = ElementExtractor.GetFor(element.GetType());

                foreach (var continuation in extractor.GetContinuations(element, ScriptType.Texts)) {
                    if (continuation != null) {
                        foreach (var ele in ExploreNode(continuation, history)) {
                            yield return ele;
                        }
                    }
                }
            }

            if (node is IEditorChapter { ContinuationChapter: StoryNode nextNode }) {
                foreach (var ele in ExploreNode(nextNode, history)) {
                    yield return ele;
                }
            }
        }
        
        public static string GetPreviousLine(NodeElement element) {
            var node = element.genericParent as ChapterEditorNode;
            if (node == null) {
                return null;
            }
            bool isChoice = element is SEditorChoice;
            if (FindPreviousLineBeforeElementIndex(node, element, isChoice, out string previousLine)) {
                return previousLine;
            }

            if (FindPreviousLineInNodesBefore(node, isChoice, null, out previousLine)) {
                return previousLine;
            }

            return null;
        }

        static bool FindPreviousLineInNodesBefore(ChapterEditorNode node, bool isChoice, HashSet<ChapterEditorNode> visitedNodes, out string previousLine) {
            previousLine = null;
            visitedNodes ??= new HashSet<ChapterEditorNode>();
            if (!visitedNodes.Add(node)) {
                return false;
            }
            
            var prevNodes = node.Inputs.SelectMany(i => i.connections.Select(c => c.node)).OfType<ChapterEditorNode>().ToList();
            
            foreach (var prevNode in prevNodes) {
                bool allowAll = prevNode.ContinuationChapter as ChapterEditorNode == node;
                List<NodeElement> allowedElements = new();
                if (allowAll) {
                    allowedElements.AddRange(prevNode.elements);
                } else {
                    for (int i = 0; i < prevNode.elements.Count; i++) {
                        var element = prevNode.elements[i];
                        if (element == null) {
                            Debug.LogError($"Element {i} on node {prevNode.name} is null", prevNode);
                            continue;
                        }
                        if (element.TargetNode() == node) {
                            allowedElements.Add(element);
                        }
                    }
                }
                
                if (FindPreviousLineInNode(allowedElements, isChoice, out previousLine)) {
                    return true;
                }
            }

            foreach (var prevNode in prevNodes) {
                if (FindPreviousLineInNodesBefore(prevNode, isChoice, visitedNodes, out previousLine)) {
                    return true;
                }
            }

            return false;
        }

        static bool FindPreviousLineBeforeElementIndex(StoryNode node, NodeElement element, bool isChoice, out string previousLine) {
            previousLine = null;

            int index = node.elements.IndexOf(element);
            
            for (int i = index - 1; i >= 0; i--) {
                var prevElement = node.elements[i];
                if (prevElement == null) {
                    continue;
                }

                if (isChoice && prevElement is SEditorChoice) {
                    return false;
                }

                var extractor = ElementExtractor.GetFor(prevElement.GetType());
                var previousText = extractor.GetTexts(prevElement).FirstOrDefault();
                if (previousText != null) {
                    previousLine = previousText.AsPreviousLine;
                    return true;
                }
            }

            return false;
        }
        
        static bool FindPreviousLineInNode(List<NodeElement> allowedElements, bool isChoice, out string previousLine) {
            previousLine = null;

            for (int i = allowedElements.Count - 1; i >= 0; i--) {
                var prevElement = allowedElements[i];
                if (prevElement == null) {
                    continue;
                }

                if (isChoice && prevElement is SEditorChoice) {
                    return false;
                }

                var extractor = ElementExtractor.GetFor(prevElement.GetType());
                var previousText = extractor.GetTexts(prevElement).FirstOrDefault();
                if (previousText != null) {
                    previousLine = previousText.AsPreviousLine;
                    return true;
                }
            }

            return false;
        }
    }
}