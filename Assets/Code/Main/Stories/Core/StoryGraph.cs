using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Stories.Actors;
using Awaken.TG.Main.Stories.Execution;
using Awaken.TG.Main.Stories.Steps;
using Awaken.TG.Main.Stories.Tags;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using Awaken.TG.Utility.Attributes.Tags;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Sirenix.OdinInspector;
using UnityEngine;
using UniversalProfiling;
using XNode;
using Debug = UnityEngine.Debug;

namespace Awaken.TG.Main.Stories.Core {
    public class StoryGraph : NodeGraph, ITemplate, ITagged {
        [UnityEngine.Scripting.Preserve] static readonly UniversalProfilerMarker LoadUsedSoundBanksMarker = new("StoryGraph.LoadUsedSoundBanks");
        [UnityEngine.Scripting.Preserve] static readonly UniversalProfilerMarker UnloadUsedSoundBanksMarker = new("StoryGraph.UnloadUsedSoundBanks");

        TemplateType ITemplate.TemplateType => templateType;
        [SerializeField, HideInInspector] TemplateType templateType;

#if UNITY_EDITOR
        [SerializeField, HideInInspector] public EditorFinderType hiddenInToolWindows;
#endif
        
        protected override string DefaultLocPrefix => "Story";

        [SerializeField, HideInInspector] TemplateMetadata metadata;

        public TemplateMetadata Metadata => metadata;

        public string[] usedSoundBanksNames = Array.Empty<string>();
        // === Properties
        [Tags(TagsCategory.Story)]
        public string[] tags = Array.Empty<string>();

        public ICollection<string> Tags => tags;

        public bool sharedBetweenMultipleNPCs;
        public List<VariableDefine> variables = new List<VariableDefine>();
        public List<VariableReferenceDefine> variableReferences = new List<VariableReferenceDefine>();

        public ActorRef[] allowedActors = Array.Empty<ActorRef>();
        public bool autofillActors;

        public bool AutofillActors => autofillActors && allowedActors.Length == 1 && !allowedActors[0].IsNone();

        public string GUID { get; set; }
        public PooledList<ITemplate> DirectAbstracts => PooledList<ITemplate>.Empty;
        public bool IsAbstract => false;
        
        string INamed.DisplayName => string.Empty;
        string INamed.DebugName => name;

        public StoryStartEditorNode StoryStartNode {
            get {
                try {
                    return (StoryStartEditorNode)nodes.First(n => n is StoryStartEditorNode);
                } catch {
                    Debug.LogError($"No Story Start node in Story Graph: {name} ({GUID})");
                    throw;
                }
            }
        }

        public IEditorChapter InitialChapter => StoryStartNode.Chapter;

        public ChapterEditorNode BookmarkedChapter(string name) => Bookmark(name)?.Parent;
        public SEditorBookmark Bookmark(string name) {
            foreach (var node in nodes) {
                if (node is ChapterEditorNode chapter) {
                    foreach (var element in chapter.Elements) {
                        if (element is SEditorBookmark bookmark && bookmark.flag == name) {
                            return bookmark;
                        }
                    }
                }
            }
            return null;
        }

        public IStorySettings Settings(StoryBookmark bookmark) {
            if (string.IsNullOrWhiteSpace(bookmark.chapterName)) {
                var sBookmark = Bookmark(bookmark.chapterName);
                if (sBookmark != null && sBookmark.storySettings) {
                    return sBookmark;
                }
            }
            return StoryStartNode;
        } 
        
        public IEnumerable<string> BookmarkNames => nodes.OfType<StoryNode>().SelectMany(n => n.NodeElements).OfType<SEditorBookmark>().Select(b => b.flag);
        public IEnumerable<SEditorBookmark> Bookmarks => nodes.OfType<StoryNode>().SelectMany(n => n.NodeElements).OfType<SEditorBookmark>();
        
        public EditorStep LastExecutedStep { get; set; }

        public void ResetGraphCache() {
            try {
                foreach (StoryNode node in nodes.OfType<StoryNode>()) {
                    foreach (NodeElement element in node.NodeElements) {
                        element.ResetCache();
                    }
                }
            } catch (Exception e) {
                Log.Important?.Error($"For graph {name} there is null in nodes or elements");
                Debug.LogException(e);
            }
        }
        
        // === Graph creator

        public static StoryGraph CreateGraph(string name) {
            StoryGraph graph = CreateInstance<StoryGraph>();
            graph.name = name;
            graph.locPrefix = name;
            return graph;
        }

#if UNITY_EDITOR
        DebugNodeData[] _editorDebugCache;

        [FoldoutGroup("Debug"), ShowInInspector, OnValueChanged(nameof(EDITOR_SearchChanged))]
        string _editorSearch;
        [FoldoutGroup("Debug"), ShowInInspector]
        DebugNodeData[] DebugElements {
            get {
                if (_editorDebugCache == null) {
                    EDITOR_RefreshDebug();
                }

                return _editorDebugCache;
            }
        }

        [FoldoutGroup("Debug"), ShowInInspector, Button]
        void EDITOR_RefreshDebug() =>
            _editorDebugCache = nodes.OfType<StoryNode>().Select(n => new DebugNodeData(n)).ToArray();

        void EDITOR_SearchChanged() {
            if (string.IsNullOrWhiteSpace(_editorSearch)) {
                EDITOR_RefreshDebug();
            }

            _editorDebugCache = _editorDebugCache
                .Where(n => n.IsValid(_editorSearch)).ToArray();
        }
        
        class DebugNodeData {
            [ShowInInspector]
            internal StoryNode node;

            [ListDrawerSettings(ShowFoldout = false, IsReadOnly = true), ShowInInspector]
            internal List<NodeElement> elements;

            public DebugNodeData(StoryNode node) {
                this.node = node;
                elements = node.elements;
            }

            internal bool IsValid(string search) {
                return NodeName(search, node) || NodeType(search, node) || elements.Any(e => ElementName(search, e) || ElementType(search, e));
            }

            bool NodeName(string search, StoryNode node) => node.name.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) > -1;
            bool NodeType(string search, StoryNode node) => node.Type.Name.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) > -1;
            bool ElementName(string search, NodeElement ele) => ele.name.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) > -1;
            bool ElementType(string search, NodeElement ele) => node.Type.Name.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) > -1;
        }
#endif
    }
}