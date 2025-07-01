#if ENABLE_PROPERTY_VARIANTS || PACKAGE_DOCS_GENERATION

using UnityEditor.Localization.UI.PropertyVariants;
using UnityEngine;
using UnityEngine.Localization.PropertyVariants.TrackedProperties;

namespace UnityEditor.Localization.PropertyVariants
{
    [CustomPropertyDrawer(typeof(EnumTrackedProperty), true)]
    class TrackedEnumPropertyDrawer : TrackedPropertyDrawer
    {
        protected override void DrawValueField(Rect position, SerializedProperty value, GUIContent label, string path)
        {
            // Aquire the original field so that we can use the PropertyField to draw the enum correctly.
            var so = new SerializedObject(GameObjectLocalizerEditor.CurrentTarget);
            var trackedField = so.FindProperty(path);

            EditorGUI.BeginChangeCheck();
            trackedField.intValue = value.intValue;
            EditorGUI.PropertyField(position, trackedField, label);
            if (EditorGUI.EndChangeCheck())
            {
                value.intValue = trackedField.intValue;
            }
        }
    }
}

#endif
