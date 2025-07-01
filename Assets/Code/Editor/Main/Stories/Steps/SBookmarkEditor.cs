using System.Linq;
using Awaken.TG.Editor.Main.Stories.Drawers;
using Awaken.TG.Editor.Utility.StoryGraphs.Toolset;
using Awaken.TG.Editor.Utility.StoryGraphs.Toolset.CustomWindow;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Steps;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Stories.Steps {
    [CustomElementEditor(typeof(SEditorBookmark))]
    public class SBookmarkEditor : ElementEditor {

        protected override void OnElementGUI() {
            DrawProperties();
            SEditorBookmark bookmark = Target<SEditorBookmark>();

            if (string.IsNullOrWhiteSpace(bookmark.flag)) {
                EditorGUILayout.HelpBox("Assign bookmark name!", MessageType.Warning);
            }

            if (DuplicatedName()) {
                EditorGUILayout.HelpBox("This name is already used!", MessageType.Warning);
            }

            if (GUILayout.Button("Check where this bookmark is used")) {
                FindAllBookmarkUsage();
            }
        }

        bool DuplicatedName() {
            SEditorBookmark bookmark = Target<SEditorBookmark>();

            bool duplicated = target.genericParent.graph.nodes
                .OfType<StoryNode>()
                .SelectMany(n => n.NodeElements)
                .OfType<SEditorBookmark>()
                .Any(b => b != bookmark && b.flag == bookmark.flag);

            return duplicated;
        }

        void FindAllBookmarkUsage() {
            SEditorBookmark bookmark = Target<SEditorBookmark>();

            BookmarkUsageFinder tool = new(StoryBookmark.EDITOR_ToSpecificChapter(bookmark.genericParent.Graph, bookmark.flag));
            // StoryGraphPopupToolEditor.OpenWindowAndExecute(tool, "Bookmark usage");
        }
    }
}