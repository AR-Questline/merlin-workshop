using System.Collections.Generic;
using System.Linq;

namespace QFSW.QC
{
    public abstract class BasicCachedQcSuggestor<TItem> : IQcSuggestor
    {
        protected abstract bool CanProvideSuggestions(SuggestionContext context, SuggestorOptions options);
        protected abstract IQcSuggestion ItemToSuggestion(TItem item);
        protected abstract IEnumerable<TItem> GetItems(SuggestionContext context, SuggestorOptions options);

        protected virtual bool IsCompatible(SuggestionContext context, IQcSuggestion suggestion, SuggestorOptions options)
        {
            return false;
        }

        public IEnumerable<IQcSuggestion> GetSuggestions(SuggestionContext context, SuggestorOptions options)
        {
            return Enumerable.Empty<IQcSuggestion>();
        }
    }
}