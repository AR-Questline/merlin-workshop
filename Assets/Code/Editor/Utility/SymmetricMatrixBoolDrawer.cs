using System;
using System.Reflection;
using Awaken.TG.Editor.Helpers;
using Awaken.TG.Utility.Collections;
using Awaken.TG.Utility.Maths;
using Awaken.Utility.Collections;
using Awaken.Utility.UI;
using UnityEditor;
using UnityEngine;

namespace Awaken.TG.Editor.Utility {
    [CustomPropertyDrawer(typeof(SymmetricMatrixBool))]
    public class SymmetricMatrixBoolDrawer : PropertyDrawer {
        const float LabelWidth = 150F;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            var rows = property.FindPropertyRelative("rows");
            return (rows.arraySize + 2) * EditorGUIUtility.singleLineHeight;
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var target = property.serializedObject.targetObject;
            var field = target.GetType().GetField(property.propertyPath, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var matrix = (SymmetricMatrixBool) field.GetValue(target);
            Func<int, string> labelProvider = GetLabelProvider(property);
            
            EditorGUI.BeginChangeCheck();
            
            var rows = new PropertyDrawerRects(position);
            EditorGUI.LabelField(rows.AllocateLine(), label);

            if (labelProvider != null) {
                for (int x = 0; x < matrix.Size; x++) {
                    var row = new PropertyDrawerRects(rows.AllocateLine());
                    EditorGUI.LabelField(row.AllocateLeft(LabelWidth), labelProvider(x));
                    for (int y = 0; y <= x; y++) {
                        matrix[x, y] = EditorGUI.Toggle(row.AllocateLeft(EditorGUIUtility.singleLineHeight), matrix[x, y]);
                    }
                }
                var bottom = new PropertyDrawerRects(rows.AllocateLine());
                bottom.AllocateLeft(LabelWidth);
                for (int y = 0; y < matrix.Size; y++) {
                    EditorGUI.LabelField(bottom.AllocateLeft(EditorGUIUtility.singleLineHeight), labelProvider(y).Remove(3, labelProvider(y).Length - 3));
                }
            } else {
                for (int x = 0; x < matrix.Size; x++) {
                    var row = new PropertyDrawerRects(rows.AllocateLine());
                    for (int y = 0; y <= x; y++) {
                        matrix[x, y] = EditorGUI.Toggle(row.AllocateLeft(EditorGUIUtility.singleLineHeight), matrix[x, y]);
                    }
                }
            }

            if (EditorGUI.EndChangeCheck()) {
                property.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        }

        Func<int, string> GetLabelProvider(SerializedProperty property) {
            var target = property.serializedObject.targetObject;
            var field = target.GetType().GetField(property.propertyPath, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var labelAttribute = field.GetCustomAttribute<SymmetricMatrixLabelsAttribute>();
            Func<int, string> labelProvider = null;
            if (labelAttribute != null) {
                var providerName = field.GetCustomAttribute<SymmetricMatrixLabelsAttribute>()?.Provider;
                var method = target.GetType().GetMethod(providerName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                labelProvider = index => (string) method.Invoke(target, new object[] { index });
            }
            return labelProvider;
        }
    }
}