using UnityEngine;
using UnityEngine.Localization;

namespace UnityEditor.Localization.Samples
{
    /// <summary>
    /// If the Locale Generator does not contain a particular Locale you need then a Custom Locale can be created.
    /// This sample shows how to create Locales through script.
    /// The example will create a new menu called **Localization Samples** which can be used to execute the example code.
    /// </summary>
    public class CreateLocaleMenu
    {
        [MenuItem("Localization Samples/Locales/Create Japanese Locale")]
        static void CreateJapanese()
        {
            // Create a locale to represent Japanese.
            var locale = Locale.CreateLocale(SystemLanguage.Japanese);

            // Customize the name.
            locale.name = "Japanese(日本)";

            var path = EditorUtility.SaveFilePanelInProject("Save Japanese Locale Asset", locale.name, "asset", null);
            if (!string.IsNullOrEmpty(path))
                AssetDatabase.CreateAsset(locale, path);
        }

        [MenuItem("Localization Samples/Locales/Create Custom Locale")]
        static void CreateCustomLocale()
        {
            // Create a locale to represent a Custom language
            var locale = Locale.CreateLocale(new LocaleIdentifier("my language code"));

            // Customize the name.
            locale.name = "My Custom Language";

            var path = EditorUtility.SaveFilePanelInProject("Save Custom Locale Asset", locale.name, "asset", null);
            if (!string.IsNullOrEmpty(path))
                AssetDatabase.CreateAsset(locale, path);
        }
    }
}
