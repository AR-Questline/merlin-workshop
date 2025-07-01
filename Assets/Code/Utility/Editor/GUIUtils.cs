using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Awaken.Utility.Editor {
    public static class GUIUtils {
        static readonly GUIContent TempContent = new();
        static readonly GUIScopeStack<float> FieldWidthStack = new();

        public static GUIContent Content(string label) {
            TempContent.text = label;
            TempContent.tooltip = null;
            return TempContent;
        }
        
        public static GUIContent Content(string label, string tooltip) {
            TempContent.text = label;
            TempContent.tooltip = tooltip;
            return TempContent;
        }

        public static float LabelWidth(GUIStyle style, string label) {
            return style.CalcSize(GUIUtils.Content(label)).x;
        }
        
        public static void PushFieldWidth(float width) {
            FieldWidthStack.Push(EditorGUIUtility.fieldWidth);
            EditorGUIUtility.fieldWidth = width;
        }

        public static void PopFieldWidth() {
            EditorGUIUtility.fieldWidth = FieldWidthStack.Pop();
        }
        
        public static void PushLabelWidth(float width) {
            //GUIHelper.PushLabelWidth(width);
        }
        
        public static void PopLabelWidth() {
            //GUIHelper.PopLabelWidth();
        }
        
        public static void PushContextWidth(float width) {
            //GUIHelper.PushContextWidth(width);
        }

        public static void PopContextWidth() {
            //GUIHelper.PopContextWidth();
        }

        public static void PushIndent0() {
            //GUIHelper.PushIndentLevel(0);
        }

        public static void PopIndent0() {
            //GUIHelper.PopIndentLevel();
        }
    }
}