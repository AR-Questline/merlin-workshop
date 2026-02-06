using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using QFSW.QC;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools.Suggestors {
    public sealed class QCTeamManagerFlagSuggestorTag : IQcSuggestorTag { }
    
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class TeamManagerFlagAttribute : SuggestorTagAttribute {
        static readonly IQcSuggestorTag[] Tags = {new QCTeamManagerFlagSuggestorTag()};

        public override IQcSuggestorTag[] GetSuggestorTags() {
            return Tags;
        }
    }
    
    [UsedImplicitly, UnityEngine.Scripting.Preserve]
    public class QCTeamManagerFlagSuggestor : BasicCachedQcSuggestor<string> {
        static readonly string[] FlagNames = {
            "Valid",
            "Enable",
            "Reset",
            "TimeReset",
            "Suspend",
            "Running",
            "Synchronization",
            "StepRunning",
            "Exit",
            "KeepTeleport",
            "InertiaShift",
            "CullingInvisible",
            "CullingKeep",
            "Spring",
            "SkipWriting",
            "Anchor",
            "AnchorReset",
            "NegativeScale"
        };
        
        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options) {
            return context.HasTag<QCTeamManagerFlagSuggestorTag>();
        }

        protected override IQcSuggestion ItemToSuggestion(string item) {
            return new SimplifiedSuggestion(item);
        }

        protected override IEnumerable<string> GetItems(SuggestionContext context, SuggestorOptions options) {
            return FlagNames;
        }
    }
}

