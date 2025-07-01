#if ENABLE_SEARCH_QUERY_BUILDER

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Localization.UI;
using UnityEditor.Search;
using UnityEngine.Localization.Metadata;

namespace UnityEditor.Localization.Search
{
    [QueryListBlock(null, "Type", FilterIds.MetadataType, ":")]
    class MetadataTypeFilterBlock : QueryListBlock
    {
        Type m_Type;

        public MetadataType AllowedMetadataTypes { get; set; } = MetadataType.All;

        static Dictionary<(int, string), List<SearchProposition>> s_PropositionCache = new Dictionary<(int, string), List<SearchProposition>>();
        static Dictionary<string, Type> s_TypeNameLookup;

        public MetadataTypeFilterBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr) :
            base(source, id, value, attr)
        {
            if (source?.context?.filterId == $"{FilterIds.AssetTableProvider}:")
                AllowedMetadataTypes = MetadataType.AssetTable | MetadataType.AssetTableEntry | MetadataType.SharedAssetTableEntry;
            else
                AllowedMetadataTypes = MetadataType.StringTable | MetadataType.StringTableEntry | MetadataType.SharedStringTableEntry;

            icon = EditorIcons.Metadata;
            SetType(GetValueType(value));
        }

        void SetType(Type type)
        {
            m_Type = type;
            if (m_Type != null)
            {
                alwaysDrawLabel = false;
                value = type.Name;
                label = ManagedReferenceUtility.GetDisplayName(type).text;
            }
            else
            {
                alwaysDrawLabel = true;
            }
        }

        static Type GetValueType(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            if (s_TypeNameLookup == null)
            {
                s_TypeNameLookup = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
                foreach (var metaType in TypeCache.GetTypesDerivedFrom<IMetadata>())
                {
                    s_TypeNameLookup[metaType.Name] = metaType;
                }
            }

            if (s_TypeNameLookup.TryGetValue(value, out var type))
                return type;
            return null;
        }

        public override void Apply(in SearchProposition searchProposition)
        {
            if (searchProposition.data is Type t)
            {
                SetType(t);
                source.Apply();
            }
        }

        public override IEnumerable<SearchProposition> GetPropositions(SearchPropositionFlags flags = SearchPropositionFlags.None)
        {
            int allowedTypes = (int)AllowedMetadataTypes; // dictionary + enum = garbage, so just use an int here.
            if (!s_PropositionCache.TryGetValue((allowedTypes, category), out var props))
            {
                props = new List<SearchProposition>();

                foreach (var metaType in TypeCache.GetTypesDerivedFrom<IMetadata>())
                {
                    // Filter out the unsupported metadata.
                    var itemAttribute = metaType.GetCustomAttribute<MetadataAttribute>();
                    if (itemAttribute == null || (itemAttribute.AllowedTypes & AllowedMetadataTypes) == 0)
                        continue;

                    var name = ManagedReferenceUtility.GetDisplayName(metaType).text;
                    if (!string.IsNullOrEmpty(category))
                        name += "/Type";

                    props.Add(new SearchProposition(category, name, $"{id}={metaType.Name}", string.Empty, -2, icon: EditorIcons.Metadata, data: metaType));
                }

                s_PropositionCache[(allowedTypes, category)] = props;
            }

            foreach (var p in props)
            {
                yield return p;
            }
        }
    }
}

#endif
