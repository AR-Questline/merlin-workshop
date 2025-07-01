using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Fights.Mounts;
using Awaken.TG.Main.Fights.NPCs;
using Awaken.TG.Main.Locations.Pets;
using Awaken.TG.Main.Locations.Setup;
using Awaken.TG.Main.Templates;
using Awaken.TG.MVC;
using JetBrains.Annotations;
using QFSW.QC;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools.Suggestors {
    public sealed class NPCSuggestorTag : IQcSuggestorTag { }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class NPCNameAttribute : SuggestorTagAttribute {
        static readonly IQcSuggestorTag[] Tags = {new NPCSuggestorTag()};

        public override IQcSuggestorTag[] GetSuggestorTags() {
            return Tags;
        }
    }

    [UsedImplicitly, UnityEngine.Scripting.Preserve]
    public class QCNPCTemplateSuggestor : BasicCachedQcSuggestor<string> {
        static List<string> s_npcs;

        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options) {
            return context.HasTag<NPCSuggestorTag>();
        }

        protected override IQcSuggestion ItemToSuggestion(string item) {
            return new SimplifiedSuggestion(item, true, null, "Spec");
        }

        protected override IEnumerable<string> GetItems(SuggestionContext context, SuggestorOptions options) {
            s_npcs ??= World.Services.Get<TemplatesProvider>()
                           .GetAllOfType<LocationTemplate>()
                           .Where(t => t.gameObject.GetComponent<NpcAttachment>())
                           .Select(t => t.name)
                           .ToList();
            return s_npcs;
        }
    }
    
    public sealed class UNPCSuggestorTag : IQcSuggestorTag { }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class UniqueNPCNameAttribute : SuggestorTagAttribute {
        static readonly IQcSuggestorTag[] Tags = {new UNPCSuggestorTag()};

        public override IQcSuggestorTag[] GetSuggestorTags() {
            return Tags;
        }
    }

    [UsedImplicitly, UnityEngine.Scripting.Preserve]
    public class QCUNPCTemplateSuggestor : BasicCachedQcSuggestor<string> {
        static List<string> s_npcs;

        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options) {
            return context.HasTag<UNPCSuggestorTag>();
        }

        protected override IQcSuggestion ItemToSuggestion(string item) {
            return new SimplifiedSuggestion(item, true, null, "Spec");
        }

        protected override IEnumerable<string> GetItems(SuggestionContext context, SuggestorOptions options) {
            s_npcs ??= World.Services.Get<TemplatesProvider>()
                           .GetAllOfType<LocationTemplate>()
                           .Where(t => t.gameObject.GetComponent<UniqueNpcAttachment>())
                           .Select(t => t.name)
                           .ToList();
            return s_npcs;
        }
    }
}