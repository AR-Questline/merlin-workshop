#if ENABLE_SEARCH

using System;
using UnityEditor.Localization.Search;
using UnityEditor.Search;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;
using ObjectField = UnityEditor.UIElements.ObjectField;

namespace UnityEditor.Localization
{
    class LocalizedAssetField : TableReferenceField<AssetTableCollection>
    {
        Type m_Type;

        public LocalizedAssetField(string label, SerializedProperty localizedAssetProperty, Type type) :
            base(label, localizedAssetProperty)
        {
            m_Type = type;
            style.marginLeft = 6;
        }

        protected override VisualElement CreateLocaleField(Locale locale, AssetTableCollection collection, LocalizationTable table, SharedTableData.SharedTableEntry entry)
        {
            var assetTable = table as AssetTable;
            var assetTableEntry = assetTable.GetEntry(entry.Id);

            var asset = AssetUtility.LoadAssetFromAddress(assetTableEntry?.Address, assetTableEntry?.GetExpectedType());

            var objectField = new ObjectField { label = locale.ToString(), value = asset, objectType = m_Type };
            objectField.AddToClassList(DropdownField.alignedFieldUssClassName);
            objectField.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue != null)
                    collection.AddAssetToTable(assetTable, entry.Id, evt.newValue, true);
                else
                    collection.RemoveAssetFromTable(assetTable, entry.Id, true);
            });
            return objectField;
        }

        protected override SearchProvider CreateSearchProvider() =>  new AssetTableSearchProvider(m_Type);
    }
}

#endif
