using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Editor.Main.Stories;
using Awaken.TG.Editor.Main.Stories.AutoGen;
using Awaken.TG.Editor.Main.Stories.Steps;
using Awaken.TG.Main.AI.Barks;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Debugging;
using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;

namespace Awaken.TG.Editor.Main.AI.Barks {
    /// <summary>
    /// This script synchronizes Bark StoryGraphs with the current state of the <c>ActorsRegister</c> and <c>BarksConfig</c>.
    /// <list type="bullet">
    ///  <item>Generates new StoryGraph assets with bark phrases for actors that don't have them yet, and assigns them to actors in <c>ActorsRegister</c>.</item>
    ///  <item>Updates existing StoryGraphs with the latest bark phrases for actors that already have them.</item>
    ///  <item>Deletes StoryGraphs for actors that no longer exist or have no tags.</item>
    /// </list>
    /// </summary>
    public class BarkStoryGraphsSyncer {
        const string BarkGraphsRootPath = "Assets/Data/Templates/Stories/Barks/ReleaseCandidatePatch/";
        readonly List<ActorSpec> _actors;
        readonly Dictionary<ActorSpec, StoryGraph> _barkGraphsByActors;
        readonly List<string> _storyGraphPaths;
        readonly ActorsRegister _actorsRegister;
        readonly HashSet<string> _tagsInUse;
        readonly BarksConfig _barksConfig;

        public BarkStoryGraphsSyncer(BarksConfig config) {
            _actorsRegister = ActorsRegister.Get;
            _actors = _actorsRegister.AllActors.ToList();
            _barkGraphsByActors = new Dictionary<ActorSpec, StoryGraph>();
            _barksConfig = config;
            _tagsInUse = config.GetTagsInUse();
            foreach (var actor in _actors) {
                string actorPath = $"{BarkGraphsRootPath}/{actor.GetPath()}.asset";
                var graph = AssetDatabase.LoadAssetAtPath<StoryGraph>(actorPath);
                if (graph != null) {
                    _barkGraphsByActors[actor] = graph;
                }
            }

            string[] storyGraphGuids = AssetDatabase.FindAssets("t:StoryGraph", new[] { BarkGraphsRootPath.Remove(BarkGraphsRootPath.Length - 1) });
            _storyGraphPaths = storyGraphGuids.Select(AssetDatabase.GUIDToAssetPath).ToList();
        }

        public void Sync() {
            DeleteNoLongerNeededGraphs();
            CreateGraphsForNewActors();
            UpdateGraphs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        void DeleteNoLongerNeededGraphs() {
            // remove graphs for actors that no longer exist
            foreach (string path in _storyGraphPaths) {
                StoryGraph storyGraph = AssetDatabase.LoadAssetAtPath<StoryGraph>(path);
                if (storyGraph != null && !_barkGraphsByActors.ContainsValue(storyGraph)) {
                    AssetDatabase.DeleteAsset(path);
                }
            }

            // remove graphs for actors that no longer have tags
            foreach (var actor in _actors) {
                // skip actors with custom graphs
                if (actor.useCustomBarkGraph) {
                    continue;
                }

                bool actorHasUnusedTags = !actor.tags.Any(tag => _tagsInUse.Contains(tag));
                if (actorHasUnusedTags && _barkGraphsByActors.ContainsKey(actor)) {
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(_barkGraphsByActors[actor]));
                    _barkGraphsByActors.Remove(actor);
                }
            }
        }

