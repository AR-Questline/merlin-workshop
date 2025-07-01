using System;
using System.Collections.Generic;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Stories.Quests;
using JetBrains.Annotations;
using QFSW.QC;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools.Suggestors {
    public sealed class QuestObjectiveSuggestorTag : IQcSuggestorTag {
    }
    
    [AttributeUsage(AttributeTargets.Parameter)]
    public class QuestObjectiveSuggestionAttribute : SuggestorTagAttribute {
        static readonly IQcSuggestorTag[] SuggestorTags = { new QuestObjectiveSuggestorTag() };
        public override IQcSuggestorTag[] GetSuggestorTags() {
            return SuggestorTags;
        }
    }
    
    [UsedImplicitly, UnityEngine.Scripting.Preserve]
    public class QCQuestObjectiveSuggestor : IQcSuggestor {
        public IEnumerable<IQcSuggestion> GetSuggestions(SuggestionContext context, SuggestorOptions options) {
            if (context.TargetType != typeof(string) || !context.HasTag<QuestObjectiveSuggestorTag>()) {
                yield break;
            }
            var quest = Hero.Current.Element<QuestTracker>().ActiveQuest;
            if (quest == null) {
                yield break;
            }
            foreach (var objective in quest.Objectives) {
                yield return new SimplifiedSuggestion(objective.Name);
            }
        }
    }
}
