using System;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Localization;

namespace UnityEditor.Localization.UI
{
    [CustomEditor(typeof(Locale))]
    class LocaleEditor : UnityEditor.Editor
    {
        static class Styles
        {
            public static readonly GUIContent code = EditorGUIUtility.TrTextContent("Locale Code");
            public static readonly GUIContent cultureNotFoundLabel = EditorGUIUtility.TrTextContent("Not Found");
            public static readonly GUIContent cultureInfo = EditorGUIUtility.TrTextContent("Culture Info");
            public static readonly GUIContent formatCurrency = EditorGUIUtility.TrTextContent("Currency");
            public static readonly GUIContent formatDateTime = EditorGUIUtility.TrTextContent("Date/Time");
            public static readonly GUIContent formatNumber = EditorGUIUtility.TrTextContent("Number");
            public static readonly GUIContent formatterCode = EditorGUIUtility.TrTextContent("Custom Formatter Code", "The Language code to use when applying any culture specific string formatting, such as date, time, currency. By default, the Locale Code will be used however this field can be used to override this such as when you are using a custom Locale which has no known formatter.");
            public static readonly GUIContent formatCultureInfo = EditorGUIUtility.TrTextContent("Formatting Culture Info", "The Culture specific formatting that will be used for this Locale.");
            public static readonly GUIContent sortOrder = EditorGUIUtility.TrTextContent("Sort Order", "The order the Locales will appear in any sorted Lists. By default Locales are ordered by name however the Sort Order can be used to override this.");
            public static readonly GUIContent emptyCultureInfo = new GUIContent("-");
        }

        protected SerializedProperty m_Name;
        protected SerializedProperty m_Code;
        protected SerializedProperty m_FormatCultureCode;
        protected SerializedProperty m_UseCustomFormatter;
        protected SerializedProperty m_Metadata;
        protected SerializedProperty m_SortOrder;

        GUIContent m_CultureInfoLabel;
        GUIContent m_FormatCultureInfoLabel;
        GUIContent m_FormattingExampleDateTime;
        GUIContent m_FormattingExampleCurrency;
        GUIContent m_FormattingExampleNumber;

        protected virtual void OnEnable()
        {
            m_Name = serializedObject.FindProperty("m_LocaleName");
            m_Code = serializedObject.FindProperty("m_Identifier.m_Code");
            m_FormatCultureCode = serializedObject.FindProperty("m_CustomFormatCultureCode");
            m_UseCustomFormatter = serializedObject.FindProperty("m_UseCustomFormatter");
            m_Metadata = serializedObject.FindProperty("m_Metadata");
            m_SortOrder = serializedObject.FindProperty("m_SortOrder");

            if (string.IsNullOrEmpty(m_Name.stringValue))
            {
                m_Name.stringValue = serializedObject.FindProperty("m_Name").stringValue;
                serializedObject.ApplyModifiedProperties();
            }

            UpdateCultureLabels();
        }

        protected virtual void DoLocaleCodeField()
        {
            EditorGUILayout.PropertyField(m_Code, Styles.code);
        }

        public override void OnInspectorGUI()
        {
            if (serializedObject.UpdateIfRequiredOrScript())
                UpdateCultureLabels();

            EditorGUILayout.PropertyField(m_Name);
            DoLocaleCodeField();
            EditorGUILayout.LabelField(Styles.cultureInfo, m_CultureInfoLabel);

            EditorGUILayout.PropertyField(m_UseCustomFormatter);
            using (new EditorGUI.DisabledGroupScope(!m_UseCustomFormatter.boolValue))
            {
                EditorGUILayout.PropertyField(m_FormatCultureCode, Styles.formatterCode);
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(Styles.formatCultureInfo, m_FormatCultureInfoLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField(Styles.formatCurrency, m_FormattingExampleCurrency);
            EditorGUILayout.LabelField(Styles.formatDateTime, m_FormattingExampleDateTime);
            EditorGUILayout.LabelField(Styles.formatNumber, m_FormattingExampleNumber);
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_SortOrder, Styles.sortOrder);
            if (EditorGUI.EndChangeCheck())
            {
                LocalizationEditorSettings.EditorEvents.RaiseLocaleSortOrderChanged(this, target as Locale);
            }

            EditorGUILayout.PropertyField(m_Metadata);

            serializedObject.ApplyModifiedProperties();
        }

        void UpdateCultureLabels()
        {
            var localeIdentifier = new LocaleIdentifier(m_Code.stringValue);
            var cultureInfo = localeIdentifier.CultureInfo;
            m_CultureInfoLabel = GenerateCultureLabel(cultureInfo);

            var formatter = Locale.GetFormatter(m_UseCustomFormatter.boolValue, localeIdentifier, m_FormatCultureCode.stringValue);
            m_FormatCultureInfoLabel = GenerateCultureLabel(formatter);

            if (formatter != null)
            {
                m_FormattingExampleDateTime = new GUIContent(DateTime.Now.ToString(formatter));
                m_FormattingExampleCurrency = new GUIContent(string.Format(formatter, "{0:C}", 123));
                m_FormattingExampleNumber = new GUIContent(string.Format(formatter, "{0:N}", 123456789.12));
            }
            else
            {
                m_FormattingExampleDateTime = m_FormattingExampleCurrency = m_FormattingExampleNumber = Styles.emptyCultureInfo;
            }
        }

        static GUIContent GenerateCultureLabel(CultureInfo cultureInfo)
        {
            if (cultureInfo == null)
                return Styles.cultureNotFoundLabel;

            if (cultureInfo.NativeName == cultureInfo.EnglishName)
                return new GUIContent(cultureInfo.EnglishName);

            return new GUIContent($"{cultureInfo.EnglishName} - {cultureInfo.NativeName}");
        }
    }
}
