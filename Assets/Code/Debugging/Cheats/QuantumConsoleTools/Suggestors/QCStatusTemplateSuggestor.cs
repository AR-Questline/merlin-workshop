using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.Statuses;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using JetBrains.Annotations;
using QFSW.QC;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools.Suggestors {
    public sealed class StatusSuggestorTag : IQcSuggestorTag { }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class StatusNameAttribute : SuggestorTagAttribute {
        static readonly IQcSuggestorTag[] Tags = { new StatusSuggestorTag() };

        public override IQcSuggestorTag[] GetSuggestorTags() {
            return Tags;
        }
    }
    

    [UsedImplicitly, UnityEngine.Scripting.Preserve]
    public class QCStatusTemplateSuggestor : BasicCachedQcSuggestor<string> {
        static List<string> s_items;
        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options) {
            return context.HasTag<StatusSuggestorTag>();
        }

        protected override IQcSuggestion ItemToSuggestion(string item) {
            return new SimplifiedSuggestion(item, true, "Status_");
        }

        protected override IEnumerable<string> GetItems(SuggestionContext context, SuggestorOptions options) {
            s_items ??= World.Services.Get<TemplatesProvider>().GetAllOfType<StatusTemplate>().Select(t => t.name).ToList();
            return s_items;
        }
    }
}