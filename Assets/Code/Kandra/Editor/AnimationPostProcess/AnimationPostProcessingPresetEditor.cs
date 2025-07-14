using Awaken.Kandra.AnimationPostProcess;
using Awaken.Utility.UI;
using UnityEditor;
using UnityEngine;

namespace Awaken.Kandra.Editor.AnimationPostProcess {
    [CustomEditor(typeof(AnimationPostProcessingPreset))]
    public class AnimationPostProcessingPresetEditor : UnityEditor.Editor {
        const int Padding = 2;

        bool _expandedTransformations = true;
        SerializedProperty _transformationsProperty;

        void OnEnable() {
            _transformationsProperty = serializedObject.FindProperty(nameof(AnimationPostProcessingPreset.transformations));
        }

        public override void OnInspectorGUI() {
            serializedObject.UpdateIfRequiredOrScript();

            _expandedTransformations = EditorGUILayout.Foldout(_expandedTransformations, "Transformations", true);
            if (!_expandedTransformations) {
                return;
            }

            var height = EditorGUIUtility.singleLineHeight * (_transformationsProperty.arraySize + 1);
            var tableRect = (PropertyDrawerRects)EditorGUILayout.GetControlRect(false, height);

            EditorGUI.DrawRect(tableRect.Rect, new Color(0.1f, 0.1f, 0.1f, 0.8f));

            var headerRect = (PropertyDrawerRects)tableRect.AllocateTop(EditorGUIUtility.singleLineHeight);
            var (boneColumn, positionColumn, scaleColumn) = GetColumns(ref headerRect);

            // Draw table header
            EditorGUI.DrawRect(boneColumn, new Color(0.2f, 0.2f, 0.2f, 0.8f));
            EditorGUI.LabelField(boneColumn, "Bone", EditorStyles.boldLabel);
            EditorGUI.DrawRect(positionColumn, new Color(0.2f, 0.2f, 0.2f, 0.8f));
            EditorGUI.LabelField(positionColumn, "Position", EditorStyles.boldLabel);
            EditorGUI.DrawRect(scaleColumn, new Color(0.2f, 0.2f, 0.2f, 0.8f));
            EditorGUI.LabelField(scaleColumn, "Scale", EditorStyles.boldLabel);

            var preset = (AnimationPostProcessingPreset)target;
            // Draw table rows
            for (int i = 0; i < _transformationsProperty.arraySize; i++) {
                var rowRect = (PropertyDrawerRects)tableRect.AllocateTop(EditorGUIUtility.singleLineHeight);
                (boneColumn, positionColumn, scaleColumn) = GetColumns(ref rowRect);
                var transformationProperty = _transformationsProperty.GetArrayElementAtIndex(i);

                var boneName = preset.transformations[i].BoneName;
                var positionProp = transformationProperty.FindPropertyRelative("position");
                var scaleProp = transformationProperty.FindPropertyRelative("scale");

                EditorGUI.DrawRect(boneColumn, new Color(0.25f, 0.25f, 0.25f, 0.6f));
                EditorGUI.LabelField(boneColumn, boneName);

                EditorGUI.DrawRect(positionColumn, new Color(0.25f, 0.25f, 0.25f, 0.6f));
                EditorGUI.PropertyField(positionColumn, positionProp, GUIContent.none);

                EditorGUI.DrawRect(scaleColumn, new Color(0.25f, 0.25f, 0.25f, 0.6f));
                EditorGUI.PropertyField(scaleColumn, scaleProp, GUIContent.none);
            }

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Open editor window")) {
                AnimationPostProcessingEditorWindow.Open(preset);
            }
        }

        (Rect, Rect, Rect) GetColumns(ref PropertyDrawerRects rowRect) {
            var fullWidth = rowRect.Rect.width;
            var boneWidth = fullWidth * 0.2f;
            var positionWidth = fullWidth * 0.5f;
            var scaleWidth = fullWidth * 0.3f;

            var boneRect = rowRect.AllocateLeftWithPadding(boneWidth, Padding);
            var positionRect = rowRect.AllocateLeftWithPadding(positionWidth, Padding);
            var scaleRect = rowRect.AllocateLeftWithPadding(scaleWidth, Padding);
            return (boneRect, positionRect, scaleRect);
        }
    }
}