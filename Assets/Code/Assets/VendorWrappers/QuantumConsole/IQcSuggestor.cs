using System;
using System.Collections.Generic;

namespace QFSW.QC
{
    public interface IQcSuggestor
    {
        IEnumerable<IQcSuggestion> GetSuggestions(SuggestionContext context, SuggestorOptions options);
    }
    public interface IQcSuggestion
    {
        string FullSignature { get; }
        string PrimarySignature { get; }
        string SecondarySignature { get; }

        bool MatchesPrompt(string prompt);
        string GetCompletion(string prompt);
        string GetCompletionTail(string prompt);

        SuggestionContext? GetInnerSuggestionContext(SuggestionContext context);
    }
    
    public struct SuggestionContext
    {
        public int Depth;
        public string Prompt;
        public Type TargetType;
        public IQcSuggestorTag[] Tags;
        
        public bool HasTag<T>() where T : IQcSuggestorTag
        {
            return false;
        }

        public T GetTag<T>() where T : IQcSuggestorTag
        {
            return default;
        }

        public IEnumerable<T> GetTags<T>() where T : IQcSuggestorTag
        {
            return null;
        }
    }
    
    public struct SuggestorOptions
    {
        public bool CaseSensitive;
        public bool Fuzzy;
        public bool CollapseOverloads;
    }
}