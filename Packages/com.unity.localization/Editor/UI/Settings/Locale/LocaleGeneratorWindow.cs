using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;

namespace UnityEditor.Localization.UI
{
    class LocaleGeneratorWindow : EditorWindow
    {
        class Texts
        {
            public GUIContent generateLocalesButton = EditorGUIUtility.TrTextContent("Add Locales");
            public const string progressTitle = "Generating Locales";
            public const string saveDialog = "Save locales to folder";

            public GUIContent[] toolbarButtons =
            {
                EditorGUIUtility.TrTextContent("Select All", "Select all visible locales"),
                EditorGUIUtility.TrTextContent("Deselect All", "Deselect all visible locales")
            };
        }

        static Texts s_Texts;

        static readonly List<LocaleIdentifier> s_Choices = GenerateLocaleChoices();

        const float k_WindowFooterHeight = 150;

        [SerializeField] internal SearchField m_SearchField;
        [SerializeField] internal LocaleGeneratorListView m_ListView;

        static List<LocaleIdentifier> GenerateLocaleChoices()
        {
            var locales = new List<LocaleIdentifier>();

            var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

            for (int i = 0; i < cultures.Length; ++i)
            {
                var cultureInfo = cultures[i];

                if (cultureInfo.LCID == CultureInfo.InvariantCulture.LCID)
                    continue;

                // Ignore legacy cultures
                if (cultureInfo.EnglishName.Contains("Legacy"))
                    continue;

                locales.Add(new LocaleIdentifier(cultureInfo));
            }

            return locales;
        }

        public static void ShowWindow()
        {
            var window = (LocaleGeneratorWindow)GetWindow(typeof(LocaleGeneratorWindow));
            window.titleContent = EditorGUIUtility.TrTextContent("Add Locale", EditorIcons.Locale);
            window.minSize = new Vector2(500, 500);
            window.ShowUtility();
        }

        void OnEnable()
        {
            m_ListView = new LocaleGeneratorListView();
            m_ListView.Items = s_Choices;
            m_SearchField = new SearchField();
            m_SearchField.downOrUpArrowKeyPressed += m_ListView.SetFocusAndEnsureSelectedItem;
        }

        void OnGUI()
        {
            if (s_Texts == null)
                s_Texts = new Texts();

            DrawLocaleList();

            using (new EditorGUI.DisabledScope(m_ListView.SelectedCount == 0))
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(s_Texts.generateLocalesButton, GUILayout.Width(180)))
                {
                    ExportSelectedLocales(m_ListView.GetSelectedLocales());
                    Close();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        void DrawLocaleList()
        {
            m_ListView.searchString = m_SearchField.OnToolbarGUI(m_ListView.searchString);
            var rect = EditorGUILayout.GetControlRect(false, position.height - k_WindowFooterHeight);
            m_ListView.OnGUI(rect);

            var selection = GUILayout.Toolbar(-1, s_Texts.toolbarButtons, EditorStyles.toolbarButton);
            if (selection >= 0)
            {
                m_ListView.SelectLocales(selection == 0);
            }
        }

        internal static void ExportSelectedLocales(List<LocaleIdentifier> localeIdentifiers)
        {
            var path = EditorUtility.SaveFolderPanel(Texts.saveDialog, "Assets/", "");
            if (!string.IsNullOrEmpty(path))
                ExportSelectedLocales(path, localeIdentifiers);
        }

        internal static void ExportSelectedLocales(string path, List<LocaleIdentifier> selectedIdentifiers)
        {
            try
            {
                // Generate the locale assets
                EditorUtility.DisplayProgressBar(Texts.progressTitle, "Creating Locale Objects", 0);
                var localeDict = new Dictionary<LocaleIdentifier, Locale>(); // Used for quick look up of parents
                var locales = new List<Locale>();

                foreach (var selectedIdentifier in selectedIdentifiers)
                {
                    var locale = CreateInstance<Locale>();
                    locale.Identifier = selectedIdentifier;
                    locale.name = selectedIdentifier.CultureInfo.EnglishName;
                    locales.Add(locale);
                    localeDict[selectedIdentifier] = locale;
                }

                // When checking for fallbacks we also need to take into account the existing locales
                var allLocales = new List<Locale>(locales);
                allLocales.AddRange(LocalizationEditorSettings.GetLocales());

                // Set up fallbacks
                foreach (var locale in allLocales)
                {
                    // A custom locale may not have a CultureInfo.
                    var cultureInfo = locale.Identifier.CultureInfo;
                    if (cultureInfo == null)
                        continue;

                    var localeParentCultureInfo = locale.Identifier.CultureInfo.Parent;
                    Locale foundParent = null;
                    while (localeParentCultureInfo != CultureInfo.InvariantCulture && foundParent == null)
                    {
                        localeDict.TryGetValue(localeParentCultureInfo.Name, out foundParent);
                        localeParentCultureInfo = localeParentCultureInfo.Parent;
                    }

                    if (foundParent != null)
                    {
                        locale.Metadata.AddMetadata(new FallbackLocale(foundParent));
                        EditorUtility.SetDirty(locale);
                    }
                }

                // Export the assets

                // Disabled StartAssetEditing and StopAssetEditing due to bug LOC-195.
                var relativePath = PathHelper.MakePathRelative(path);
                for (int i = 0; i < locales.Count; ++i)
                {
                    var locale = locales[i];
                    EditorUtility.DisplayProgressBar(Texts.progressTitle, "Creating Asset " + locale.name, i / (float)locales.Count);
                    var assetPath = Path.Combine(relativePath, $"{locale.name} ({locale.Identifier.Code}).asset");
                    assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
                    AssetDatabase.CreateAsset(locale, assetPath);
                }

                // Import the Locales now instead of waiting for them to be imported via the asset post processor.
                // If we wait for them to be imported during the asset post processor then they will not be available
                // until all current assets have been imported.
                // This can cause duplicate Locales to be created when importing multiple tables with missing Locales.
                locales.ForEach(l => LocalizationEditorSettings.AddLocale(l));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
