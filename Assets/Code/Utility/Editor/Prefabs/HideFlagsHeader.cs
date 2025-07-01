using Awaken.TG.Utility;
using Awaken.Utility.Extensions;
using Awaken.Utility.UI;
using UnityEditor;
using UnityEngine;

namespace Awaken.Utility.Editor.Prefabs {
    public static class HideFlagsHeader {
        static GUIStyle s_labelStyle;
        static GUIStyle LabelStyle => s_labelStyle ??= new(GUI.skin.label) {richText = true};
        
        [InitializeOnLoadMethod]
        static void InitHeader() {
            UnityEditor.Editor.finishedDefaultHeaderGUI -= OnPostHeaderGUI;
            UnityEditor.Editor.finishedDefaultHeaderGUI += OnPostHeaderGUI;
        }

        static void OnPostHeaderGUI(UnityEditor.Editor editor) {
            if (EditorPrefs.GetBool("disabledHideFlagsHeader", false)) {
                return;
            }
            if (editor.targets.Length != 1) {
                return;
            }
            if (editor.target is not GameObject gameObject) {
                return;
            }

            HideFlags gameObjectHideFlags = gameObject.hideFlags;
            if (gameObjectHideFlags == HideFlags.None) {
                return;
            }
            var hideFlags = gameObjectHideFlags.ToStringFast();
            EditorGUI.indentLevel += 4;
            EditorGUILayout.LabelField(" Hide Flags: " + hideFlags.ColoredText(GUIColors.Green), LabelStyle);
            EditorGUI.indentLevel -= 4;
        }
    }
}