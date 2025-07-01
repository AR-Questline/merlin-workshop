namespace QFSW.QC
{
    public class RawSuggestion : IQcSuggestion
    {
        public string FullSignature { get; }
        public string PrimarySignature { get; }
        public string SecondarySignature => string.Empty;

        public RawSuggestion(string value, bool singleLiteral = false) { }

        public bool MatchesPrompt(string prompt)
        {
            return false;
        }

        public string GetCompletion(string prompt)
        {
            return string.Empty;
        }

        public string GetCompletionTail(string prompt)
        {
            return string.Empty;
        }

        public SuggestionContext? GetInnerSuggestionContext(SuggestionContext context)
        {
            return null;
        }
    }
}