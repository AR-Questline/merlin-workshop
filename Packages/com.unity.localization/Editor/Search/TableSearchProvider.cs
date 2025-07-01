#if ENABLE_SEARCH || PACKAGE_DOCS_GENERATION

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Localization.Bridge;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.Search
{
    /// <summary>
    /// Search item data when using a String Table or Asset Table search provider.
    /// </summary>
    /// <example>
    /// This shows how to extract the search data after performing a search for string and asset tables.
    /// <code source="../../DocCodeSamples.Tests/SearchSamples.cs" region="sample"/>
    /// </example>
    public class TableEntrySearchData
    {
        internal int TableIndex { get; set; }

        /// <summary>
        /// The resulting <see cref="StringTableCollection"/> or <see cref="AssetTableCollection"/> for this search item.
        /// </summary>
        public LocalizationTableCollection Collection { get; internal set; }

        /// <summary>
        /// The resulting table entry for this search item.
        /// </summary>
        public SharedTableData.SharedTableEntry Entry { get; internal set; }

        public override string ToString() => $"{Collection.TableCollectionName} - {Entry.Key}";
    }

    abstract class TableSearchProvider<TEntry> : SearchProvider where TEntry : TableEntry
    {
        static Dictionary<Type, Dictionary<string, object>> s_MetdataPropertyCache = new Dictionary<Type, Dictionary<string, object>>();

        protected QueryEngine<TableEntrySearchData> QueryEngine { get; } = new QueryEngine<TableEntrySearchData>();

        public TableSearchProvider(string id, string displayName) :
            base(id, displayName)
        {
            trackSelection = TrackSelection;
            fetchColumns = CreateColumns;
            fetchItems = FetchItems;
            AddFilters(QueryEngine);
            showDetails = true;
            fetchDescription = FetchDescription;

            #if ENABLE_SEARCH_QUERY_BUILDER
            fetchPropositions = FetchPropositions;
            #endif
        }

        static string FetchDescription(SearchItem si, SearchContext sc)
        {
            // Show more info when in the minimum zoom level (LOC-941)
            if (sc.searchView.itemIconSize == 0)
                return $"{si.description}/{si.label}";
            return si.description;
        }

        static void TrackSelection(SearchItem si, SearchContext sc)
        {
            // Track selection needs to be enabled in settings
            if (si.data is TableEntrySearchData td && td.Collection != null)
                EditorGUIUtility.PingObject(td.Collection);
        }

        static void AddFilters(QueryEngine<TableEntrySearchData> queryEngine)
        {
            // Collection name
            queryEngine.AddFilter(FilterIds.CollectionName, d => d.Collection.TableCollectionName);

            // Collection group
            queryEngine.AddFilter(FilterIds.CollectionGroup, d => d.Collection.Group);

            // Key name
            queryEngine.AddFilter(FilterIds.KeyName, d => d.Entry.Key);

            // Key Id
            queryEngine.AddFilter(FilterIds.KeyId, d => d.Entry.Id);

            // Metadata
            AddMetadataTypeFilter(queryEngine);
            AddMetadataValueFilter(queryEngine);
        }

        static void AddMetadataTypeFilter(QueryEngine<TableEntrySearchData> queryEngine)
        {
            queryEngine.AddFilter(FilterIds.MetadataType, d => d); // Meta type
            queryEngine.TryGetFilter(FilterIds.MetadataType, out var metaTypeFilter); // GetFilter does not exist in 2021.2
            metaTypeFilter.AddOperator(":").AddHandler((TableEntrySearchData d, string value)
                => CompareMetadataToString(d, value, (a, b) =>
                {
                    long score = 0;

                    // Fuzzy expects the pattern to be lowercase.
                    return FuzzySearch.FuzzyMatch(b.ToLowerInvariant(), a, ref score);
                }));

            metaTypeFilter.AddOperator("=").AddHandler((TableEntrySearchData d, string value)
                => CompareMetadataToString(d, value, (a, b) => a.Equals(b, StringComparison.OrdinalIgnoreCase)));
        }

        static bool CompareMetadataToString(TableEntrySearchData d, string value, Func<string, string, bool> compare)
        {
            // Shared metadata
            foreach (var md in d.Entry.Metadata.MetadataEntries)
            {
                if (compare(md.GetType().Name, value))
                    return true;
            }

            // Entry metadata
            foreach (var tbl in d.Collection.GetTableEnumerator())
            {
                var t = tbl as DetailedLocalizationTable<TEntry>;
                var entry = t.GetEntry(d.Entry.Id);
                if (entry != null)
                {
                    foreach (var md in entry.MetadataEntries)
                    {
                        if (compare(md.GetType().Name, value))
                            return true;
                    }
                }
            }

            return false;
        }

        static void AddMetadataValueFilter(QueryEngine<TableEntrySearchData> queryEngine)
        {
            queryEngine.AddFilter(FilterIds.MetadataValue, (TableEntrySearchData d, string filterNameMatch, string operatorToken, string filterValue) =>
            {
                // Shared metadata
                foreach (var md in d.Entry.Metadata.MetadataEntries)
                {
                    if (GetValueThroughReflection(md, filterNameMatch, out object value) && IsMatch(value, filterValue, operatorToken))
                        return true;
                }

                // Entry metadata
                foreach (var tbl in d.Collection.GetTableEnumerator())
                {
                    var t = tbl as DetailedLocalizationTable<TEntry>;
                    var entry = t.GetEntry(d.Entry.Id);
                    if (entry != null)
                    {
                        foreach (var md in entry.MetadataEntries)
                        {
                            if (GetValueThroughReflection(md, filterNameMatch, out object value) && IsMatch(value, filterValue, operatorToken))
                                return true;
                        }
                    }
                }

                return false;
            });
        }

        static bool GetValueThroughReflection(IMetadata md, string fieldName, out object value)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                value = md.ToString();
                return true;
            }

            if (!s_MetdataPropertyCache.TryGetValue(md.GetType(), out var typeDict))
            {
                typeDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                s_MetdataPropertyCache[md.GetType()] = typeDict;

                foreach (var property in md.GetType().GetRuntimeProperties())
                {
                    typeDict[property.Name] = property;
                }

                foreach (var field in md.GetType().GetRuntimeFields())
                {
                    typeDict[field.Name] = field;
                }
            }

            if (typeDict.TryGetValue(fieldName, out var getter))
            {
                if (getter is PropertyInfo pi)
                {
                    value = pi.GetValue(md);
                    return true;
                }
                else
                {
                    value = ((FieldInfo)getter).GetValue(md);
                    return true;
                }
            }

            value = null;
            return false;
        }

        protected static bool IsStringMatch(string entryValue, string filterValue, string operatorToken)
        {
            if (operatorToken == ":")
            {
                long score = 0;
                return FuzzySearch.FuzzyMatch(filterValue.ToLowerInvariant(), entryValue, ref score);
            }

            var result = string.CompareOrdinal(filterValue, entryValue);
            switch (operatorToken)
            {
                case "=": return result == 0;
                case ">": return result > 0;
                case "<": return result < 0;
                case ">=": return result >= 0;
                case "<=": return result <= 0;
            }
            return false;
        }

        internal static bool IsMatch(object metaValue, string filterValue, string operatorToken)
        {
            if (operatorToken == ":")
            {
                long score = 0;
                var metaValueString = metaValue == null ? "null" : metaValue.ToString();
                return FuzzySearch.FuzzyMatch(filterValue.ToLowerInvariant(), metaValueString, ref score);
            }

            if (metaValue is IComparable comparable)
            {
                object converted;
                try
                {
                    converted = ConvertStringToObject(metaValue.GetType(), filterValue);
                }
                catch
                {
                    // Ignore convert errors
                    converted = null;
                }

                var result = comparable.CompareTo(converted);

                switch (operatorToken)
                {
                    case "=": return result == 0;
                    case ">": return result > 0;
                    case "<": return result < 0;
                    case ">=": return result >= 0;
                    case "<=": return result <= 0;
                }
            }
            return false;
        }

        static object ConvertStringToObject(Type type, string value)
        {
            if (type.IsEnum && Enum.TryParse(type, value, true, out var result))
                return result;
            return Convert.ChangeType(value, type);
        }

        protected virtual IEnumerable<SearchColumn> CreateColumns(SearchContext context, IEnumerable<SearchItem> searchDatas)
        {
            var keyCol = SearchBridge.CreateColumn("Entry/Key", null, null, new GUIContent("Key"));
            keyCol.getter = ColumnSelectors.SelectTableEntry;
            yield return keyCol;

            var idCol = SearchBridge.CreateColumn("Entry/Id", null, null, new GUIContent("Key Id"));
            idCol.getter = ColumnSelectors.SelectTableEntryId;
            yield return idCol;

            var colName = SearchBridge.CreateColumn("Collection Name", null, null, new GUIContent("Collection Name"));
            colName.getter = ColumnSelectors.SelectTableCollection;
            yield return colName;

            var colGroup = SearchBridge.CreateColumn("Collection Group", null, null, new GUIContent("Collection Group"));
            colGroup.getter = ColumnSelectors.SelectTableCollectionGroup;
            yield return colGroup;
        }

        IEnumerator FetchItems(SearchContext sc, List<SearchItem> items, SearchProvider provider)
        {
            // Only show results when our provider is being used when there are multiple providers.
            if (sc.providers.Count() != 1 && sc.filterId != filterId)
                yield break;

            var query =
            #if UNITY_2022_2_OR_NEWER
            QueryEngine.ParseQuery(sc.searchQuery);
            #else
            QueryEngine.Parse(sc.searchQuery);
            #endif
            if (!query.valid)
                yield break;

            var filteredObjects = query.Apply(GetSearchData());
            foreach (var fo in filteredObjects)
            {
                yield return CreateSearchItem(sc, provider, fo);
            }
        }

        protected abstract SearchItem CreateSearchItem(SearchContext sc, SearchProvider provider, TableEntrySearchData data);

        protected abstract IEnumerable<TableEntrySearchData> GetSearchData();

        #if ENABLE_SEARCH_QUERY_BUILDER
        protected virtual IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options)
        {
            // Only show propositions when our provider is being used, not for general searches.
            if (!options.flags.HasAny(SearchPropositionFlags.QueryBuilder) || context.filterId != filterId)
                yield break;

            yield return new SearchProposition(category: "Entry", "Key", $"{FilterIds.KeyName}:\"My Entry\"", "Filter by Table Entry Key.");
            yield return new SearchProposition(category: "Entry", "Id", $"{FilterIds.KeyId}=12345", "Filter by Table Entry Id.");

            var nameBlock = new CollectionNameFilterBlock(null, FilterIds.CollectionName, null, new QueryListBlockAttribute("Collection", "Collection Name", FilterIds.CollectionName, ":"));
            foreach (var t in nameBlock.GetPropositions())
            {
                yield return t;
            }

            var groupBlock = new CollectionGroupFilterBlock(null, FilterIds.CollectionGroup, null, new QueryListBlockAttribute("Group", "Collection Group", FilterIds.CollectionGroup, ":"));
            foreach (var t in groupBlock.GetPropositions())
            {
                yield return t;
            }

            var metaTypeBlock = new MetadataTypeFilterBlock(null, FilterIds.MetadataType, null, new QueryListBlockAttribute("Metadata", "Metadata Type", FilterIds.MetadataType, ":"));
            foreach (var t in metaTypeBlock.GetPropositions())
            {
                yield return t;
            }

            var metaValueBlock = new MetadataValueFilterBlock(null, FilterIds.MetadataValue, null, new QueryListBlockAttribute("Metadata", "Metadata Value", FilterIds.MetadataValue));
            foreach (var t in metaValueBlock.GetPropositions())
            {
                yield return t;
            }
        }

        #endif
    }
}

#endif
