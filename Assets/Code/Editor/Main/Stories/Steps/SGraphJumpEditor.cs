using Awaken.TG.Editor.Main.Stories.Drawers;
using Awaken.TG.Editor.Utility;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Stories.Steps;
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Awaken.TG.Editor.Main.Stories.Steps {
    [CustomElementEditor(typeof(SEditorGraphJump))]
    public class SGraphJumpEditor : ElementEditor {
        protected override void OnElementGUI(bool isEditMode) {
            if (isEditMode) {
                DrawEditModeGUI();
                return;
            }

            DrawReadonlyGUI();
        }

        void DrawEditModeGUI() {
            base.OnElementGUI(true);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Jump to target")) {
                JumpToBookmark();
            }

            if (GUILayout.Button("Set self")) {
                SetSelf();
            }

            GUILayout.EndHorizontal();
        }

        void DrawReadonlyGUI() {
            if (NodeEditorWindow.FarView) {
                return;
            }
            
            SerializedProperty bookmarkProperty = _serializedObject.FindProperty(nameof(SEditorGraphJump.bookmark));
            SerializedProperty storyRefProperty = bookmarkProperty.FindPropertyRelative(nameof(StoryBookmark.story));
            SerializedProperty guidProperty = storyRefProperty.FindPropertyRelative("_guid");
            SerializedProperty chapterNameProperty = bookmarkProperty.FindPropertyRelative(nameof(StoryBookmark.chapterName));

            string path = AssetDatabase.GUIDToAssetPath(guidProperty.stringValue);
            var lastSlash = path.LastIndexOf('/');
            var fileName = lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;
            var chapterName = fileName.Length > 0
                ? $"<color=#909090>Chapter: {chapterNameProperty.stringValue}</color>"
                : string.Empty;
            var label = $"{fileName}\n{chapterName}";
            EditorGUILayout.LabelField(label, TGEditorGUIStyles.ReadOnlyTextArea);
            
            if (GUILayout.Button("Jump to target")) {
                JumpToBookmark();
            }
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