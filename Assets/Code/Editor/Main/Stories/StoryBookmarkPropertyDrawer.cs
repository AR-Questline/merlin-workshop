using System.Linq;
using Awaken.TG.Editor.Helpers;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Stories.Core;
using Awaken.TG.Main.Templates;
using Awaken.Utility.Collections;
using Awaken.Utility.Debugging;
using Awaken.Utility.Extensions;
using Awaken.Utility.UI;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.Stories {
    [CustomPropertyDrawer(typeof(StoryBookmark))]
    public class StoryBookmarkPropertyDrawer : PropertyDrawer {

        //static readonly Dictionary<StoryGraph, string[]> PossibleBookmarksDictionary = new Dictionary<StoryGraph, string[]>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            var rects = new PropertyDrawerRects(position);

            // label
            if (!label.text.IsNullOrWhitespace()) {
                label.text = label.text.Capitalize();
                var labelRect = rects.AllocateTop((int)EditorGUIUtility.singleLineHeight + 2);
                EditorGUI.LabelField(labelRect, label, EditorStyles.boldLabel);
            }

            // rects
            var graphRect = rects.AllocateTop((int)GraphReferenceHeight(property));
            var chapterRect = rects.AllocateTop((int)EditorGUIUtility.singleLineHeight);

            EditorGUI.indentLevel++;

            // properties
            SerializedProperty storyRef = property.FindPropertyRelative("story");

            // draw story graph reference
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(graphRect, storyRef, new GUIContent(""));
            string[] possibleBookmarks = RefreshPossibleBookmarks(property);
            if (EditorGUI.EndChangeCheck() || possibleBookmarks == null) {
                possibleBookmarks = RefreshPossibleBookmarks(property);
            }

            // draw available bookmarks
            if (HasStoryGraph(property)) {
                if (possibleBookmarks.Length > 1) {
                    int selected;
                    if (string.IsNullOrWhiteSpace(property.FindPropertyRelative("chapterName").stringValue)) {
                        selected = 0;
                    } else {
                        selected = possibleBookmarks.IndexOf(property.FindPropertyRelative("chapterName").stringValue);
                    }
                    EditorGUI.BeginChangeCheck();
                    selected = EditorGUI.Popup(chapterRect, selected, possibleBookmarks);
                    if (EditorGUI.EndChangeCheck()) {
                        SaveCurrentValue(property, selected, possibleBookmarks);
                    }
                } else {
                    property.FindPropertyRelative("chapterName").stringValue = "";
                    EditorGUI.LabelField(chapterRect, "No bookmarks available");
                }
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            float height = 0;
            if (!label.text.IsNullOrWhitespace()) {
                height += EditorGUIUtility.singleLineHeight; //Prefix
            }
            height += GraphReferenceHeight(property); //Template reference height
            height += HasStoryGraph(property) ? EditorGUIUtility.singleLineHeight : 0; // If has story add popup
            height += 2; // Const spacing

            return height;
        }

        // === Helpers
        void SaveCurrentValue(SerializedProperty property, int selected, string[] possibleBookmarks) {
            SerializedProperty chapterRef = property.FindPropertyRelative("chapterName");
            chapterRef.stringValue = selected == 0 ? null : possibleBookmarks[selected];
        }

        bool HasStoryGraph(SerializedProperty property) {
            SerializedProperty storyRef = property.FindPropertyRelative("story");
            return !string.IsNullOrWhiteSpace(storyRef.FindPropertyRelative("_guid").stringValue);
        }

        string[] RefreshPossibleBookmarks(SerializedProperty property) {
            if (!HasStoryGraph(property)) {
                return null;
            }

            SerializedProperty storyRef = property.FindPropertyRelative("story");

            var templateRef = new TemplateReference(storyRef.FindPropertyRelative("_guid").stringValue);
            var storyGraph = templateRef.Get<StoryGraph>();
            if (storyGraph == null) {
                Log.Important?.Warning("Invalid template assigned to StoryBookmark");
                return null;
            }

            return new[] { "Start" }.Concat(storyGraph.BookmarkNames).ToArray();
        }

        float GraphReferenceHeight(SerializedProperty _) => EditorGUIUtility.singleLineHeight * 2f;
    }
}