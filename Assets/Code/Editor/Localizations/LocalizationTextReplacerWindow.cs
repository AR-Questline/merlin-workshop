using System.Text.RegularExpressions;
using Awaken.TG.Main.Localization;
using Awaken.Utility;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Awaken.TG.Editor.Localizations {
    public class LocalizationTextReplacerWindow : OdinEditorWindow {
        [InfoBox("This window allows to find and replace text in all localized tables. " +
                 "Use regular expressions to find text, and specify the replacement text.")]
        [ShowInInspector] string _expression;
        [ShowInInspector] string _replacement = LocTerms.NonBreakingSpace + "%";
        
        // === Initialization
        [MenuItem("TG/Localization/Localization Text Replacer")]
        public static void ShowWindow() {
            ShowAndInit();
            return;

            static async void ShowAndInit() {
                LocalizationSettings.Instance.ResetState();
                await LocalizationSettings.InitializationOperation.Task;
                GetWindow<LocalizationTextReplacerWindow>();
            }
        }

        [Button]
        void FillReplacementFieldWithNonBreakingSpace() {
            _replacement = LocTerms.NonBreakingSpace.ToString();
        }
        
        [Button("Replace")]
        void Replace() {
            foreach (var tableId in LocalizationHelper.StringTables) {
                StringTable stringTable = LocalizationSettings.StringDatabase.GetTable(tableId, LocalizationSettings.ProjectLocale);
                foreach (var entry in stringTable.Values) {
                    LocTermData locTermData = LocalizationTools.TryGetTermData(entry.Key);
                    foreach(var locale in LocalizationSettings.AvailableLocales.Locales) {
                        var locEntry = locTermData.EntryFor(locale);
                        if (locEntry == null || !Regex.IsMatch(locEntry.Value, _expression)) {
                            continue;
                        }
                        
                        // get replacement 
                        var newTranslation= Regex.Replace(locEntry.Value, _expression, _replacement);
                        
                        // set as redacted if was redacted before
                        TermStatusMeta meta = locEntry.GetOrCreateMetadata<TermStatusMeta>();
                        if(meta != null && meta.TranslationHash == locTermData.englishEntry.Value.Trim().GetHashCode()) {
                            meta.TranslationHash = newTranslation.Trim().GetHashCode();
                        }
                        
                        // set new value
                        LocalizationUtils.ChangeTextTranslation(locEntry.Key, newTranslation, locEntry.Table as StringTable);
                    }
                }
            }
            LocalizationTools.UpdateAllSources();
        }
    }
}