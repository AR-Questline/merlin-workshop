using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.UI
{
    [CustomPropertyDrawer(typeof(Variable<>), true)]
    class PersistentVariablePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var value = property.FindPropertyRelative("m_Value");
            return new PropertyField(value, ManagedReferenceUtility.GetDisplayName(property.managedReferenceFullTypename).text);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (string.IsNullOrEmpty(label.text) && property.propertyType == SerializedPropertyType.ManagedReference)
                label = ManagedReferenceUtility.GetDisplayName(property.managedReferenceFullTypename);
            EditorGUI.PropertyField(position, property.FindPropertyRelative("m_Value"), label, true);
        }
    }
}
