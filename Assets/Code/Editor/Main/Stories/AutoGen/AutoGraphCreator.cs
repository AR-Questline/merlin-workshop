using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Assets;
using Awaken.TG.Editor.Localizations;
using Awaken.TG.Editor.Main.Stories.Steps;
using Awaken.TG.Editor.Main.Utility;
using Awaken.TG.Editor.Utility.StoryGraphs.Converter;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Steps;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using XNode;
using XNodeEditor;

namespace Awaken.TG.Editor.Main.Stories.AutoGen {
    /// <summary>
    /// User Manual for AutoGraphCreator can be found at https://www.notion.so/awaken/Story-Graphs-Automatyczne-generowanie-graf-w-f242f24d22ce4caa8de71e04978a694e
    /// </summary>
    public class AutoGraphCreator {
        const string ChoiceNodeName = "Choice Node";
        const string TextNodeName = "Text Node";
        const float HorizontalSpreadDistance = 450;
        const string DefaultPath = "/Data/Templates/Stories";
        
        StoryGraph _storyGraph;
        Node _startNode;
        readonly IEnumerable<NodeDataElement> _nodes;
        readonly HashSet<ActorRef> _allowedActors;
        readonly ActorRef _mainSpeaker;

        public AutoGraphCreator(ActorRef mainActor, IEnumerable<NodeDataElement> nodes) {
            _mainSpeaker = mainActor;
            _allowedActors = new HashSet<ActorRef>();
            _nodes = nodes;
        }

        public AutoGraphCreator(StoryGraph storyGraph, Node startNode, IEnumerable<NodeDataElement> nodes, ActorRef mainSpeaker, IEnumerable<ActorRef> allowedActors) {
            _storyGraph = storyGraph;
            _startNode = startNode;
            _nodes = nodes;
            _mainSpeaker = mainSpeaker;
            _allowedActors = new HashSet<ActorRef>(allowedActors);
        }

        public void CreateNewStoryGraph(string path = DefaultPath) {
            var truePath = Application.dataPath + path;
            _storyGraph = (StoryGraph)TemplateCreation.CreateScriptableObject(StoryGraph.CreateGraph, truePath);
            _startNode = (StoryStartEditorNode)NodeGraphEditor.CreateNode(typeof(StoryStartEditorNode), _storyGraph, new Vector2(0, 0));
            Run();
            Save();
        }

        public void AddToExisting() {
            _storyGraph = (StoryGraph)_startNode.graph;
            Run();
            Save();
        }

        void Run() {
            _storyGraph.allowedActors = _allowedActors.ToArray();
            GenerateNodes();
        }

        void GenerateNodes() {
            var chaptersByNodes = new Dictionary<NodeDataElement, ChapterEditorNode>();

            foreach (var node in _nodes) {
                ChapterEditorNode chapter = (ChapterEditorNode)NodeGraphEditor.CreateNode(typeof(ChapterEditorNode), _storyGraph);
                chaptersByNodes.Add(node, chapter);
            }

            foreach (var node in _nodes) {
                switch (node) {
                    case TextNodeDataElement textNode:
                        SetUpTextNode(textNode, chaptersByNodes);
                        break;
                    case ChoiceNodeDataElement choiceNode:
                        SetUpChoiceNode(choiceNode, chaptersByNodes);
                        break;
                    case BookmarkNodeDataElement bookmarkNode:
                        SetUpBookmarkNode(bookmarkNode, chaptersByNodes);
                        break;
                }
            }

            CorrectPositionsForGraphNodes(chaptersByNodes);
        }
        
        void SetUpTextNode(TextNodeDataElement textNode, Dictionary<NodeDataElement, ChapterEditorNode> chaptersByNodes) {
            var chapter = chaptersByNodes[textNode];
            chapter.name = TextNodeName;

            if (textNode.IsRandomized) {
                StoryNodeEditor.CreateElement(chapter, typeof(SEditorRandomTextShow));
            }

            foreach (var message in textNode.Message) {
                var speaker = GetBestSuitedActorRef(message.Speaker, _mainSpeaker);
                var listener = GetBestSuitedActorRef(message.Listener, DefinedActor.Hero.ActorRef);
                STextEditor.CreateSText(chapter, speaker, listener, message.Text);
            }

            var nextChapter = chaptersByNodes.FirstOrDefault((pair => pair.Key.ID == textNode.QuitNodeID))
                .Value;

            if (nextChapter && !textNode.IsSeparate) {
                chapter.continuation = nextChapter;

                var chapterContinuationPort = chapter.GetPort(NodePort.FieldNameCompressed.Continuation);
                var nextChapterLinkPort = nextChapter.GetPort(NodePort.FieldNameCompressed.Link);
                chapterContinuationPort.Connect(nextChapterLinkPort);
            }

            if (textNode.ID == 0) {
                NodePort startChapterPort =
                    _startNode.GetPort(_startNode is StoryStartEditorNode ? NodePort.FieldNameCompressed.Chapter : NodePort.FieldNameCompressed.Continuation);

                var chapterLinkPort = chapter.GetPort(NodePort.FieldNameCompressed.Link);
                startChapterPort.Connect(chapterLinkPort);
            }
        }

