#if ENABLE_PROPERTY_VARIANTS || PACKAGE_DOCS_GENERATION

using UnityEngine;
using UnityEngine.Localization.PropertyVariants.TrackedObjects;

namespace UnityEditor.Localization.PropertyVariants
{
    [CustomPropertyDrawer(typeof(TrackedObject.TrackedPropertiesCollection), false)]
    class TrackedObjectPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUIUtility.singleLineHeight;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);
            position.MoveToNextLine();

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                var trackedProps = property.FindPropertyRelative("items");
                for (int i = 0; i < trackedProps.arraySize; ++i)
                {
                    var item = trackedProps.GetArrayElementAtIndex(i);
                    position.height = EditorGUI.GetPropertyHeight(item);

                    var split = position.SplitHorizontalFixedWidthRight(17);
                    EditorGUI.PropertyField(split.left, item, true);

                    split.right.height = EditorGUIUtility.singleLineHeight;
                    if (GUI.Button(split.right, GUIContent.none, "OL Minus"))
                    {
                        trackedProps.DeleteArrayElementAtIndex(i);
                    }

                    position.MoveToNextLine();
                }

                EditorGUI.indentLevel--;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Foldout

            if (property.isExpanded)
            {
                var trackedProps = property.FindPropertyRelative("items");
                for (int i = 0; i < trackedProps.arraySize; ++i)
                {
                    var item = trackedProps.GetArrayElementAtIndex(i);
                    height += EditorGUI.GetPropertyHeight(item) + EditorGUIUtility.standardVerticalSpacing;
                }
            }
            return height;
        }
    }
}

#endif
