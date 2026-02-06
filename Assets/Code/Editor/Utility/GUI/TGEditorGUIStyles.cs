using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility {
    public static class TGEditorGUIStyles {
        static GUIStyle s_readOnlyTextArea;

        public static GUIStyle ReadOnlyTextArea => s_readOnlyTextArea ??= CreateReadOnlyTextArea();

        static GUIStyle CreateReadOnlyTextArea() {
            var style = new GUIStyle(EditorStyles.textArea) {
                wordWrap = true,
                richText = true,
                fontStyle = FontStyle.Bold
            };
            return style;
        }
    }
}