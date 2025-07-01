#if ENABLE_PROPERTY_VARIANTS || PACKAGE_DOCS_GENERATION

using UnityEngine;
using UnityEngine.Localization.PropertyVariants.TrackedProperties;

namespace UnityEditor.Localization.PropertyVariants
{
    [CustomPropertyDrawer(typeof(LocalizedStringProperty), true)]
    [CustomPropertyDrawer(typeof(LocalizedAssetProperty), true)]
    class TrackedLocalizedPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var path = property.FindPropertyRelative("m_PropertyPath");
            var locString = property.FindPropertyRelative("m_Localized");
            label = new GUIContent(ObjectNames.NicifyVariableName(path.stringValue));
            EditorGUI.PropertyField(position, locString, label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var locString = property.FindPropertyRelative("m_Localized");
            return EditorGUI.GetPropertyHeight(locString) + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}

#endif
