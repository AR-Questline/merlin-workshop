using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Awaken.PackageUtilities.CommonInterfaces;
using Awaken.TG.Utility;
using Awaken.Utility.Debugging;
using Sirenix.Utilities;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Pseudo;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Awaken.Utility {
    /// <summary>
    /// Helper for getting translations from all available StringTables
    /// </summary>
    [Il2CppEagerStaticClassConstruction]
    public static class LocalizationHelper {
        public static Locale SelectedLocale {
            get {
#if UNITY_EDITOR
                return (LocalizationsTestMode.Enabled || Application.isPlaying ? LocalizationSettings.SelectedLocale : LocalizationSettings.ProjectLocale)
                       ?? LocalizationSettings.ProjectLocale;
#else
                return LocalizationSettings.SelectedLocale;
#endif
            }
        }

        public static CultureInfo SelectedCulture => SelectedLocale?.Identifier.CultureInfo;

        public static readonly string DefaultTable = "Prefabs";
        public static readonly string StoryTable = "Story";
        public static readonly string OverridesTable = "Overrides";
        public static readonly string TagsTable = "Tags";
        public static readonly string OldLoc = "OldLocalization";
        public static readonly string[] StringTables = {StoryTable, DefaultTable, OverridesTable, "LocTerms", "KeyBindings", TagsTable};

        static bool DebugTranslations => SafeEditorPrefs.GetBool("debugTranslations");
        
        public static bool IsNonLatinaCharacters(Locale localeToCheck = null) {
            var locale = localeToCheck ? localeToCheck : SelectedLocale;
            if (locale == null) {
                return false;
            }

            string code = locale.Identifier.Code;
            return code.Length >= 2 && (
                (code[0] == 'z' && code[1] == 'h') ||
                (code[0] == 'j' && code[1] == 'a') ||
                (code[0] == 'k' && code[1] == 'o') ||
                (code[0] == 'r' && code[1] == 'u')
            );
        }
        
        public static void ForceLoadTables() {
            foreach (string tableID in StringTables) {
                Log.Marking?.Warning($"Loading localization table {tableID}");
                LocalizationSettings.StringDatabase.GetTable(tableID, SelectedLocale);
            }
        }

        // --- Localization Helpers
        public static string EditorOnly_Translate(string id, Locale locale = null, bool ignoreSmartStrings = false) {
            if (string.IsNullOrWhiteSpace(id)) {
                return string.Empty;
            }
            locale ??= SelectedLocale;
        
            TableEntryResult tableEntryResult = EditorOnly_GetTableEntry(id, locale);
            return EditorOnly_Translate(id, locale, tableEntryResult, ignoreSmartStrings);
        }
        
        public static string EditorOnly_Translate(string id, Locale locale, TableEntryResult tableEntryResult, bool ignoreSmartStrings = false) {
            if (tableEntryResult.code != TableResultCode.Success) {
#if UNITY_EDITOR
                if (DebugTranslations) {
                    return tableEntryResult.code.ToString();
                }
#endif
                return string.Empty;
            }

            StringTableEntry tableEntry = tableEntryResult.entry;
            
            string translation;
            if (!ignoreSmartStrings) {
                try {
                    translation = tableEntry.GetLocalizedString(null, null, locale as PseudoLocale);
                } catch (Exception e) {
                    Log.Critical?.Error($"Invalid smart string for id {id}. Real exception below:");
                    Debug.LogException(e);
                    translation = tableEntry.Value;
                }
            } else {
                translation = tableEntry.Value;
            }

            return translation ?? string.Empty;
        }
        
        public static string Translate(string id, bool ignoreSmartStrings = false) {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                return EditorOnly_Translate(id, SelectedLocale, ignoreSmartStrings);
            }
 #endif
            if (ignoreSmartStrings) {
                return ILocalizationManager.Current.Translate(id);
            } else {
                ILocalizationManager.Current.GetTranslationAndSmartFormatTag(id, out var translation, out bool smartFormatTag);
                return smartFormatTag ? FormatSmartString(translation) : translation;
            }
        }
        
        public static string Translate(LocalizationEntryId id, bool ignoreSmartStrings = false) {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                Log.Important?.Error($"Translation with {nameof(LocalizationEntryId)} is not supported in edit mode!");
                return string.Empty;
            }
#endif
            if (ignoreSmartStrings) {
                return ILocalizationManager.Current.Translate(id);
            } else {
                ILocalizationManager.Current.GetTranslationAndSmartFormatTag(id, out var translation, out bool smartFormatTag);
                return smartFormatTag ? FormatSmartString(translation) : translation;
            }
        }
        
        public static string GetGestureMetadata(LocalizationEntryId id) {
#if UNITY_EDITOR 
            if (!Application.isPlaying) {
                Log.Important?.Error($"Getting Gesture metadata with {nameof(LocalizationEntryId)} is not supported in edit mode!");
                return string.Empty;
            }
#endif
            return ILocalizationManager.Current.GetGesture(id);
        }
        
       static string FormatSmartString(string value) {
           var formatProvider = SelectedLocale.Formatter;
           var translatedText = LocalizationSettings.StringDatabase.SmartFormatter.Format(formatProvider, value, null);
           return translatedText;
       }
        
        public static TableEntryResult EditorOnly_GetTableEntry(string id, Locale locale = null) {
            if (string.IsNullOrWhiteSpace(id)) {
                return TableEntryResult.Failure(TableResultCode.WrongId);
            }

            locale ??= SelectedLocale;

            FallbackBehavior fallbackBehavior = GetFallbackBehaviour();
            StringTableEntry tableEntry = null;
            int i = 0;
            
            while (tableEntry == null && i < StringTables.Length) {
                tableEntry = LocalizationSettings.StringDatabase.GetTableEntryARRealSync(StringTables[i], id, locale, fallbackBehavior).Entry;
                i++;
            }

            if (tableEntry != null && !tableEntry.Value.IsNullOrWhitespace()) {
                return TableEntryResult.Success(tableEntry);
            }
            
#if AR_DEBUG || DEBUG
            if (DebugTranslations) {
                Log.Minor?.Info("No Translation Exists for " + id);
                return TableEntryResult.Failure(TableResultCode.Missing);
            }

            tableEntry = LocalizationSettings.StringDatabase.GetTableEntryARRealSync(OldLoc, id, locale, fallbackBehavior).Entry;
            if (tableEntry != null && !tableEntry.Value.IsNullOrWhitespace()) {
                if (Application.isPlaying) {
                    Log.Important?.Error("Found Translation in OldLoc for " + id + "\n" + tableEntry.Value);
                }

                return TableEntryResult.Failure(TableResultCode.FoundInOld);
            }
#endif
            return TableEntryResult.Failure(TableResultCode.OtherFailure);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static FallbackBehavior GetFallbackBehaviour() {
#if UNITY_EDITOR
            if (DebugTranslations) {
                return FallbackBehavior.DontUseFallback;
            }
#endif
            if (PlatformUtils.IsConsole) {
                return FallbackBehavior.DontUseFallback;
            } else {
                return FallbackBehavior.UseProjectSettings;
            }
        }

        public struct TableEntryResult {
            public StringTableEntry entry;
            public TableResultCode code;
            
            public static TableEntryResult Success(StringTableEntry entry) {
                return new TableEntryResult {entry = entry, code = TableResultCode.Success};
            }
            
            public static TableEntryResult Failure(TableResultCode code) {
                return new TableEntryResult { entry = null, code = code };
            }
            
            public static implicit operator StringTableEntry(TableEntryResult result) => result.entry;
        }

        public enum TableResultCode : byte {
            Success = 0,
            WrongId = 1,
            Missing = 2,
            FoundInOld = 3,
            OtherFailure = 4,
        }
    }
}
