using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.AI.Barks;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Steps;
using Awaken.Utility.Collections;
using Awaken.Utility.Extensions;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using XNode;
using static Awaken.TG.Editor.Utility.StoryGraphs.Converter.GraphConverterUtils;

namespace Awaken.TG.Editor.Utility.StoryGraphs.Toolset {
    [Serializable]
    [TypeInfoBox("Find all usages of SBookmark name\n" +
                 "1. Provide full bookmark name\n" +
                 "2. Click Execute button\n" +
                 "3. Some bookmarks are used in BarkBookmarks, you can highlight it\n" +
                 "4. If there is only itself in the results, it means that it is used from code or visual scripting")]
    public class BookmarkUsageFinder : StoryGraphUtilityTool<SearchResult<BookmarkResultEntry>, BookmarkResultEntry> {
        [BoxGroup(InputSectionName, centerLabel: true), PropertyOrder(InputSectionOrder)]
        [SerializeField] 
        StoryBookmark bookmark;

        public BookmarkUsageFinder() { }
        public BookmarkUsageFinder(StoryBookmark storyBookmark) {
            bookmark = storyBookmark;
        }

        protected override bool Validate() {
            return bookmark.EDITOR_Graph != null;
        }

        protected override void ExecuteTool() {
            ResultController.SetCurrentlySearched($"{bookmark.EDITOR_Graph.name} - {bookmark.chapterName}");
            
            if (!string.IsNullOrEmpty(bookmark.chapterName) && bookmark.chapterName != "Start") {
                ResultController.Feed(new BookmarkResultEntry(bookmark.EDITOR_Graph, bookmark.EDITOR_Graph.Bookmark(bookmark.chapterName).genericParent, bookmark.chapterName, false, "Current bookmark"));
            }
            
            AllElements<StoryNode, SEditorGraphJump>()
                .Where(trio => !trio.node.Graph.hiddenInToolWindows.HasFlagFast(EditorFinderType.BookmarkUsage) 
                               && trio.element.bookmark.chapterName == bookmark.chapterName 
                               && trio.element.bookmark.EDITOR_Graph == bookmark.EDITOR_Graph)
                .OrderBy(trio => trio.graph.name)
                .ForEach(trio => ResultController.Feed(new BookmarkResultEntry(trio.graph, trio.node, bookmark.chapterName, false)));
            
            List<string> barksNames = typeof(BarkBookmarks).GetFields().Select(field => field.Name).ToList();
            
            if(barksNames.Contains(bookmark.chapterName)) {
                ResultController.Feed(new BookmarkResultEntry(null, null, bookmark.chapterName, true, $"Bookmark is used in {nameof(BarkBookmarks)}!"));
            }
        }
    }
    
    [Serializable]
    public class BookmarkResultEntry : DefaultResultEntry {
        [SerializeField, ReadOnly] string foundName;
        bool _isBark;

        public BookmarkResultEntry(NodeGraph graph, StoryNode node, string name, bool isBark, string notes = "") : base(graph, node, notes){
            foundName = name;
            _isBark = isBark;
        }
            
        [ShowIf("@" + nameof(_isBark))]
        [HorizontalGroup(ACTIONS_SECTION_NAME), Button("Highlight Bookmark script")]
        void HighlightBarkBookmarksAsset() {
            string path = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets($"t:script {nameof(BarkBookmarks)}").FirstOrDefault());
            EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(path));
        }
    }
}
