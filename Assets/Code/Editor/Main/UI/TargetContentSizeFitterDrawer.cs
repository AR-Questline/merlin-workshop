using Awaken.TG.Main.Utility.UI;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace Awaken.TG.Editor.Main.UI {
    [CustomEditor(typeof(TargetContentSizeFitter), true)]
    [CanEditMultipleObjects]
    public class TargetContentSizeFitterDrawer : SelfControllerEditor {
        SerializedProperty m_ReferenceTransform;
        SerializedProperty m_ReferenceNotifier;
        SerializedProperty m_HorizontalFit;
        SerializedProperty m_HorizontalLimit;
        SerializedProperty m_HorizontalAllowance;
        SerializedProperty m_VerticalFit;
        SerializedProperty m_VerticalLimit;
        SerializedProperty m_VerticalAllowance;

        protected virtual void OnEnable() {
            m_ReferenceTransform = serializedObject.FindProperty("m_ReferenceTransform");
            m_ReferenceNotifier = serializedObject.FindProperty("m_ReferenceNotifier");
            m_HorizontalFit = serializedObject.FindProperty("m_HorizontalFit");
            m_HorizontalLimit = serializedObject.FindProperty("m_HorizontalLimit");
            m_HorizontalAllowance = serializedObject.FindProperty("m_HorizontalAllowance");
            m_VerticalFit = serializedObject.FindProperty("m_VerticalFit");
            m_VerticalLimit = serializedObject.FindProperty("m_VerticalLimit");
            m_VerticalAllowance = serializedObject.FindProperty("m_VerticalAllowance");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_ReferenceTransform, true);
            EditorGUILayout.PropertyField(m_ReferenceNotifier, true);

            if (m_ReferenceTransform.objectReferenceValue == null && m_ReferenceNotifier.objectReferenceValue == null) {
                EditorGUILayout.HelpBox(
                    $"A {nameof(RectTransform)} or {nameof(UIBehaviourNotifier)} is required for {nameof(TargetContentSizeFitter.FitMode.PreferredSize)} or {nameof(TargetContentSizeFitter.FitMode.LimitedPreferredSize)}",
                    MessageType.Warning);
            }

            EditorGUILayout.PropertyField(m_HorizontalFit, true);
            if (m_HorizontalFit.enumValueIndex == 3) {
                EditorGUILayout.PropertyField(m_HorizontalLimit, true);
            }

            if (m_HorizontalFit.enumValueIndex != 0) {
                EditorGUILayout.PropertyField(m_HorizontalAllowance, true);
            }

            EditorGUILayout.PropertyField(m_VerticalFit, true);
            if (m_VerticalFit.enumValueIndex == 3) {
                EditorGUILayout.PropertyField(m_VerticalLimit, true);
            }

            if (m_VerticalFit.enumValueIndex != 0) {
                EditorGUILayout.PropertyField(m_VerticalAllowance, true);
            }

            serializedObject.ApplyModifiedProperties();

            base.OnInspectorGUI();
        }
    }
}