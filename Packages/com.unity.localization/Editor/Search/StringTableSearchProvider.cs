#if ENABLE_SEARCH

using System;
using System.Collections.Generic;
using UnityEditor.Localization.Bridge;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.Search
{
    class StringTableSearchProvider : TableSearchProvider<StringTableEntry>
    {
        public StringTableSearchProvider() :
            base(FilterIds.StringTableProvider, "String Table")
        {
            QueryEngine.SetSearchDataCallback(GetSearchableData, StringComparison.OrdinalIgnoreCase);
            AddTranslatedValueFilter(QueryEngine);
        }

        [SearchItemProvider]
        internal static SearchProvider CreateProvider() => new StringTableSearchProvider();

        static void AddTranslatedValueFilter(QueryEngine<TableEntrySearchData> queryEngine)
        {
            queryEngine.AddFilter(FilterIds.TranslatedValue, (TableEntrySearchData d, string filterNameMatch, string operatorToken, string filterValue) =>
            {
                var locale = string.IsNullOrEmpty(filterNameMatch) ? null : LocalizationEditorSettings.GetLocale(filterNameMatch);

                // Check all values
                if (locale == null)
                {
                    foreach (var st in ((StringTableCollection)d.Collection).StringTables)
                    {
                        var entry = st.GetEntry(d.Entry.Id);
                        var entryValue = entry != null ? entry.LocalizedValue : string.Empty;
                        if (IsStringMatch(entryValue, filterValue, operatorToken))
                            return true;
                    }
                }
                else
                {
                    var table = d.Collection.GetTable(locale.Identifier);
                    if (table is StringTable st)
                    {
                        var entry = st.GetEntry(d.Entry.Id);
                        var entryValue = entry != null ? entry.LocalizedValue : string.Empty;
                        if (IsStringMatch(entryValue, filterValue, operatorToken))
                            return true;
                    }
                }

                return false;
            });
        }

        protected override IEnumerable<SearchColumn> CreateColumns(SearchContext context, IEnumerable<SearchItem> searchDatas)
        {
            foreach (var c in base.CreateColumns(context, searchDatas))
            {
                yield return c;
            }

            foreach (var locale in LocalizationEditorSettings.GetLocales())
            {
                var col = SearchBridge.CreateColumn($"Localized Text/{locale.LocaleName}", locale.Identifier.Code, null, new GUIContent(locale.LocaleName, EditorIcons.Locale));
                col.getter = SelectTranslatedStringEntry;
                yield return col;
            }
        }

        // Method is serialized into the column so must be static and not renamed.
        static object SelectTranslatedStringEntry(SearchColumnEventArgs args)
        {
            if (!(args.item.data is TableEntrySearchData e))
                return null;

            if (e.Collection.GetTable(args.column.selector) is StringTable st && st.GetEntry(e.Entry.Id) is StringTableEntry ste)
            {
                return ste.LocalizedValue;
            }
            return null;
        }

        protected override SearchItem CreateSearchItem(SearchContext sc, SearchProvider provider, TableEntrySearchData data)
        {
            var item = provider.CreateItem(sc, $"{data.Collection.TableCollectionName}/{data.Entry.Key}", data.TableIndex, data.Entry.Key, data.Collection.TableCollectionName, EditorIcons.StringTableCollection, data);
            item.options = SearchItemOptions.Ellipsis | SearchItemOptions.FullDescription | SearchItemOptions.FuzzyHighlight;
            return item;
        }

        protected override IEnumerable<TableEntrySearchData> GetSearchData()
        {
            foreach (var collection in LocalizationEditorSettings.GetStringTableCollections())
            {
                for (int i = 0; i < collection.SharedData.Entries.Count; ++i)
                {
                    var entry = collection.SharedData.Entries[i];
                    yield return new TableEntrySearchData { TableIndex = i, Collection = collection, Entry = entry };
                }
            }
        }

        static IEnumerable<string> GetSearchableData(TableEntrySearchData item)
        {
            yield return item.Collection.TableCollectionName;
            yield return item.Entry.Key;

            foreach (var st in ((StringTableCollection)item.Collection).StringTables)
            {
                var tableEntry = st.GetEntry(item.Entry.Id);
                if (tableEntry != null)
                {
                    yield return tableEntry.LocalizedValue;
                }
                yield return string.Empty;
            }
        }

        #if ENABLE_SEARCH_QUERY_BUILDER
        protected override IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options)
        {
            // Only show propositions when our provider is being used, not for general searches.
            if (!options.flags.HasAny(SearchPropositionFlags.QueryBuilder) || context.filterId != filterId)
                yield break;

            foreach (var prop in base.FetchPropositions(context, options))
            {
                yield return prop;
            }

            yield return new SearchProposition("Translated", "Any", $"{FilterIds.TranslatedValue}:\"some value\"", "Filter by Table localized value.");
            foreach (var locale in LocalizationEditorSettings.GetLocales())
            {
                yield return new SearchProposition("Translated", locale.LocaleName, $"{FilterIds.TranslatedValue}({locale.Identifier.Code}):\"some value\"", $"Filter by {locale.LocaleName} Table localized value.", icon: EditorIcons.Locale);
            }
        }

        #endif
    }
}

#endif