        void CreateGraphsForNewActors() {
            bool hasNewActors = false;
            foreach (var actor in _actors) {
                // skip actors with custom graphs
                if (actor.useCustomBarkGraph) {
                    continue;
                }

                if (actor.barkConfig != null && actor.barkConfig.EDITOR_GetStory() != null
                    || actor.tags.Length == 0
                    || !actor.tags.Any(tag => _tagsInUse.Contains(tag))) {
                    continue;
                }

                var actorPath = actor.GetPath();
                var newStoryGraph = CreateGraphForActor(actorPath);
                if (newStoryGraph != null) {
                    var startNode = (StoryStartEditorNode)NodeGraphEditor.CreateNode(typeof(StoryStartEditorNode), newStoryGraph, new Vector2(0, 0));
                    startNode.involveHero = false;
                    startNode.involveAI = false;
                    startNode.enableChoices = false;
                    TemplatesUtil.EDITOR_AssignGuid(newStoryGraph, newStoryGraph);

                    actor.barkConfig ??= new BarkConfig();
                    actor.barkConfig.storyRef = new TemplateReference(newStoryGraph);
                    _barkGraphsByActors[actor] = newStoryGraph;
                    hasNewActors = true;
                }
            }

            if (hasNewActors) {
                EditorUtility.SetDirty(_actorsRegister);
            }
        }

