#if ENABLE_SEARCH

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Localization.Bridge;
using UnityEditor.Search;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization.Search
{
    class AssetTableSearchProvider : TableSearchProvider<AssetTableEntry>
    {
        Type m_AssetType;
        static GUIContent s_NoneLabel;

        public AssetTableSearchProvider(Type assetType = null) :
            base(FilterIds.AssetTableProvider, "Asset Table")
        {
            m_AssetType = assetType ?? typeof(UnityEngine.Object);
            fetchPreview = GetPreview;

            QueryEngine.SetSearchDataCallback(GetSearchableData, StringComparison.OrdinalIgnoreCase);

            AddTranslatedValueFilter(QueryEngine);

            s_NoneLabel = new GUIContent($"None ({m_AssetType.Name})", EditorGUIUtility.ObjectContent(null, m_AssetType).image);
        }

        [SearchItemProvider]
        internal static SearchProvider CreateProvider() => new AssetTableSearchProvider(typeof(UnityEngine.Object));

        protected override IEnumerable<TableEntrySearchData> GetSearchData()
        {
            // Only include Entries that have a matching type
            var incompatibleTypes = new HashSet<long>();

            foreach (var collection in LocalizationEditorSettings.GetAssetTableCollections())
            {
                // Record all the types that are not compatible with this one, then we can later discard the incompatible entries.
                foreach (var md in collection.SharedData.Metadata.MetadataEntries)
                {
                    if (md is AssetTypeMetadata assetTypeMd && !m_AssetType.IsAssignableFrom(assetTypeMd.Type))
                    {
                        incompatibleTypes.UnionWith(assetTypeMd.EntriesLookup.Keys);
                    }
                }

                foreach (var entry in collection.SharedData.Entries)
                {
                    if (!incompatibleTypes.Contains(entry.Id))
                    {
                        yield return new TableEntrySearchData { Collection = collection, Entry = entry };
                    }
                }

                incompatibleTypes.Clear();
            }
        }

        static Texture2D GetPreview(SearchItem si, SearchContext sc, Vector2 v, FetchPreviewOptions o)
        {
            if (si.data is TableEntrySearchData data)
            {
                return GetPreviewIcon(data);
            }
            return null;
        }

        static Texture2D GetPreviewIcon(TableEntrySearchData data)
        {
            var projectlocaleId = LocalizationSettings.ProjectLocale != null ? LocalizationSettings.ProjectLocale.Identifier : "en"; // Fallback to English if we dont have one.
            if (data.Collection.GetTable(projectlocaleId) is AssetTable at)
            {
                var entry = at.GetEntry(data.Entry.Id);
                if (entry != null && !string.IsNullOrEmpty(entry.Guid))
                {
                    return GetAssetThumbnail(entry.Guid);
                }
            }
            return null;
        }

        // Use https://jira.unity3d.com/browse/AW-976 in the future
        static Texture2D GetAssetThumbnail(string guid)
        {
            var thumbnail = AssetPreviewBridge.GetAssetPreviewFromGUID(guid);
            if (thumbnail != null)
                return thumbnail;
            var path = AssetDatabase.GUIDToAssetPath(guid);
            thumbnail = AssetDatabase.GetCachedIcon(path) as Texture2D;
            return thumbnail ?? InternalEditorUtility.FindIconForFile(path);
        }

        static void AddTranslatedValueFilter(QueryEngine<TableEntrySearchData> queryEngine)
        {
            queryEngine.AddFilter(FilterIds.TranslatedValue, (TableEntrySearchData d, string filterNameMatch, string operatorToken, string filterValue) =>
            {
                var locale = string.IsNullOrEmpty(filterNameMatch) ? null : LocalizationEditorSettings.GetLocale(filterNameMatch);

                // Check all values
                if (locale == null)
                {
                    foreach (var at in ((AssetTableCollection)d.Collection).AssetTables)
                    {
                        var entry = at.GetEntry(d.Entry.Id);
                        if (IsEntryMatch(entry, filterValue, operatorToken))
                            return true;
                    }
                }
                else
                {
                    var table = d.Collection.GetTable(locale.Identifier);
                    if (table is AssetTable at && at.GetEntry(d.Entry.Id) is AssetTableEntry entry)
                    {
                        if (IsEntryMatch(entry, filterValue, operatorToken))
                            return true;
                    }
                }

                return false;
            });
        }

        static bool IsEntryMatch(AssetTableEntry entry, string filterValue, string operatorToken)
        {
            var entryGuid = entry != null ? entry.Guid : string.Empty;
            if (IsStringMatch(entryGuid, filterValue, operatorToken))
                return true;

            if (!string.IsNullOrEmpty(entryGuid))
            {
                var path = AssetDatabase.GUIDToAssetPath(entryGuid);
                var filename = Path.GetFileName(path);
                if (IsStringMatch(filename, filterValue, operatorToken))
                    return true;
            }

            return false;
        }

        protected override IEnumerable<SearchColumn> CreateColumns(SearchContext context, IEnumerable<SearchItem> searchDatas)
        {
            foreach (var c in base.CreateColumns(context, searchDatas))
            {
                yield return c;
            }

            foreach (var locale in LocalizationEditorSettings.GetLocales())
            {
                var col = SearchBridge.CreateColumn($"Localized Asset/{locale.LocaleName}", locale.Identifier.Code, null, new GUIContent(locale.LocaleName, EditorIcons.Locale));
                col.getter = SelectTranslatedAssetEntry;
                col.drawer = DrawTranslatedAssetEntry;
                col.comparer = CompareTranslatedAssetEntry;
                yield return col;
            }
        }

        static object DrawTranslatedAssetEntry(SearchColumnEventArgs args)
        {
            if (Event.current.type != EventType.Repaint || !(args.value is GUIContent label))
                return args.value;

            GUI.Label(args.rect, label);
            return args.value;
        }

        static int CompareTranslatedAssetEntry(SearchColumnCompareArgs args)
        {
            if (args.lhs.value is GUIContent lLabel && args.rhs.value is GUIContent rLabel)
            {
                return string.CompareOrdinal(lLabel.text, rLabel.text);
            }
            return 0;
        }

        // Method is serialized into the column so must be static and not renamed.
        static object SelectTranslatedAssetEntry(SearchColumnEventArgs args)
        {
            if (!(args.item.data is TableEntrySearchData e)) // The None item
                return GUIContent.none;

            if (e.Collection.GetTable(args.column.selector) is AssetTable at && at.GetEntry(e.Entry.Id) is AssetTableEntry ate)
            {
                if (!string.IsNullOrEmpty(ate.LocalizedValue))
                {
                    var path = AssetDatabase.GUIDToAssetPath(ate.Guid);
                    var fileName = Path.GetFileName(path);
                    return new GUIContent(fileName, GetAssetThumbnail(ate.Guid));
                }
            }
            return s_NoneLabel;
        }

        protected override SearchItem CreateSearchItem(SearchContext sc, SearchProvider provider, TableEntrySearchData data)
        {
            var item = provider.CreateItem(sc, $"{data.Collection.TableCollectionName}/{data.Entry.Key}", data.Entry.Key, data.Collection.TableCollectionName, EditorIcons.AssetTableCollection, data);
            item.options = SearchItemOptions.Ellipsis | SearchItemOptions.FullDescription | SearchItemOptions.FuzzyHighlight;
            return item;
        }

        static IEnumerable<string> GetSearchableData(TableEntrySearchData item)
        {
            yield return item.Collection.TableCollectionName;
            yield return item.Entry.Key;

            foreach (var at in ((AssetTableCollection)item.Collection).AssetTables)
            {
                var entry = at.GetEntry(item.Entry.Id);
                var entryGuid = entry != null ? entry.Guid : string.Empty;

                // Guid
                yield return entryGuid;

                if (!string.IsNullOrEmpty(entryGuid))
                {
                    // Path
                    var path = AssetDatabase.GUIDToAssetPath(entryGuid);
                    var fileName = Path.GetFileName(path);
                    yield return fileName;
                }
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
