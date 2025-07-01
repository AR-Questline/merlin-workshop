#if MODULE_UITK && UNITY_2023_3_OR_NEWER

using UnityEditor.UIElements;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization
{
    class TableReferenceConverter : UxmlAttributeConverter<TableReference>
    {
        public override TableReference FromString(string value) => TableReference.TableReferenceFromString(value);
        public override string ToString(TableReference value) => value.GetSerializedString();
    }
}

#endif
