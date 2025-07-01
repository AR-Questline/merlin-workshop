using System;
using System.Text.RegularExpressions;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Extensions;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Awaken.TG.Main.Localization {
    [Serializable]
    public partial class LocString {
        public ushort TypeForSerialization => SavedTypes.LocString;

        [Saved] public string IdOverride = string.Empty;
        [Saved] public string ID = string.Empty;
        [Saved] public string Fallback { get; private set; }

        public void SetFallback(string fallbackText, bool overrideExisting = false) {
            if (overrideExisting || Fallback.IsNullOrWhitespace()) {
                Fallback = fallbackText;
            }
        }

        public string Translate() {
            return ToString();
        }
        
        public string Translate(params object[] parameters) {
            string translation = ToString();
            if (translation != null) {
                translation = RichTextUtil.SmartFormat(translation, parameters);
                return Regex.Unescape(translation);
            }
            return translation;
        }
        
        public static implicit operator string(LocString s) {
            return s != null ? s.ToString() : string.Empty;
        }

        public static explicit operator LocString(string fallback) {
            return new LocString {Fallback = fallback};
        }

        public override string ToString() {
            Locale locale = LocalizationHelper.SelectedLocale;
            string id = !string.IsNullOrWhiteSpace(IdOverride) ? IdOverride : ID;
            string translation = GetTranslation(id, locale);
            
            return translation.IsNullOrWhitespace() 
                ? Fallback ?? string.Empty
                : translation;
        }

        public T GetMetadata<T>(Locale locale) where T : class, IMetadata {
            locale ??= LocalizationHelper.SelectedLocale;
            string id = !string.IsNullOrWhiteSpace(IdOverride) ? IdOverride : ID;
            StringTableEntry entry = LocalizationHelper.GetTableEntry(id, locale);
            return entry?.GetMetadata<T>();
        }
        
        public T GetSharedMetadata<T>() where T : class, IMetadata {
            string id = !string.IsNullOrWhiteSpace(IdOverride) ? IdOverride : ID;
            var sharedEntry = LocalizationHelper.GetTableEntry(id).entry?.SharedEntry;
            return sharedEntry?.Metadata.GetMetadata<T>();
        }

        static string GetTranslation(string id, Locale locale) => LocalizationHelper.Translate(id, locale);
    }
}

