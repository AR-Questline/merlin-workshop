#if ENABLE_SEARCH && MODULE_UITK && UNITY_2023_3_OR_NEWER

using System;
using UnityEditor.Localization.Search;
using UnityEditor.Localization.UI;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;
using ListView = UnityEngine.UIElements.ListView;

namespace UnityEditor.Localization
{
    class LocalizedStringField : TableReferenceField<StringTableCollection>
    {
        static readonly string k_Empty = L10n.Tr("<empty>");

        ListView m_Variables;

        public LocalizedStringField(string label, SerializedProperty localizedStringProperty) :
            base(label, localizedStringProperty)
        {
            style.marginLeft = 6;
            m_Variables = new ListView()
            {
                bindItem = BindVariablesListItem,
                bindingPath = nameof(LocalizedString.LocalVariablesUXML),
                headerTitle = "Variables",
                overridingAddButtonBehavior = OnAddUxmlSerializedDataVariable,
                showAddRemoveFooter = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                showBorder = true,
                showBoundCollectionSize = true,
                showFoldoutHeader = true,
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                viewDataKey = "variables-list",
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,

            };
            Add(m_Variables);
            m_Variables.Bind(m_LocalizedStringProperty.serializedObject);
        }

        protected override VisualElement CreateLocaleField(Locale locale, StringTableCollection collection, LocalizationTable table, SharedTableData.SharedTableEntry entry)
        {
            var stringTable = table as StringTable;
            var stringTableEntry = stringTable.GetEntry(entry.Id);

            var foldout = new Foldout { value = false, text = locale.ToString(), style = { unityFontStyleAndWeight = FontStyle.Bold } };
            foldout.viewDataKey = locale.ToString();

            var toggle = foldout.Q<Toggle>();
            toggle[0].style.width = 150;
            toggle[0].style.flexGrow = 0;

            var label = new Label(GetLabelText(stringTableEntry)) { style = { flexGrow = 1, unityFontStyleAndWeight = FontStyle.Normal } };
            toggle.Add(label);

            var smartFormatField = new SmartFormatField(stringTable, entry.Id, m_LocalizedStringProperty);
            smartFormatField.ValueField.RegisterCallback<ChangeEvent<string>>(evt =>
            {
                label.text = evt.newValue;
            });

            foldout.Add(smartFormatField);

            return foldout;
        }

        static string GetLabelText(StringTableEntry entry) => string.IsNullOrEmpty(entry?.Value) ? k_Empty : entry.Value;

        void BindVariablesListItem(VisualElement visualElement, int index)
        {
            visualElement.Clear();

            var list = m_LocalizedStringProperty.FindPropertyRelative(nameof(LocalizedString.LocalVariablesUXML));
            var element = list.GetArrayElementAtIndex(index);

            var nameProperty = element.FindPropertyRelative(nameof(LocalVariable.Name));
            if (nameProperty == null)
            {
                visualElement.Add(new Label("null"));
                return;
            }

            var nameField = new PropertyField { bindingPath = nameProperty.propertyPath };
            visualElement.Add(nameField);

            var variableProperty = element.FindPropertyRelative(nameof(LocalVariable.Variable));
            if (variableProperty == null)
                return;

            var variableField = new PropertyField { bindingPath = variableProperty.propertyPath };
            visualElement.Add(variableField);

            visualElement.Bind(m_LocalizedStringProperty.serializedObject);
        }

        void OnAddUxmlSerializedDataVariable(BaseListView list, Button addButton)
        {
            var menu = new GenericMenu();
            TypeUtility.PopulateMenuWithCreateItems(menu, typeof(IVariable), type =>
            {
                var uxmlSerializedDataType = type.GetNestedType(nameof(UxmlSerializedData));
                if (uxmlSerializedDataType == null)
                {
                    Debug.LogError($"Expected {type.FullName} to have {nameof(UxmlSerializedData)}");
                    return;
                }

                // Create list element
                var list = m_LocalizedStringProperty.FindPropertyRelative(nameof(LocalizedString.LocalVariablesUXML));
                var element = list.AddArrayElement();
                element.managedReferenceValue = new LocalVariable.UxmlSerializedData(); 

                // Add variable instance
                var variable = element.FindPropertyRelative(nameof(LocalVariable.Variable));
                variable.managedReferenceValue = Activator.CreateInstance(uxmlSerializedDataType);

                // Set default name
                var name = element.FindPropertyRelative(nameof(LocalVariable.Name));
                name.stringValue = list.arraySize > 1 ? $"variable-{list.arraySize - 1}" : "variable";

                list.serializedObject.ApplyModifiedProperties();
            }, typeof(UxmlObjectAttribute));

            menu.DropDown(addButton.worldBound);
        }

        protected override SearchProvider CreateSearchProvider() => new StringTableSearchProvider();
    }
}

#endif
