using System;
using System.Collections.Generic;
using Awaken.TG.Main.Localization;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Awaken.TG.Editor.Localizations {
    [Serializable]
    public class DataToExport {
        public LocTermData termData;
        public List<LocTermData> groupedTermsData;
        
        [ShowInInspector, DisplayAsString(false)]
        public string ID => termData.id;
        [ShowInInspector, DisplayAsString(false)]
        public string Source => LocalizationTools.TryGetTranslation(ID, LocalizationSettings.ProjectLocale);
        [SerializeField, HideInInspector] 
        public string originalSource;
        [SerializeField, HideInInspector]
        public string gesture;
        [SerializeField, HideInInspector]
        public string category;
        [SerializeField, HideInInspector]
        public string previousLine;
        [ReadOnly]
        public string actor;
        [MultiLineProperty, DisplayAsString(false)]
        public string translation;
        
        [VerticalGroup("Term Status")]
        public int translationHash;

        [ShowInInspector, DisplayAsString(false)]
        string Gesture => termData.englishEntry?.SharedEntry.Metadata.GetMetadata<GestureMetadata>()?.GestureKey ?? string.Empty;

        public bool IsTranslated => translationHash != 0;
        public bool IsDebug => termData.EntryFor(LocalizationSettings.ProjectLocale, false)?.SharedEntry?.Metadata?.GetMetadata<DebugMarkerMeta>() != null;
        public Locale CurrentLocale {
            get => LocalizationSettings.AvailableLocales.GetLocale(currentLocale);
            set => currentLocale = value != null ? value.Identifier : new LocaleIdentifier();
        }
        
        [SerializeField, HideInInspector] 
        LocaleIdentifier currentLocale;

        /// <summary>
        /// This constructor only exists for deserialization purposes and shouldn't be used in any other case.
        /// </summary>
        public DataToExport() {}
        
        public DataToExport(LocTermData termData, string actor = "", string previousLine = "") {
            this.termData = termData;
            this.actor = actor;
            this.originalSource = Source;
            this.previousLine = previousLine;
        }
            
        public DataToExport(DataToExport data) {
            this.termData = data.termData;
            this.groupedTermsData = data.groupedTermsData;
            this.actor = data.actor;
            this.gesture = data.gesture;
            this.category = data.category;
            this.previousLine = data.previousLine;
            this.translation = data.translation;
            this.translationHash = data.translationHash;
            this.originalSource = data.Source;
            SetLanguage(data.CurrentLocale);
        }
        
        public bool IsSourceRedacted() {
            var meta = termData.englishEntry?.GetMetadata<TermStatusMeta>();
            return Source.Trim().GetHashCode() == meta?.TranslationHash;
        } 
        
        public bool IsProofRead(Locale locale) { 
            var meta = termData.EntryFor(locale, false)?.GetMetadata<TermStatusMeta>();
            return meta?.ProofreadHash == Source?.Trim().GetHashCode();
        } 
        
        public void SetLanguage(Locale locale) {
            translationHash = 0;
            if (locale != null) {
                CurrentLocale = locale;
                var statusMeta = termData.EntryFor(locale, false)?.GetMetadata<TermStatusMeta>();
                translationHash = statusMeta?.TranslationHash ?? 0;
            }
        }

        public void SetGestureAndCategory() {
            gesture = Gesture;
            category = termData.englishEntry?.SharedEntry.Metadata.GetMetadata<CategoryMetadata>()?.CategoryText ?? string.Empty;
        }
        
        public void ApplyGesture(LocTermData t) {
            if (string.IsNullOrWhiteSpace(gesture)) {
                return;
            }

            var entry = t.englishEntry;
            if (entry == null) {
                return;
            }

            GestureMetadata d = entry.SharedEntry.Metadata.GetMetadata<GestureMetadata>();
            if (d == null) {
                d = new GestureMetadata();
                entry.SharedEntry.Metadata.AddMetadata(d);
            }
            d.GestureKey = gesture;
            EditorUtility.SetDirty(entry.Table.SharedData);
        }
    }
}