using Awaken.TG.Editor.Main.Stories.Drawers;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Steps;
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Awaken.TG.Editor.Main.Stories.Steps {
    [CustomElementEditor(typeof(SEditorGraphJump))]
    public class SGraphJumpEditor : ElementEditor {
        protected override void OnElementGUI() {
            base.OnElementGUI();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Jump to target")) {
                JumpToBookmark();
            }

            if (GUILayout.Button("Set self")) {
                SetSelf();
            }

            GUILayout.EndHorizontal();
        }

        void JumpToBookmark() {
            StoryBookmark bookmark = Target<SEditorGraphJump>().bookmark;
            StoryNode targetNode = string.IsNullOrEmpty(bookmark.chapterName) || bookmark.chapterName == "Start"
                ? bookmark.EDITOR_Graph.StoryStartNode
                : bookmark.EDITOR_Graph.Bookmark(bookmark.chapterName).genericParent;

            NodeEditorWindow.Open(bookmark.EDITOR_Graph).CenterOnNode(targetNode);
        }

        void SetSelf() {
            var sGraphJump = (SEditorGraphJump)target;
            Undo.RecordObject(sGraphJump, "Changed GraphJump's bookmark field to it's own graph");
            sGraphJump.bookmark = StoryBookmark.EDITOR_ToInitialChapter(ParentNode<StoryNode>().Graph);
        }
    }
}