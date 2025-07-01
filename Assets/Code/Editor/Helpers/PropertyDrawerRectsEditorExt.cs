using Awaken.Utility.UI;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Helpers {
    public static class PropertyDrawerRectsEditorExt {
        public static Rect AllocateLine(this ref PropertyDrawerRects rect) {
            return rect.AllocateTop(EditorGUIUtility.singleLineHeight);
        }

        public static Rect AllocateLines(this ref PropertyDrawerRects rect, int count) {
            return rect.AllocateTop(EditorGUIUtility.singleLineHeight * count);
        }
    }
}