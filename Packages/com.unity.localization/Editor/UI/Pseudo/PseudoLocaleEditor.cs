using System;
using System.Globalization;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Pseudo;

namespace UnityEditor.Localization.UI
{
    [CustomEditor(typeof(PseudoLocale))]
    class PseudoLocaleEditor : LocaleEditor
    {
        class Styles
        {
            public static readonly GUIContent methods = EditorGUIUtility.TrTextContent("Pseudo-Localization Methods", "The pseudo-localization transformations that will be applied in order(top to bottom).");
            public static readonly GUIContent preview = EditorGUIUtility.TrTextContent("Pseudo-Localization Preview", "Preview the result of applying the pseudo-localization methods to a sample string.");
            public static readonly GUIContent sourceLocale = EditorGUIUtility.TrTextContent("Source Locale", "The source locale that will be used when loading the localized strings before they have pseudo-localization applied.");
        }

        const string k_PreviewTextPref = "Localization-Pseudo-PreviewText";
        const string k_PreviewTextExpandPref = "Localization-Pseudo-PreviewTextExpanded";

        SerializedProperty m_Methods;
        ReorderableListExtended m_MethodsList;
        string m_PseudoPreviewText;
        Locale m_SourceLocale;

        string PreviewText
        {
            get => EditorPrefs.GetString(k_PreviewTextPref, "This is an example string");
            set => EditorPrefs.SetString(k_PreviewTextPref, value);
        }

        bool PreviewExpanded
        {
            get => EditorPrefs.GetBool(k_PreviewTextExpandPref, false);
            set => EditorPrefs.SetBool(k_PreviewTextExpandPref, value);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_Methods = serializedObject.FindProperty("m_Methods");
            m_MethodsList = new ReorderableListExtended(m_Methods.serializedObject, m_Methods);
            m_MethodsList.headerHeight = 2;
            m_MethodsList.onAddDropdownCallback = AddMethod;
            m_SourceLocale = LocalizationEditorSettings.GetLocale(m_Code.stringValue);
            UpdatePreviewText();
        }

        void UpdatePreviewText()
        {
            var pseudoLocale = target as PseudoLocale;

            // If we have a CharacterSubstitutor using LoopFromPrevious then we need to reset
            // its loop counter or it will keep changing when the editor updates.
            pseudoLocale.Reset();

            m_PseudoPreviewText = pseudoLocale.GetPseudoString(PreviewText);

            // Append details
            var originalText = new StringInfo(PreviewText);
            var pseudoText = new StringInfo(m_PseudoPreviewText);
            m_PseudoPreviewText += $"\n\nLength({originalText.LengthInTextElements}/{pseudoText.LengthInTextElements})";
        }

        protected override void DoLocaleCodeField()
        {
            EditorGUI.BeginChangeCheck();
            var locale = EditorGUILayout.ObjectField(Styles.sourceLocale, m_SourceLocale, typeof(Locale), false) as Locale;
            if (EditorGUI.EndChangeCheck() && !(locale is PseudoLocale))
            {
                m_SourceLocale = locale;
                m_Code.stringValue = locale != null ? locale.Identifier.Code : string.Empty;
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (serializedObject.UpdateIfRequiredOrScript())
            {
                m_PseudoPreviewText = null;
                m_SourceLocale = LocalizationEditorSettings.GetLocale(m_Code.stringValue);
            }

            m_Methods.isExpanded = EditorGUILayout.Foldout(m_Methods.isExpanded, Styles.methods, true);
            if (m_Methods.isExpanded)
            {
                m_MethodsList.DoLayoutList();
            }

            PreviewExpanded = EditorGUILayout.Foldout(PreviewExpanded, Styles.preview, true);
            if (PreviewExpanded)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUI.BeginChangeCheck();
                var newPreviewText = EditorGUILayout.TextArea(PreviewText);
                if (EditorGUI.EndChangeCheck())
                {
                    PreviewText = newPreviewText;
                    m_PseudoPreviewText = null;
                }

                if (string.IsNullOrEmpty(m_PseudoPreviewText))
                    UpdatePreviewText();

                GUILayout.Label(m_PseudoPreviewText, EditorStyles.wordWrappedLabel);

                GUILayout.EndVertical();
            }

            if (serializedObject.ApplyModifiedProperties())
                m_PseudoPreviewText = null;
        }

        void AddMethod(Rect rect, ReorderableList list)
        {
            var menu = new GenericMenu();
            var methodTypes = TypeCache.GetTypesDerivedFrom<IPseudoLocalizationMethod>();

            for (int i = 0; i < methodTypes.Count; ++i)
            {
                var mt = methodTypes[i];
                if (mt.IsAbstract)
                    continue;

                var name = ObjectNames.NicifyVariableName(mt.Name);
                menu.AddItem(new GUIContent(name), false, () =>
                {
                    var hasDefaultConstructor = mt.GetConstructors().Any(c => c.GetParameters().Length == 0);
                    if (!hasDefaultConstructor)
                    {
                        Debug.LogError($"Can not create an instance of {mt}, it does not have a default constructor.");
                        return;
                    }

                    var elementProp = list.serializedProperty.AddArrayElement();
                    elementProp.managedReferenceValue = Activator.CreateInstance(mt);
                    list.serializedProperty.serializedObject.ApplyModifiedProperties();
                });
            }

            menu.DropDown(rect);
        }
    }
}
