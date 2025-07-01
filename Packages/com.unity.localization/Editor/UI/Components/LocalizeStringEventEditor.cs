using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

namespace UnityEditor.Localization.UI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(LocalizeStringEvent))]
    class LocalizeStringEventEditor : UnityEditor.Editor
    {
        SerializedProperty m_StringReference;
        SerializedProperty m_FormatArguments;
        SerializedProperty m_UpdateString;

        void OnEnable()
        {
            m_StringReference = serializedObject.FindProperty("m_StringReference");
            m_FormatArguments = serializedObject.FindProperty("m_FormatArguments");
            m_UpdateString = serializedObject.FindProperty("m_UpdateString");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_StringReference);
            EditorGUILayout.PropertyField(m_UpdateString);

            if (m_FormatArguments.arraySize > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.HelpBox("LocalizeStringEvent Format Arguments have been deprecated. Please use the String Reference Format Arguments field instead. Press the Upgrade button to copy the arguments over to String Reference Arguments.", MessageType.Warning);

                if (GUILayout.Button("Upgrade"))
                {
                    var stringRefArgs = m_StringReference.FindPropertyRelative("m_LocalVariables");
                    for (int i = 0; i < m_FormatArguments.arraySize; ++i)
                    {
                        var reference = m_FormatArguments.GetArrayElementAtIndex(i).objectReferenceValue;
                        if (reference == null)
                            continue;

                        var newArg = stringRefArgs.AddArrayElement();
                        var name = newArg.FindPropertyRelative("name");
                        var value = newArg.FindPropertyRelative("variable");

                        name.stringValue = (stringRefArgs.arraySize - 1).ToString();
                        value.managedReferenceValue = new ObjectVariable { Value = reference };
                    }

                    m_FormatArguments.arraySize = 0;
                }

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_FormatArguments);
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
