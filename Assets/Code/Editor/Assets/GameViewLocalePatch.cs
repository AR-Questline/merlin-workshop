using System.Reflection;
using Awaken.TG.Main.Localization;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Addressables;
using UnityEngine.Localization.Settings;

namespace Awaken.TG.Editor.Assets {
    public static class GameViewLocalePatch {
        public static void ApplyPatch() {
            // Localization system will handle showing the menu in playmode
            LocalizationEditorSettings.ShowLocaleMenuInGameView = true;
            
            LoadSavedLocal();
            EditorStartupLocaleOverride.AttachCallbacks();
            
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            if (!EditorApplication.isPlayingOrWillChangePlaymode) {
                // Upon domain reload due to script changes or opening editor we want to show the menu
                ShowMenu();
            }
        }

        static void OnPlayModeStateChanged(PlayModeStateChange newPlayMode) {
            if (newPlayMode == PlayModeStateChange.EnteredEditMode) {
                ShowMenu();
            }
        }

        static void ShowMenu() {
            Assembly assembly = typeof(AddressableGroupRules).Assembly;
            var type = assembly.GetType("UnityEditor.Localization.UI.GameViewLanguageMenu");
            var showLanguageMenu = type.GetMethod("Show");
            showLanguageMenu.Invoke(null, null);
            
            LoadSavedLocal();
        }

        static void LoadSavedLocal() {
            var localeCode = EditorPrefs.GetString(EditorStartupLocaleOverride.EditorSelectedLanguage, null);
            if (!string.IsNullOrEmpty(localeCode)) {
                var locale = LocalizationEditorSettings.GetLocale(localeCode);
                if (locale != null) {
                    LocalizationSettings.SelectedLocale = locale;
                }
            }
        }
    }
}
