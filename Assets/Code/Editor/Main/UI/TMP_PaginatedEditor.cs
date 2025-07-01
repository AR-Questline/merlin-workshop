using Awaken.TG.Main.UI.Components;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Main.UI {
    [CustomEditor(typeof(TMP_Paginated))]
    public class TMP_PaginatedEditor : TMP_EditorPanelUI {
        SerializedProperty _pageLeft;
        SerializedProperty _pageRight;

        protected override void OnEnable() {
            base.OnEnable();
            _pageLeft = serializedObject.FindProperty("pageLeft");
            _pageRight = serializedObject.FindProperty("pageRight");
        }

        protected override void DrawExtraSettings() {
            base.DrawExtraSettings();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.ObjectField(_pageLeft, new GUIContent("Page Left", "Changes pages down"));
            if (EditorGUI.EndChangeCheck())
            {
                m_HavePropertiesChanged = true;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.ObjectField(_pageRight, new GUIContent("Page Right", "Changes pages up"));
            if (EditorGUI.EndChangeCheck())
            {
                m_HavePropertiesChanged = true;
            }

            EditorGUILayout.Space();
        }
    }
}