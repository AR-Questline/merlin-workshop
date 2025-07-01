using System.Collections.Generic;
using UnityEngine.Localization.Settings;

namespace UnityEngine.Localization.Samples
{
    /// <summary>
    /// This example shows how a simple language selection menu can be implemented using IMGUI.
    /// This class shows how to provide custom Locales for the SimpleLocalesProvider, waiting for the SelectedLocaleAsync to be ready,
    /// creating custom labels for the Locales and responding to Locale changed events.
    /// </summary>
    public class LanguageSelectionMenuIMGUI : MonoBehaviour
    {
        public Rect windowRect = new Rect(0, 0, 300, 300);
        public Color selectColor = Color.yellow;
        public Color defaultColor = Color.gray;
        Vector2 m_ScrollPos;
        Dictionary<Locale, string> m_Labels = new Dictionary<Locale, string>();

        [Tooltip("Use the current active settings if possible or create a new one for the example")]
        public bool useActiveLocalizationSettings = false;

        void Start()
        {
            var localizationSettings = LocalizationSettings.GetInstanceDontCreateDefault();

            // Use included settings if one is available.
            if (useActiveLocalizationSettings && localizationSettings != null)
            {
                Debug.Log("Using included localization data");
                return;
            }

            Debug.Log("Creating default localization data");

            // Create our localization settings. If a LocalizationSettings asset has been created and configured in the editor then we can leave this step out.
            localizationSettings = ScriptableObject.CreateInstance<LocalizationSettings>();

            // Replace the default Locales Provider with something we can manage locally for the example
            var simpleLocalesProvider = new SimpleLocalesProvider();
            localizationSettings.SetAvailableLocales(simpleLocalesProvider);

            // Add the locales we support
            simpleLocalesProvider.AddLocale(Locale.CreateLocale(new LocaleIdentifier(SystemLanguage.Arabic)));
            simpleLocalesProvider.AddLocale(Locale.CreateLocale(new LocaleIdentifier(SystemLanguage.English)));
            simpleLocalesProvider.AddLocale(Locale.CreateLocale(new LocaleIdentifier(SystemLanguage.French)));
            simpleLocalesProvider.AddLocale(Locale.CreateLocale(new LocaleIdentifier(SystemLanguage.German)));
            simpleLocalesProvider.AddLocale(Locale.CreateLocale(new LocaleIdentifier(SystemLanguage.Japanese)));

            // Set English to be our default
            var startupSelectors = localizationSettings.GetStartupLocaleSelectors();
            startupSelectors.Clear(); // Remove the defaults
            startupSelectors.Add(new SpecificLocaleSelector { LocaleId = SystemLanguage.English });

            // Listen to any Locale changed events
            localizationSettings.OnSelectedLocaleChanged += OnSelectedLocaleChanged;

            // Make this our new LocalizationSettings
            LocalizationSettings.Instance = localizationSettings;
        }

        static void OnSelectedLocaleChanged(Locale newLocale)
        {
            Debug.Log("OnSelectedLocaleChanged: The locale just changed to " + newLocale);
        }

        void OnGUI()
        {
            windowRect = GUI.Window(GetHashCode(), windowRect, DrawWindowContents, "Select Language");
        }

        string GetLocaleLabel(Locale locale)
        {
            // Cache our generated labels.
            if (m_Labels.TryGetValue(locale, out var label))
                return label;

            // Create a label which shows the English name and the native name.
            var cultureInfo = locale.Identifier.CultureInfo;

            // If the Locale is custom then it may not have a CultureInfo
            if (cultureInfo != null)
            {
                // We will show a label in the form "<EnglishName>(<NativeName>)" when the Native name is not the same as the English name.
                if (cultureInfo.EnglishName != cultureInfo.NativeName)
                    label = $"{cultureInfo.EnglishName}({cultureInfo.NativeName})";
                else
                    label = cultureInfo.EnglishName;
            }
            else
            {
                label = locale.ToString();
            }

            m_Labels[locale] = label;
            return label;
        }

        void DrawWindowContents(int id)
        {
            // We need to wait for the Locales to be ready. If we are using the default LocalesProvider then they may still be loading.
            // SelectedLocaleAsync will wait for the Locales to be ready before selecting the SelectedLocale.
            if (!LocalizationSettings.SelectedLocaleAsync.IsDone)
            {
                GUILayout.Label("Initializing Locales: " + LocalizationSettings.SelectedLocaleAsync.PercentComplete);
                GUI.DragWindow();
                return;
            }

            // If we wanted to wait for everything to be initialized including preloading tables then we could check LocalizationSettings.InitializationOperation instead.
            //if (!LocalizationSettings.InitializationOperation.IsDone)
            //{
            //    GUILayout.Label("Initializing Localization: " + LocalizationSettings.InitializationOperation.PercentComplete);
            //    GUI.DragWindow();
            //    return;
            //}

            var availableLocales = LocalizationSettings.AvailableLocales;
            if (availableLocales.Locales.Count == 0)
            {
                GUILayout.Label("No Locales included in the active Localization Settings.");
            }
            else
            {
                // We will use a color to indicate the SelectedLocale
                var originalColor = GUI.contentColor;

                m_ScrollPos = GUILayout.BeginScrollView(m_ScrollPos);
                for (int i = 0; i < availableLocales.Locales.Count; ++i)
                {
                    var locale = availableLocales.Locales[i];

                    GUI.contentColor = LocalizationSettings.SelectedLocale == locale ? selectColor : defaultColor;
                    if (GUILayout.Button(GetLocaleLabel(locale)))
                    {
                        LocalizationSettings.SelectedLocale = locale;
                    }
                }
                GUILayout.EndScrollView();

                GUI.contentColor = originalColor;
            }

            GUI.DragWindow();
        }
    }
}
