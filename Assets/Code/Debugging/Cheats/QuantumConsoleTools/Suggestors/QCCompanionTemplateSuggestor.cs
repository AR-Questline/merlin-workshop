using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.Mounts;
using Awaken.TG.Main.Locations.Pets;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using JetBrains.Annotations;
using QFSW.QC;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools.Suggestors {
    public sealed class CompanionSuggestorTag : IQcSuggestorTag { }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class CompanionNameAttribute : SuggestorTagAttribute {
        static readonly IQcSuggestorTag[] Tags = { new CompanionSuggestorTag() };

        public override IQcSuggestorTag[] GetSuggestorTags() {
            return Tags;
        }
    }

    [UsedImplicitly, UnityEngine.Scripting.Preserve]
    public class QCCompanionTemplateSuggestor : BasicCachedQcSuggestor<string> {
        static List<string> s_npcs;

        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options) {
            return context.HasTag<CompanionSuggestorTag>();
        }

        protected override IQcSuggestion ItemToSuggestion(string item) {
            return new SimplifiedSuggestion(item, true, null, "Spec");
        }

        protected override IEnumerable<string> GetItems(SuggestionContext context, SuggestorOptions options) {
            s_npcs ??= World.Services.Get<TemplatesProvider>()
                .GetAllOfType<LocationTemplate>()
                .Where(t =>
                    t.gameObject.GetComponent<MountAttachment>() ||
                    t.gameObject.GetComponent<PetAttachment>()
                )
                .Select(t => t.name)
                .ToList();
            return s_npcs;
        }
    }
}