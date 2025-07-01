using Awaken.TG.Editor.DataViews.Data;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.DataViews.Utils {
    public static class DataViewStyle {
        public static GUIStyle Number { get; private set; }
        public static GUIStyle Enum { get; private set; }
        public static GUIStyle Text { get; private set; }
        
        public static void Refresh() {
            Number = null;
            Enum = null;
            Text = null;
        }

        public static void Validate() {
            var preferences = DataViewPreferences.Instance;
            Number ??= new GUIStyle(EditorStyles.textField) {
                alignment = preferences.numberAlignment,
            };
            Enum ??= new GUIStyle(EditorStyles.textField) {
                alignment = preferences.enumAlignment,
            };
            Text ??= new GUIStyle(EditorStyles.textField) {
                alignment = preferences.textAlignment,
                wordWrap = true
            };
        }
    }
}