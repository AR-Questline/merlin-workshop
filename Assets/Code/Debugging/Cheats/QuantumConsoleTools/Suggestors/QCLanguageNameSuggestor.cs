using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QFSW.QC;
using UnityEngine.Localization.Settings;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools.Suggestors {
    public sealed class LanguageNameSuggestorTag : IQcSuggestorTag { }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class LanguageNameAttribute : SuggestorTagAttribute {
        static readonly IQcSuggestorTag[] Tags = {new LanguageNameSuggestorTag()};

        public override IQcSuggestorTag[] GetSuggestorTags() {
            return Tags;
        }
    }

    [UsedImplicitly, UnityEngine.Scripting.Preserve]
    public class QCLanguageNameSuggestor : BasicCachedQcSuggestor<string> {
        static List<string> s_languages;

        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options) {
            return context.HasTag<LanguageNameSuggestorTag>();
        }

        protected override IQcSuggestion ItemToSuggestion(string item) {
            return new SimplifiedSuggestion(item, true, null, "Language");
        }

        protected override IEnumerable<string> GetItems(SuggestionContext context, SuggestorOptions options) {
            s_languages ??= LocalizationSettings.AvailableLocales.Locales
                                               .Select(l => l.Identifier.Code)
                                               .ToList();
            return s_languages;
        }
    }
}