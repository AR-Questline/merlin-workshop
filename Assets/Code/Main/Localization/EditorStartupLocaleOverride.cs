using JetBrains.Annotations;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Awaken.TG.Main.Localization {
    [UsedImplicitly, UnityEngine.Scripting.Preserve]
    public class EditorStartupLocaleOverride : IStartupLocaleSelector {
        public const string EditorSelectedLanguage = "AR_EditorSelectedLanguage";
        const string InternalUnityEditorLocal = "com.unity.localization-edit-locale";

        public Locale GetStartupLocale(ILocalesProvider availableLocales) {
#if UNITY_EDITOR
            return availableLocales.GetLocale(UnityEditor.EditorPrefs.GetString(EditorSelectedLanguage, "en"));
#else
            return null;
#endif
        }

#if UNITY_EDITOR
        public static void AttachCallbacks() {
            LocalizationSettings.SelectedLocaleChanged -= OnLanguageChanged;
            LocalizationSettings.SelectedLocaleChanged += OnLanguageChanged;
        }
        
        static void OnLanguageChanged(Locale locale) {
            string identifierCode = locale.Identifier.Code;
            UnityEditor.EditorPrefs.SetString(EditorSelectedLanguage, identifierCode);
            UnityEditor.EditorPrefs.SetString(InternalUnityEditorLocal, identifierCode);
        } 
#endif
    }
}
