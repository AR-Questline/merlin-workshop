#if MODULE_UITK && UNITY_2023_3_OR_NEWER

using UnityEditor.UIElements;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization
{
    class TableEntryReferenceConverter : UxmlAttributeConverter<TableEntryReference>
    {
        const string k_IdStart = "Id(";
        const string k_IdEnd = ")";
        const string k_IdFormat = k_IdStart + "{0}" + k_IdEnd;

        public override TableEntryReference FromString(string value)
        {
            if (value != null && value.StartsWith(k_IdStart, System.StringComparison.OrdinalIgnoreCase) && value.EndsWith(k_IdEnd))
            {
                if (long.TryParse(value.Substring(k_IdStart.Length, value.Length - k_IdStart.Length - k_IdEnd.Length), out var id))
                    return id;
            }
            return value;
        }

        public override string ToString(TableEntryReference value)
        {
            if (value.ReferenceType == TableEntryReference.Type.Id)
                return string.Format(k_IdFormat, value.KeyId);
            return value.Key;
        }
    }
}

#endif
