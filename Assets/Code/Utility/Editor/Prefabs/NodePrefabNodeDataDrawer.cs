using UnityEditor;
using UnityEngine;

namespace Awaken.Utility.Editor.Prefabs {
    [CustomPropertyDrawer(typeof(PrefabVariantCrawler.Node<PrefabVariantCrawler.PrefabNodeData>))]
    public class NodePrefabNodeDataDrawer : PropertyDrawer {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return GetPropertyHeight(property);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            var dataProp = property.FindPropertyRelative("data");
            var prefabProp = dataProp.FindPropertyRelative("_prefab");
            var variantsProp = property.FindPropertyRelative("variants");

            EditorGUI.PropertyField(position, prefabProp, true);
            position.y += EditorGUIUtility.singleLineHeight;

            if (variantsProp.arraySize > 0) {
                EditorGUI.PropertyField(position, variantsProp, new GUIContent("Variants"), true);
            }

            EditorGUI.EndProperty();
        }

        float GetPropertyHeight(SerializedProperty property) {
            var variantsProp = property.FindPropertyRelative("variants");

            var dataSize = EditorGUIUtility.singleLineHeight;
            var variantsSize = 0f;
            if (variantsProp.arraySize > 0) {
                // Header
                variantsSize += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                if (variantsProp.isExpanded) {
                    // Footer
                    variantsSize += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2;

                    for (var i = 0; i < variantsProp.arraySize; i++) {
                        var variant = variantsProp.GetArrayElementAtIndex(i);
                        variantsSize += GetPropertyHeight(variant);
                    }
                }
            }


            return dataSize + variantsSize + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
