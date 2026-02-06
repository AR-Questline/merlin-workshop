using System;
using System.Text.RegularExpressions;
using Awaken.PackageUtilities.CommonInterfaces;
using Awaken.TG.Utility;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Awaken.Utility.Extensions;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Tables;

namespace Awaken.TG.Main.Localization {
    [Serializable]
    public partial class LocString {
        public ushort TypeForSerialization => SavedTypes.LocString;

        [Saved] public string IdOverride = string.Empty;
        [Saved] public string ID = string.Empty;
        [Saved] public string Fallback { get; private set; }

        public string FinalId => IdOverride.IsNullOrWhitespace() ? ID : IdOverride;

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
            string translation = GetTranslation(FinalId);
            
            return translation.IsNullOrWhitespace() 
                ? Fallback ?? string.Empty
                : translation;
        }

        static string GetTranslation(string id) => LocalizationHelper.Translate(id);
    }

    [Serializable]
    public partial struct LightLocString : IEquatable<LightLocString> {
        public ushort TypeForSerialization => SavedTypes.LightLocString;

        [Saved] public LocalizationEntryId id;

        public LightLocString(LocalizationEntryId id) {
            this.id = id;
        }

        public static implicit operator string(LightLocString s) {
            return s.ToString();
        }

        public override string ToString() {
            string translation = LocalizationHelper.Translate(id);
            return translation.IsNullOrWhitespace() ? string.Empty : translation;
        }

        public string GetGestureMetadata() {
            var sharedEntry = LocalizationHelper.GetGestureMetadata(id);
            return sharedEntry.IsNullOrWhitespace() ? string.Empty : sharedEntry;
        }

        public bool Equals(LightLocString other) {
            return id.Equals(other.id);
        }

        public override bool Equals(object obj) {
            return obj is LightLocString other && Equals(other);
        }

        public override int GetHashCode() {
            return id.GetHashCode();
        }

        public static bool operator ==(LightLocString left, LightLocString right) {
            return left.Equals(right);
        }

        public static bool operator !=(LightLocString left, LightLocString right) {
            return !left.Equals(right);
        }
    }
}

