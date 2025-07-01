using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

#if ENABLE_SEARCH
using UnityEditor.Localization.Search;
#endif

namespace UnityEditor.Localization.UI
{
    [CustomPropertyDrawer(typeof(LocalizedAsset<>), true)]
    class LocalizedAssetPropertyDrawer : LocalizedReferencePropertyDrawer<AssetTableCollection>
    {
        static LocalizedAssetPropertyDrawer()
        {
            GetProjectTableCollections = LocalizationEditorSettings.GetAssetTableCollections;
        }

        public override Data CreatePropertyData(SerializedProperty property)
        {
            var prop = base.CreatePropertyData(property);

            // Find the asset type.
            Type baseType = fieldInfo.FieldType;
            if (property.propertyType == SerializedPropertyType.ManagedReference)
                baseType = ManagedReferenceUtility.GetType(property.managedReferenceFullTypename);

            Type assetType = null;
            while (baseType != null)
            {
                if (baseType.IsArray)
                    baseType = baseType.GetElementType().BaseType;

                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(LocalizedAsset<>))
                {
                    assetType = baseType.GetGenericArguments()[0];
                    break;
                }
                baseType = baseType.BaseType;
            }
            Debug.Assert(assetType != null, "Could not determine the asset type for " + fieldInfo.FieldType.Name);
            prop.assetType = assetType;
            return prop;
        }

        protected override void DrawTableEntryDetails(ref Rect rowPosition, Data data, Rect position)
        {
            base.DrawTableEntryDetails(ref rowPosition, data, position);

            var projectLocales = LocalizationEditorSettings.GetLocales();

            foreach (var locale in projectLocales)
            {
                var table = data.SelectedTableCollection.Tables.FirstOrDefault(tbl => tbl.asset?.LocaleIdentifier == locale.Identifier).asset as AssetTable;

                if (table != null)
                {
                    var tableEntry = table.GetEntry(data.SelectedTableEntry.Id);
                    var asset = AssetUtility.LoadAssetFromAddress(tableEntry?.Address, tableEntry?.GetExpectedType());

                    EditorGUI.BeginChangeCheck();
                    var newAsset = EditorGUI.ObjectField(rowPosition, locale.Identifier.ToString(), asset, data.assetType, false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (newAsset != null)
                            data.SelectedTableCollection.AddAssetToTable(table, data.SelectedTableEntry.Id, newAsset, true);
                        else
                            data.SelectedTableCollection.RemoveAssetFromTable(table, data.SelectedTableEntry.Id, true);
                    }
                }
                else
                {
                    var buttonPosition = EditorGUI.PrefixLabel(rowPosition, new GUIContent(locale.Identifier.ToString()));
                    if (GUI.Button(buttonPosition, "Create Table"))
                    {
                        data.SelectedTableCollection.AddNewTable(locale.Identifier);
                        GUIUtility.ExitGUI();
                    }
                }

                rowPosition.y += rowPosition.height + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        #if ENABLE_SEARCH
        protected override void ShowPicker(Data data, Rect dropDownPosition)
        {
            if (!LocalizationEditorSettings.UseLocalizedAssetSearchPicker)
            {
                base.ShowPicker(data, dropDownPosition);
                return;
            }

            var provider = new AssetTableSearchProvider(data.assetType);
            var context = UnityEditor.Search.SearchService.CreateContext(provider, FilterIds.AssetTableProviderFilter);
            var picker = new LocalizedReferencePicker<StringTableCollection>(context, "string table entry", data.tableReference.Property, data.tableEntryReference.Property);
            picker.Show();
        }
        #endif

        public override float GetPropertyHeight(Data data, SerializedProperty property, GUIContent label)
        {
            float height = base.GetPropertyHeight(data, property, label);

            if (property.isExpanded && data.SelectedTableEntry != null)
            {
                height += LocalizationEditorSettings.GetLocales().Count * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
            }
            return height;
        }
    }
}
