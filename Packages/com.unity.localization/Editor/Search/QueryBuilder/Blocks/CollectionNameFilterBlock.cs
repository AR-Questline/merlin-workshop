#if ENABLE_SEARCH_QUERY_BUILDER

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Search;

namespace UnityEditor.Localization.Search
{
    [QueryListBlock(null, "Collection Name", FilterIds.CollectionName, ":")]
    class CollectionNameFilterBlock : QueryListBlock
    {
        LocalizationTableCollection m_Collection;

        bool IsStringTable => source?.context?.filterId == "at:" ? false : true;

        public CollectionNameFilterBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr) :
            base(source, id, value, attr)
        {
            SetCollection(GetCollection(value));
        }

        void SetCollection(LocalizationTableCollection collection)
        {
            m_Collection = collection;
            if (m_Collection != null)
            {
                alwaysDrawLabel = false;
                label = m_Collection.TableCollectionName;
                value = GetQuotedValue(label);
                icon = EditorGUIUtility.GetIconForObject(collection);
            }
            else
            {
                icon = null;
                alwaysDrawLabel = true;
            }
        }

        /// <summary>
        /// Encapsulates text in quotes if it contains any whitespace.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        static string GetQuotedValue(string name) => name.Any(char.IsWhiteSpace) ? $"\"{name}\"" : name;

        LocalizationTableCollection GetCollection(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            // Remove quotes
            name = name.Replace("\"", "");

            var collections = IsStringTable ? LocalizationEditorSettings.GetStringTableCollections().Cast<LocalizationTableCollection>() : LocalizationEditorSettings.GetAssetTableCollections().Cast<LocalizationTableCollection>();
            foreach (var col in collections)
            {
                if (col.TableCollectionName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return col;
            }
            return null;
        }

        public override void Apply(in SearchProposition searchProposition)
        {
            if (searchProposition.data is LocalizationTableCollection col)
            {
                SetCollection(col);
                source.Apply();
            }
        }

        public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags = SearchPropositionFlags.None)
        {
            var collections = IsStringTable ? LocalizationEditorSettings.GetStringTableCollections().Cast<LocalizationTableCollection>() : LocalizationEditorSettings.GetAssetTableCollections().Cast<LocalizationTableCollection>();
            foreach (var col in collections)
            {
                var name = GetQuotedValue(col.TableCollectionName);
                yield return new SearchProposition(category, col.TableCollectionName, $"{id}={name}", string.Empty, 0, icon: EditorGUIUtility.GetIconForObject(col), data: col);
            }
        }
    }
}

#endif
