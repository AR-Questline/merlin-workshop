using System;
using System.Collections.Generic;
using System.IO;
using Awaken.Utility.Sessions;
using JetBrains.Annotations;
using QFSW.QC;
using UnityEngine;

namespace Awaken.TG.Debugging.Cheats.QuantumConsoleTools.Suggestors {
    public sealed class QuestFlagSuggestorTag : IQcSuggestorTag { }
    
    [AttributeUsage(AttributeTargets.Parameter)]
    public class QuestFlagSuggestionAttribute : SuggestorTagAttribute {
        static readonly IQcSuggestorTag[] SuggestorTags = { new QuestFlagSuggestorTag() };
        public override IQcSuggestorTag[] GetSuggestorTags() {
            return SuggestorTags;
        }
    }
    
    [UsedImplicitly, UnityEngine.Scripting.Preserve]
    public class QCQuestFlagSuggestor : BasicCachedQcSuggestor<string> {
        static Cached<List<string>> s_questFlags = new(GetFlagsFromFile);
        
        protected override bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options) {
            return context.HasTag<QuestFlagSuggestorTag>() && context.TargetType == typeof(string);
        }
        protected override IQcSuggestion ItemToSuggestion(string item) {
            return new RawSuggestion(item);
        }
        protected override IEnumerable<string> GetItems(SuggestionContext context, SuggestorOptions options) {
            return s_questFlags.Get();
        }

        static List<string> GetFlagsFromFile() {
            const string FileName = "FlagSuggestionCache.txt"; // see ConsoleFlagCacheGeneration.cs
            string path = Path.Combine(Application.streamingAssetsPath, FileName);

            if (!File.Exists(path)) {
                Debug.LogError($"File not found: {path}");
                return new List<string>();
            }
            
            var newList = new List<string>(2_000);
            
            using (FileStream fs = new(path, FileMode.Open, FileAccess.Read)) {
                using (StreamReader reader = new(fs)) {
                    while (!reader.EndOfStream) {
                        newList.Add(reader.ReadLine());
                    }
                }
            }
            return newList;
        }
    }
}
