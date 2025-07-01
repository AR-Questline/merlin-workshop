using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.UI
{
    [CustomPropertyDrawer(typeof(LocaleIdentifier))]
    class LocaleIdentifierPropertyDrawer : PropertyDrawer
    {
        readonly float k_ExpandedHeight = (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2;

        /// <summary>
        /// UI Toolkit version
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var code = property.FindPropertyRelative("m_Code");

            var foldout = new Foldout { text = property.displayName };
            foldout.BindProperty(property);

            var localeField = new ObjectField { objectType = typeof(Locale), value = LocalizationEditorSettings.GetLocale(code.stringValue) };
            localeField.AddToClassList("unity-base-field__input");
            localeField.RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation());
            localeField.RegisterCallback<MouseUpEvent>(evt => evt.StopPropagation());
            localeField.RegisterValueChangedCallback(evt =>
            {
                var locale = evt.newValue as Locale;
                code.stringValue = locale != null ? locale.Identifier.Code : string.Empty;
                code.serializedObject.ApplyModifiedProperties();
            });
            foldout.hierarchy[0].Add(localeField);
            foldout.hierarchy[0].hierarchy[0].RemoveFromClassList("unity-base-field__input");
            foldout.hierarchy[0].hierarchy[0].AddToClassList("unity-base-field__label");

            var codeField = new TextField { label = "Code" };
            codeField.RegisterValueChangedCallback(evt =>
            {
                localeField.SetValueWithoutNotify(LocalizationEditorSettings.GetLocale(evt.newValue));
            });
            codeField.BindProperty(code);
            foldout.Add(codeField);

            return foldout;
        }

        /// <summary>
        /// IMGUI Version
        /// </summary>
        /// <param name="position"></param>
        /// <param name="property"></param>
        /// <param name="label"></param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var code = property.FindPropertyRelative("m_Code");

            var foldRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldRect, property.isExpanded, label, true);

            EditorGUI.BeginChangeCheck();
            EditorGUI.BeginProperty(foldRect, GUIContent.none, property);
            var localeRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - foldRect.width, foldRect.height);
            var newSelectedLocale = EditorGUI.ObjectField(localeRect, LocalizationEditorSettings.GetLocale(code.stringValue), typeof(Locale), false) as Locale;
            EditorGUI.EndProperty();
            if (EditorGUI.EndChangeCheck())
            {
                code.stringValue = newSelectedLocale != null ? newSelectedLocale.Identifier.Code : string.Empty;
            }

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                position.height = EditorGUIUtility.singleLineHeight;

                position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.PropertyField(position, code);
                EditorGUI.indentLevel--;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return property.isExpanded ? k_ExpandedHeight : EditorGUIUtility.singleLineHeight;
        }
    }
}
