using System;
using UnityEngine.Localization;

namespace Awaken.PackageUtilities.CommonInterfaces {
    public interface ILocalizationManager {
        static ILocalizationManager Current { get; set; }
        
        string Translate(string id);
        string Translate(LocalizationEntryId id);
        bool HasSmartFormatTag(string id);
        bool HasSmartFormatTag(LocalizationEntryId id);
        void GetTranslationAndSmartFormatTag(string id, out string translation, out bool smartFormatTag);
        void GetTranslationAndSmartFormatTag(LocalizationEntryId id, out string translation, out bool smartFormatTag);
        string GetGesture(string id);
        string GetGesture(LocalizationEntryId id);
        void Initialize();
        void SwitchLanguage(in LocaleIdentifier locale);
        void Dispose();
    }
    
    public readonly struct LocalizationEntryId : IEquatable<LocalizationEntryId> {
        readonly uint _id;

        public bool IsValid => _id != 0;
        public uint Index => _id - 1;

        public LocalizationEntryId(uint index) {
            _id = index + 1;
        }

        public bool Equals(LocalizationEntryId other) {
            return _id == other._id;
        }

        public override bool Equals(object obj) {
            return obj is LocalizationEntryId other && Equals(other);
        }

        public override int GetHashCode() {
            return (int)_id;
        }

        public static bool operator ==(LocalizationEntryId left, LocalizationEntryId right) {
            return left.Equals(right);
        }

        public static bool operator !=(LocalizationEntryId left, LocalizationEntryId right) {
            return !left.Equals(right);
        }
    }
}