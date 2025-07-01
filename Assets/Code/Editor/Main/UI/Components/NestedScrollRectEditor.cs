using Awaken.TG.Main.UI.Components;
using UnityEditor;
using UnityEditor.UI;

namespace Awaken.TG.Editor.Main.UI.Components {
    [CustomEditor(typeof(NestedScrollRect))]
    public class NestedScrollRectEditor : ScrollRectEditor {
        public override void OnInspectorGUI() {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(NestedScrollRect.routeToParent)));
            base.OnInspectorGUI();
        }
    }
}
