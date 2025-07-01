#if ENABLE_SEARCH_QUERY_BUILDER

using System.Collections.Generic;
using System.Linq;
using UnityEditor.Search;

namespace UnityEditor.Localization.Search
{
    [QueryListBlock(null, "Collection Group", FilterIds.CollectionGroup, ":")]
    class CollectionGroupFilterBlock : QueryListBlock
    {
        HashSet<string> m_GroupHash;

        bool IsStringTable => source?.context?.filterId == "at:" ? false : true;

        public CollectionGroupFilterBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr) :
            base(source, id, value, attr)
        {
        }

        /// <summary>
        /// Encapsulates text in quotes if it contains any whitespace.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        static string GetQuotedValue(string name) => name.Any(char.IsWhiteSpace) ? $"\"{name}\"" : name;

        public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags = SearchPropositionFlags.None)
        {
            if (m_GroupHash == null)
            {
                var collections = IsStringTable ? LocalizationEditorSettings.GetStringTableCollections().Cast<LocalizationTableCollection>() : LocalizationEditorSettings.GetAssetTableCollections().Cast<LocalizationTableCollection>();
                m_GroupHash = new HashSet<string>();

                foreach (var col in collections)
                {
                    m_GroupHash.Add(col.Group);
                }
            }

            foreach (var grp in m_GroupHash)
            {
                var name = GetQuotedValue(grp);
                yield return new SearchProposition(category, grp, $"{id}={name}", string.Empty, 0, data: grp);
            }
        }
    }
}

#endif