        void SetUpChoiceNode(ChoiceNodeDataElement choiceNode, Dictionary<NodeDataElement, ChapterEditorNode> chaptersByNodes) {
            var chapter = chaptersByNodes[choiceNode];
            chapter.name = ChoiceNodeName;
            foreach (var choice in choiceNode.Choices) {
                SEditorChoice sChoice = (SEditorChoice)StoryNodeEditor.CreateElement(chapter, typeof(SEditorChoice));

                SerializedObject serializedObject = new(sChoice);
                var singleChoiceProperty = serializedObject.FindProperty(nameof(sChoice.choice));
                string localizationPrefix = _storyGraph.LocalizationPrefix;
                LocalizationUtils.ValidateTerm(singleChoiceProperty.FindPropertyRelative(nameof(sChoice.choice.text)), localizationPrefix, out string newLocId);
                sChoice.choice.text.ID = newLocId;
                _storyGraph.StringTable.AddEntry(sChoice.choice.text.ID, choice.Message);

                var target = chaptersByNodes.FirstOrDefault(pair => pair.Key.ID == choice.QuitNodeID).Value;
                if (target != null) {
                    sChoice.TargetPort().Connect(target.GetPort(NodePort.FieldNameCompressed.Link));
                }
            }
        }

        void SetUpBookmarkNode(BookmarkNodeDataElement bookmarkNode, IReadOnlyDictionary<NodeDataElement, ChapterEditorNode> chaptersByNodes) {
            var chapter = chaptersByNodes[bookmarkNode];
            chapter.name = bookmarkNode.BookmarkName;

            SEditorBookmark sBookmark = (SEditorBookmark)StoryNodeEditor.CreateElement(chapter, typeof(SEditorBookmark));
            sBookmark.flag = bookmarkNode.BookmarkName;

            var nextChapter = chaptersByNodes.FirstOrDefault(pair => pair.Key.ID == bookmarkNode.QuitNodeID).Value;
            if (nextChapter != null) {
                chapter.continuation = nextChapter;
                var chapterContinuationPort = chapter.GetPort(NodePort.FieldNameCompressed.Continuation);
                var nextChapterLinkPort = nextChapter.GetPort(NodePort.FieldNameCompressed.Link);
                chapterContinuationPort.Connect(nextChapterLinkPort);
            }
        }

        void CorrectPositionsForGraphNodes(IReadOnlyDictionary<NodeDataElement, ChapterEditorNode> chaptersByNodes) {
            foreach (var nodeSet in _nodes.GroupBy(p => p.Depth)) {
                var nodesInNodeSet = nodeSet.ToArray();
                for (int i = 0; i < nodesInNodeSet.Length; i++) {
                    var node = nodesInNodeSet[i];
                    ChapterEditorNode chapter = chaptersByNodes[node];
                    int currentDepthNodesCount = _nodes.Count(p => p.Depth == node.Depth);

                    float x = HorizontalSpreadDistance * (node.Depth + 1);
                    float y = -(currentDepthNodesCount / 2) * node.ApproximateGuiHeight + i * node.ApproximateGuiHeight;
                    chapter.position = _startNode.position + new Vector2(x, y);
                }
            }

            foreach (var nodeDataElement in chaptersByNodes.Keys) {
                if (nodeDataElement is BookmarkNodeDataElement) {
                    if (chaptersByNodes[nodeDataElement].continuation != null) {
                        chaptersByNodes[nodeDataElement].position.y = chaptersByNodes[nodeDataElement].continuation.position.y;
                    }

                    int bookmarksOnTheSameLevel = chaptersByNodes.Count(p =>
                        p.Key is BookmarkNodeDataElement && p.Value.position.y.Equals(chaptersByNodes[nodeDataElement].position.y));
                    if (bookmarksOnTheSameLevel > 1) {
                        chaptersByNodes[nodeDataElement].position.x -= (HorizontalSpreadDistance / 2) * bookmarksOnTheSameLevel;
                    }
                }
            }
        }

        void Save() {
            var tableCollection = LocalizationEditorSettings.GetStringTableCollection(_storyGraph.StringTable.TableCollectionName);
            EditorUtility.SetDirty(_storyGraph.StringTable);
            EditorUtility.SetDirty(tableCollection);
            EditorUtility.SetDirty(tableCollection.SharedData);
            GraphConverterUtils.UpdateVoiceOvers(true, _storyGraph);
            AssetDatabase.SaveAssets();
        }

        // === Utils
        ActorRef GetBestSuitedActorRef(string actorName, ActorRef fallback) {
            actorName = actorName.Trim();

            if (!string.IsNullOrWhiteSpace(actorName) && ActorFinder.TryGetActorWithFix(actorName, out ActorRef actorRef)) {
                TryAddActorToAllowedActors(actorRef);
                return actorRef;
            }

            TryAddActorToAllowedActors(fallback);
            return fallback;

            void TryAddActorToAllowedActors(ActorRef actorRef) {
                if (actorRef.Equals(DefinedActor.None.ActorRef)) {
                    return;
                }

                _allowedActors.Add(actorRef);
                _storyGraph.allowedActors = _allowedActors.ToArray();
            }
        }
    }
}