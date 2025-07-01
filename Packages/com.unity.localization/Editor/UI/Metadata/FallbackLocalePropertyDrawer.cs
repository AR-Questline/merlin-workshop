using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;

namespace UnityEditor.Localization.UI
{
    [CustomPropertyDrawer(typeof(FallbackLocale))]
    class FallbackLocalePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var propertyRef = property.FindPropertyRelative("m_Locale");
            EditorGUI.BeginChangeCheck();
            var locale = EditorGUI.ObjectField(position, label, propertyRef.objectReferenceValue, typeof(Locale), false) as Locale;
            if (EditorGUI.EndChangeCheck())
            {
                // Produce an error if the assignment is cyclic
                var fb = property.GetActualObjectForSerializedProperty<FallbackLocale>(fieldInfo);
                fb.Locale = locale;

                // Reject if cyclic
                if (!fb.IsCyclic(locale))
                    propertyRef.objectReferenceValue = locale;
            }
        }
    }
}
