#if UNITY_EDITOR && ENABLE_UNITY_OCCLUSION && (HDRP_10_0_0_OR_NEWER || URP_10_0_0_OR_NEWER)

using UnityEditor;
using UnityEngine;

namespace Unity.Rendering.Occlusion
{
    [CustomEditor(typeof(Occluder))]
    [CanEditMultipleObjects]
    class OccluderInspector : Editor
    {

        class Contents
        {
            public GUIContent meshContent = EditorGUIUtility.TrTextContent("Mesh", "The occluder mesh");
            public GUIContent positionContent = EditorGUIUtility.TrTextContent("Position", "The position of this occluder relative to transform.");
            public GUIContent rotationContent = EditorGUIUtility.TrTextContent("Rotation", "The rotation of this occluder relative to transform.");
            public GUIContent scaleContent = EditorGUIUtility.TrTextContent("Scale", "The scale of this occluder relative to transform.");
        }
        static Contents s_Contents;

        SerializedProperty m_Mesh;
        SerializedProperty m_Position;
        SerializedProperty m_Rotation;
        SerializedProperty m_Scale;

        public void OnEnable()
        {
            m_Mesh = serializedObject.FindProperty("mesh");
            m_Position = serializedObject.FindProperty("localPosition");
            m_Rotation = serializedObject.FindProperty("localRotation");
            m_Scale = serializedObject.FindProperty("localScale");
        }

        public override void OnInspectorGUI()
        {
            if (s_Contents == null)
                s_Contents = new Contents();

            if (!EditorGUIUtility.wideMode)
            {
                EditorGUIUtility.wideMode = true;
                EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth - 212;
            }

            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Mesh, s_Contents.meshContent);
            EditorGUILayout.LabelField("Local Transform");
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_Position, s_Contents.positionContent);
            EditorGUILayout.PropertyField(m_Rotation, s_Contents.rotationContent);
            EditorGUILayout.PropertyField(m_Scale, s_Contents.scaleContent);
            EditorGUI.indentLevel--;

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif
