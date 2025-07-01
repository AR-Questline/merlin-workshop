using UnityEditor.UIElements;
using UnityEngine.Localization.SmartFormat.Extensions;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.UI
{
    // This is a workaround because UI Toolkit does not support MaskFields (case 1295231)
    [CustomPropertyDrawer(typeof(TimeFormatter))]
    class TimeFormatterPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new Foldout { text = "Time Formatter", value = false };
            var names = new PropertyField(property.FindPropertyRelative("m_Names"));
            root.Add(names);

            var options = property.FindPropertyRelative("m_DefaultFormatOptions");
            var optionsPanel = new IMGUIContainer(() =>
            {
                EditorGUILayout.PropertyField(options);
            });
            root.Add(optionsPanel);

            return root;
        }
    }
}
