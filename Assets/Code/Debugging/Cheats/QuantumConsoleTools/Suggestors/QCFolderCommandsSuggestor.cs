using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using QFSW.QC;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools.Suggestors {
    public sealed class FolderCommandsSuggestorTag : IQcSuggestorTag { }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class FolderCommandsAttribute : SuggestorTagAttribute {
        static readonly IQcSuggestorTag[] Tags = {new FolderCommandsSuggestorTag()};

        public override IQcSuggestorTag[] GetSuggestorTags() {
            return Tags;
        }
    }

    [UsedImplicitly, UnityEngine.Scripting.Preserve]
    public class QCFolderCommandsSuggestor : BasicCachedQcSuggestor<string> {
        static string[] s_rootCommands;
        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options) {
            return context.HasTag<FolderCommandsSuggestorTag>();
        }

        protected override IQcSuggestion ItemToSuggestion(string item) {
            return new RawSuggestion(item, true);
        }

        protected override IEnumerable<string> GetItems(SuggestionContext context, SuggestorOptions options) {
            s_rootCommands ??= QCFolderCommands.AllFolders().ToArray();
            return s_rootCommands;
        }
    }
}