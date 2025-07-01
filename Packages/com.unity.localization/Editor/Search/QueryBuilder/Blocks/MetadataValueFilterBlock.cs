#if ENABLE_SEARCH_QUERY_BUILDER

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Localization.UI;
using UnityEditor.Search;
using UnityEngine.Localization.Metadata;

namespace UnityEditor.Localization.Search
{
    //[QueryListBlock(null, "Metadata Value", FilterIds.MetadataValue)]
    class MetadataValueFilterBlock : QueryListBlock
    {
        object m_FieldOrProperty;

        static Dictionary<(int, string), List<SearchProposition>> s_PropositionCache = new Dictionary<(int, string), List<SearchProposition>>();

        public MetadataType AllowedMetadataTypes { get; set; } = MetadataType.All;

        public MetadataValueFilterBlock(IQuerySource source, string id, string value, QueryListBlockAttribute attr) :
            base(source, id, value, attr)
        {
            if (source?.context.filterId == $"{FilterIds.AssetTableProvider}:")
                AllowedMetadataTypes = MetadataType.AssetTable | MetadataType.AssetTableEntry | MetadataType.SharedAssetTableEntry;
            else
                AllowedMetadataTypes = MetadataType.StringTable | MetadataType.StringTableEntry | MetadataType.SharedStringTableEntry;

            //SetType(GetValueType(value));


            // SetType(GetValueType(value));
        }

        void SetValue(object getter)
        {
            m_FieldOrProperty = getter;
            if (m_FieldOrProperty != null)
            {
                //value = type.Name;
                label = ObjectNames.NicifyVariableName(getter.ToString());
            }
        }

        //static Type GetValueType(string value)
        //{
        //    foreach (var metaType in TypeCache.GetTypesDerivedFrom<IMetadata>())
        //    {
        //        // Filter out the unsupported metadata.
        //        var itemAttribute = metaType.GetCustomAttribute<MetadataAttribute>();
        //        if (itemAttribute == null)
        //            continue;

        //        if (string.Equals(metaType.Name, value, StringComparison.OrdinalIgnoreCase))
        //        {
        //            return metaType;
        //        }

        //    }
        //    return null;
        //}

        public override void Apply(in SearchProposition searchProposition)
        {
            if (searchProposition.data is FieldInfo || searchProposition.data is PropertyInfo)
            {
                SetValue(searchProposition.data);
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

                    // Extract values
                    foreach (var prop in metaType.GetRuntimeProperties())
                    {
                        props.Add(new SearchProposition(category, $"{name}/{prop.Name}({prop.PropertyType.Name})", $"mv({prop.Name}):someValue", string.Empty, 0, type: prop.PropertyType, data: prop));
                    }

                    foreach (var field in metaType.GetRuntimeFields())
                    {
                        props.Add(new SearchProposition(category, $"{name}/{field.Name}({field.FieldType.Name})", $"mv({field.Name}):someValue", string.Empty, 0, type: field.FieldType, data: field));
                    }

                    //props.Add(new SearchProposition(category, $"{name}/ToString", "mv:someValue", string.Empty, -1, data: null));
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