        void UpdateGraphs() {
            foreach (var pair in _barkGraphsByActors) {
                ActorSpec actorSpec = pair.Key;

                // skip actors with custom graphs
                if (actorSpec.useCustomBarkGraph) {
                    continue;
                }

                StoryGraph graph = pair.Value;
                if (graph == null) {
                    continue;
                }

                ActorRef actorRef = new() {
                    guid = actorSpec.Guid,
                };
                HashSet<ActorRef> allowedActors = new() { actorRef, DefinedActor.Hero.ActorRef };

                var phrasesByBookmarks = _barksConfig.GetBarkPhrasesByBookmarks(pair.Key);
                phrasesByBookmarks = phrasesByBookmarks.Where(kv => actorSpec.AvailableBookmarks.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
                
                if (graph.nodes.Count <= 1) {
                    CreateInitialBookmarks(phrasesByBookmarks, 1, actorSpec, graph, actorRef, allowedActors);
                } else {
                    UpdateBookmarks(graph, phrasesByBookmarks, actorRef);
                }
            }

            return;

            void CreateInitialBookmarks(Dictionary<string, List<string>> phrasesByBookmarks, int initialNodeID, ActorSpec actorSpec, StoryGraph graph,
                ActorRef actorRef, HashSet<ActorRef> allowedActors) {
                List<NodeDataElement> nodes = new();
                foreach (var bookmark in phrasesByBookmarks) {
                    var bookmarkNodeDataElement = new BookmarkNodeDataElement(initialNodeID, (initialNodeID - 1) % 2, bookmark.Key, ++initialNodeID);
                    var textNodeDataElement = new TextNodeDataElement(initialNodeID++, (initialNodeID) % 2, -1, false, true);
                    foreach (var phrase in bookmark.Value) {
                        textNodeDataElement.Message.Add(new Message(phrase, actorSpec.name, DefinedActor.Hero.ActorName));
                    }

                    nodes.Add(bookmarkNodeDataElement);
                    nodes.Add(textNodeDataElement);
                }

                AutoGraphCreator autoGraphCreator = new(graph, graph.StoryStartNode, nodes, actorRef, allowedActors);
                autoGraphCreator.AddToExisting();
            }

            void UpdateBookmarks(StoryGraph graph, Dictionary<string, List<string>> phrasesByBookmarks, ActorRef actorRef) {
                var graphBookmarks = graph.Bookmarks.ToList();
                for (int i = graphBookmarks.Count - 1; i >= 0; i--) {
                    SEditorBookmark bookmark = graphBookmarks[i];
                    if (!phrasesByBookmarks.ContainsKey(bookmark.flag)
                        || phrasesByBookmarks[bookmark.flag].Count == 0
                        || bookmark.ContinuationChapter == null) {
                        DeleteBookmark(bookmark);
                    } else {
                        UpdateTextNode(phrasesByBookmarks, bookmark, actorRef);
                    }
                }

                foreach (var phrase in phrasesByBookmarks) {
                    if (graphBookmarks.Any(b => b.flag == phrase.Key)) {
                        continue;
                    }

                    AddBookmarkAsNew(graph, actorRef, phrase.Key, phrase.Value);
                }
            }

            void DeleteBookmark(SEditorBookmark bookmark) {
                var textNode = bookmark.ContinuationChapter as ChapterEditorNode;

                if (textNode != null) {
                    StoryGraphEditorUtils.RemoveNodeFromGraph(textNode.Graph, textNode);
                }

                StoryGraphEditorUtils.RemoveNodeFromGraph(bookmark.Parent.Graph, bookmark.genericParent);
            }

            void AddBookmarkAsNew(NodeGraph graph, ActorRef actor, string bookmarkName, List<string> phrases) {
                var bookmarkChapter = (ChapterEditorNode)NodeGraphEditor.CreateNode(typeof(ChapterEditorNode), graph);

                SEditorBookmark sBookmark = (SEditorBookmark)StoryNodeEditor.CreateElement(bookmarkChapter, typeof(SEditorBookmark));
                sBookmark.name = bookmarkName;
                sBookmark.flag = bookmarkName;

                ChapterEditorNode textChapter = (ChapterEditorNode)NodeGraphEditor.CreateNode(typeof(ChapterEditorNode), graph);
                bookmarkChapter.continuation = textChapter;

                StoryNodeEditor.CreateElement(textChapter, typeof(SEditorRandomTextShow));
                foreach (var phrase in phrases) {
                    STextEditor.CreateSText(textChapter, actor, DefinedActor.Hero.ActorRef, phrase);
                }

                var bookmarkChapterContinuationPort = bookmarkChapter.GetPort(NodePort.FieldNameCompressed.Continuation);
                var textChapterLinkPort = textChapter.GetPort(NodePort.FieldNameCompressed.Link);
                bookmarkChapterContinuationPort.Connect(textChapterLinkPort);
            }

            void UpdateTextNode(Dictionary<string, List<string>> phrasesByBookmarks, SEditorBookmark bookmark, ActorRef actorRef) {
                var phrasesRequired = phrasesByBookmarks[bookmark.flag];
                var chapter = bookmark.ContinuationChapter;
                if (chapter == null) {
                    Log.Important?.Error(
                        $"There is a bookmark without a continuation chapter! Bookmark: {bookmark.flag} in graph {bookmark.genericParent.Graph.name}");
                }

                Queue<SEditorText> textStepsToRemove = new();

                // prepare all text that are no longer used for removal
                var textSteps = bookmark.ContinuationChapter.Steps.OfType<SEditorText>().ToList();
                for (int index = textSteps.Count - 1; index >= 0; index--) {
                    SEditorText step = textSteps[index];
                    if (!phrasesRequired.Contains(step.text)) {
                        textStepsToRemove.Enqueue(step);
                    }
                }

                // create new text steps for phrases that are not yet in the graph, but try to reuse existing steps if possible
                foreach (var phrase in phrasesRequired) {
                    if (textSteps.Any(s => s.text == phrase)) {
                        continue;
                    }

                    if (textStepsToRemove.Count > 0) {
                        var step = textStepsToRemove.Dequeue();
                        STextEditor.UpdateSText(step, actorRef, DefinedActor.Hero.ActorRef, phrase);
                    } else {
                        STextEditor.CreateSText((ChapterEditorNode)chapter, actorRef, DefinedActor.Hero.ActorRef, phrase);
                    }
                }

                // finally remove all text steps that are no longer used and could not be recycled
                while (textStepsToRemove.Count > 0) {
                    var sText = textStepsToRemove.Dequeue();
                    StoryNodeEditor.RemoveElement(sText.genericParent, sText);
                }
            }
        }

        // === Utils
        static StoryGraph CreateGraphForActor(string actorPath) {
            string directoryPath = BarkGraphsRootPath;
            string[] pathParts = actorPath.Split('/');
            for (int i = 0; i < pathParts.Length - 1; i++) {
                directoryPath = $"{directoryPath}/{pathParts[i]}";
                if (!AssetDatabase.IsValidFolder(directoryPath)) {
                    AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(directoryPath), pathParts[i]);
                }
            }

            string fileName = pathParts.Last();
            string assetPath = $"{directoryPath}/{fileName}.asset";
            StoryGraph storyGraph = StoryGraph.CreateGraph(actorPath);
            AssetDatabase.CreateAsset(storyGraph, assetPath);
            return storyGraph;
        }
    }
}