using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Localization.UI
{
    class LocalizationPreferencesProvider : SettingsProvider
    {
        static readonly GUIContent showGameViewToolbar = EditorGUIUtility.TrTextContent("Locale Game View Menu", "Shows a menu for changing the selected locale in the GameView during playmode.");
        static readonly GUIContent stringPicker = new GUIContent("String Search Picker");
        static readonly GUIContent assetPicker = new GUIContent("Asset Search Picker");
        static readonly GUIContent tableReferenceMthod = new GUIContent("Table Reference Method", "The method that will be used when adding a reference to a table in the editor.");
        static readonly GUIContent entryReferenceMethod = new GUIContent("Entry Reference Method", "The method that will be used when adding a reference to a table entry in the editor.");

        public LocalizationPreferencesProvider()
            : base("Preferences/Localization", SettingsScope.User)
        {
            keywords = new HashSet<string>(new[] { "Localization", "Locale GameView Menu" });
        }

        public override void OnGUI(string searchContext)
        {
            GUILayout.Space(10);

            var rect = GetControlRect();
            EditorGUI.BeginChangeCheck();
            LocalizationEditorSettings.ShowLocaleMenuInGameView = EditorGUI.Toggle(rect, showGameViewToolbar, LocalizationEditorSettings.ShowLocaleMenuInGameView);
            if (EditorGUI.EndChangeCheck() && Application.isPlaying)
            {
                if (LocalizationEditorSettings.ShowLocaleMenuInGameView)
                    GameViewLanguageMenu.Show();
                else
                    GameViewLanguageMenu.Hide();
            }

            rect = GetControlRect();
            LocalizationEditorSettings.UseLocalizedAssetSearchPicker = EditorGUI.Toggle(rect, assetPicker, LocalizationEditorSettings.UseLocalizedAssetSearchPicker);

            rect = GetControlRect();
            LocalizationEditorSettings.UseLocalizedStringSearchPicker = EditorGUI.Toggle(rect, stringPicker, LocalizationEditorSettings.UseLocalizedStringSearchPicker);

            rect = GetControlRect();
            LocalizationEditorSettings.TableReferenceMethod = (TableReferenceMethod)EditorGUI.EnumPopup(rect, tableReferenceMthod, LocalizationEditorSettings.TableReferenceMethod);

            rect = GetControlRect();
            LocalizationEditorSettings.EntryReferenceMethod = (EntryReferenceMethod)EditorGUI.EnumPopup(rect, entryReferenceMethod, LocalizationEditorSettings.EntryReferenceMethod);
        }

        static Rect GetControlRect()
        {
            // Small indent to match the other preference editors.
            const float indent = 8;

            var rect = EditorGUILayout.GetControlRect();
            rect.xMin += indent;
            return rect;
        }

        [SettingsProvider]
        static SettingsProvider CreateProjectSettingsProvider() => new LocalizationPreferencesProvider();
    }
}
