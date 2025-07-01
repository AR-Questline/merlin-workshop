using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.UI
{
    class LocalizationSettingsProvider : AssetSettingsProvider
    {
        class Texts
        {
            public GUIContent noSettingsMsg = EditorGUIUtility.TrTextContent("You have no active Localization Settings. Would you like to create one?");
            public GUIContent activeSettings = EditorGUIUtility.TrTextContent("Active Settings", "The Localization Settings that will be used by this project and included into any builds.");
        }
        static Texts s_Texts;

        string m_SearchContext;
        VisualElement m_Root;
        VisualElement m_Editor;

        public LocalizationSettingsProvider()
            : base("Project/Localization", () => LocalizationEditorSettings.ActiveLocalizationSettings)
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_SearchContext = searchContext;
            if (s_Texts == null)
                s_Texts = new Texts();

            m_Root = new ScrollView { style = { marginLeft = 9, marginTop = 1 }};
            m_Root.Add(new Label("Localization") { style = { marginBottom = 12, fontSize = 19, unityFontStyleAndWeight = FontStyle.Bold } });
            rootElement.Add(m_Root);
            if (LocalizationEditorSettings.ActiveLocalizationSettings != null)
            {
                m_Root.Add(new InspectorElement(LocalizationEditorSettings.ActiveLocalizationSettings));
            }
            else
            {
                m_Root.Add(new IMGUIContainer(CreateSettingsGUI));
            }
        }

        void CreateSettingsGUI()
        {
            if (LocalizationEditorSettings.ActiveLocalizationSettings == null)
            {
                if (Event.current.type != EventType.Layout && m_Editor != null)
                {
                    m_Editor.RemoveFromHierarchy();
                    m_Editor = null;
                }

                EditorGUI.BeginChangeCheck();
                var obj = EditorGUILayout.ObjectField(s_Texts.activeSettings, LocalizationEditorSettings.ActiveLocalizationSettings, typeof(LocalizationSettings), false) as LocalizationSettings;
                if (EditorGUI.EndChangeCheck())
                {
                    LocalizationEditorSettings.ActiveLocalizationSettings = obj;
                }

                EditorGUILayout.HelpBox(s_Texts.noSettingsMsg.text, MessageType.Info, true);
                if (GUILayout.Button("Create", GUILayout.Width(100)))
                {
                    var created = LocalizationSettingsMenuItems.CreateLocalizationAsset();
                    if (created != null)
                    {
                        LocalizationEditorSettings.ActiveLocalizationSettings = created;
                    }
                }
            }
            else if (Event.current.type != EventType.Layout && m_Editor == null)
            {
                m_Editor = new InspectorElement(LocalizationEditorSettings.ActiveLocalizationSettings);
                m_Root.Add(m_Editor);
            }
        }

        [SettingsProvider]
        static SettingsProvider CreateProjectSettingsProvider() => new LocalizationSettingsProvider();
    }
}
