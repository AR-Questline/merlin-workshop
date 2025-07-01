using System.IO;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Localization;
using System.Globalization;

namespace UnityEditor.Localization.UI
{
    class CustomLocaleUIWindow : EditorWindow
    {
        VisualElement m_root;
        Button m_createButton;
        VisualElement m_helpBox;

        string m_warningMessage = "The Locale Identifier is already being used by {0}. Using the Same Id may cause conflicts.";

        public static void ShowWindow()
        {
            var window = (CustomLocaleUIWindow)GetWindow(typeof(CustomLocaleUIWindow));
            window.titleContent = EditorGUIUtility.TrTextContent("Add Custom Locale");
            window.minSize = new Vector2(500, 500);
            window.ShowUtility();
        }

        public void OnEnable()
        {
            m_root = rootVisualElement;

            // Import UXML
            var asset = Resources.GetTemplateAsset(nameof(CustomLocaleUIWindow));
            asset.CloneTree(m_root);

            var localeName = m_root.Q<TextField>("customLocaleUI_localeName");
            var localeIdentifier = m_root.Q<TextField>("customLocaleUI_localeIdentifier");
            localeIdentifier.RegisterValueChangedCallback(evt =>
            {
                var enableCreateBTN = true;
                var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
                var locales = LocalizationEditorSettings.GetLocales();
                var localeIDName = localeIdentifier.value;
                foreach (var culture in cultures)
                {
                    if (culture.Name == localeIdentifier.value && localeIdentifier.value.Length > 1)
                    {
                        enableCreateBTN = false;
                        localeIDName = culture.ToString();
                    }
                }

                for (int i = 0; i < (int)SystemLanguage.Unknown; ++i)
                {
                    var localeID = new LocaleIdentifier((SystemLanguage)i);
                    if (localeID.Code == localeIdentifier.value && localeIdentifier.value.Length > 1)
                    {
                        enableCreateBTN = false;
                        localeIDName = localeID.ToString();
                    }
                }

                foreach (var availableLocale in locales)
                {
                    if (availableLocale.Identifier.Code == localeIdentifier.value && localeIdentifier.value.Length > 1)
                    {
                        enableCreateBTN = false;
                        localeIDName = availableLocale.name;
                    }
                }

                m_createButton.SetEnabled(enableCreateBTN);
                if (!enableCreateBTN)
                {
                    if (m_helpBox == null)
                        AddHelpInfo(localeIDName);
                }
                else
                {
                    if (m_helpBox != null)
                    {
                        m_root.Remove(m_helpBox);
                        m_helpBox = null;
                    }
                }
            });
            m_createButton = m_root.Q<Button>("customLocaleUI_CreateBTN");
            m_createButton.clickable.clicked += () =>
            {
                CreateCustomLocale(localeName.value, localeIdentifier.value);
            };
        }

        internal void CreateCustomLocale(string localeName, string localeIdentifier)
        {
            var path = EditorUtility.SaveFilePanel("Save the locale to folder", "Assets/", $"{localeName} ({localeIdentifier}).asset", "asset");
            if (!string.IsNullOrEmpty(path))
                ExportCustomLocales(localeName, localeIdentifier, path);
        }

        internal void ExportCustomLocales(string localeName, string localeIdentifier, string path)
        {
            var locale = CreateInstance<Locale>();
            locale.Identifier = localeIdentifier;
            locale.name = localeName;
            CreateAsset(path, locale);
            Close();
        }

        void CreateAsset(string path, Locale locale)
        {
            var relativePath = PathHelper.MakePathRelative(path);
            var assetPath = AssetDatabase.GenerateUniqueAssetPath(relativePath);
            AssetDatabase.CreateAsset(locale, assetPath);
        }

        void AddHelpInfo(string localeName)
        {
            const float margin = 2;
            const float padding = 1;
            m_helpBox = null;
            m_helpBox = new VisualElement()
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = margin,
                    marginRight = margin,
                    marginLeft = margin,
                    marginTop = margin,
                    paddingTop = padding,
                    paddingBottom = padding,
                    paddingRight = padding,
                    paddingLeft = padding,
                    fontSize = 9,
                    height = 30
                }
            };
            m_helpBox.AddToClassList("unity-box");
            m_helpBox.Add(new Image() { image = EditorGUIUtility.FindTexture("d_console.warnicon"), scaleMode = ScaleMode.ScaleToFit });
            m_helpBox.Add(new Label(string.Format(m_warningMessage, localeName)));
            m_root.Add(m_helpBox);
        }
    }
}
