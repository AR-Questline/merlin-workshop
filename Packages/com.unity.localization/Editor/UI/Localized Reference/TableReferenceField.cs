#if ENABLE_SEARCH

using System.Collections.Generic;
using UnityEditor.Localization.UI;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;
using Toggle = UnityEngine.UIElements.Toggle;

namespace UnityEditor.Localization
{
    abstract class TableReferenceField<TCollection> : Foldout where TCollection : LocalizationTableCollection
    {
        static readonly string k_None = L10n.Tr("Select Entry...");
        const string k_MixedValues = "-";

        VisualElement m_CachedInspectorElement;
        VisualElement m_CachedContextWidthElement;

        Label m_Label;
        Button m_Menu;
        Button m_OpenTableEditorButton;
        SerializedProperty m_TableProperty;
        SerializedProperty m_EntryProperty;
        TextField m_EntryNameField;
        VisualElement m_TableDetails;
        VisualElement m_ProjectLocaleDetails;
        VisualElement m_OtherLocaleDetails;

        TCollection m_Collection;
        SharedTableData.SharedTableEntry m_Entry;

        protected SerializedProperty m_LocalizedStringProperty;

        public TableReferenceField(string label, SerializedProperty localizedStringProperty)
        {
            text = label;
            m_LocalizedStringProperty = localizedStringProperty;
            m_Menu = new Button { style = { flexGrow = 1, paddingRight = 0, marginRight = 0 } };
            m_Menu.clicked += OnSelectEntry;

            // Determine if we are editing a LocalizedString or LocalizedString.UxmlSerializedData
            m_TableProperty = localizedStringProperty.FindPropertyRelative("m_TableReference") ?? localizedStringProperty.FindPropertyRelative("TableReferenceUXML");
            m_EntryProperty = localizedStringProperty.FindPropertyRelative("m_TableEntryReference") ?? localizedStringProperty.FindPropertyRelative("TableEntryReferenceUXML");

            var toggle = this.Q<Toggle>();
            m_Label = this.Q<Label>();
            m_Label.parent.style.flexGrow = 0;
            toggle.Add(m_Menu);
            //toggle.AddToClassList(DropdownField.alignedFieldUssClassName);

            this.TrackPropertyValue(m_TableProperty, OnTrackedPropertyChanged);
            this.TrackPropertyValue(m_EntryProperty, OnTrackedPropertyChanged);

            var fallback = new PropertyField(localizedStringProperty.FindPropertyRelative("m_FallbackState") ?? localizedStringProperty.FindPropertyRelative("FallbackStateUXML"));
            fallback.label = L10n.Tr("Fallback");
            fallback.tooltip = L10n.Tr("Fallback behaviour to use if the entry cannot be found in the selected table.");
            Add(fallback);

            m_EntryNameField = new TextField(L10n.Tr("Entry Name")) { isDelayed = true };
            m_EntryNameField.RegisterValueChangedCallback(OnEntryNameChanged);
            m_EntryNameField.AddToClassList(DropdownField.alignedFieldUssClassName);
            Add(m_EntryNameField);

            var buttonRow = new VisualElement { style = { marginBottom = 6, height = 25, flexDirection = FlexDirection.RowReverse } };
            var spacer = new VisualElement();
            buttonRow.Add(spacer);
            Add(buttonRow);

            var addEntryButton = new Button() { text = L10n.Tr("Add New Table Entry") };
            addEntryButton.clickable.clicked += () => OnAddNewEntry(addEntryButton);
            buttonRow.Add(addEntryButton);

            m_OpenTableEditorButton = new Button() { text = L10n.Tr("Open Table Editor") };
            m_OpenTableEditorButton.clickable.clicked += () =>
            {
                var tableRef = new SerializedTableReference(m_TableProperty);
                var entryRef = new SerializedTableEntryReference(m_EntryProperty);
                LocalizationTablesWindow.ShowWindow(tableRef.Reference, entryRef.Reference);
            };
            buttonRow.Add(m_OpenTableEditorButton);

            m_ProjectLocaleDetails = new VisualElement();
            m_OtherLocaleDetails = new Foldout { text = L10n.Tr("Localized Values"), viewDataKey = "localized-values-foldout" };
            m_TableDetails = new VisualElement();
            m_TableDetails.Add(m_ProjectLocaleDetails);
            m_TableDetails.Add(m_OtherLocaleDetails);
            Add(m_TableDetails);

            RegisterCallback<AttachToPanelEvent>(_ => Refresh());
            this.Bind(m_LocalizedStringProperty.serializedObject);

            RegisterCallback<CustomStyleResolvedEvent>(_ => AlignLabel());
            RegisterCallback<GeometryChangedEvent>(_ => AlignLabel());
            RegisterCallback<AttachToPanelEvent>(evt =>
            {
                m_CachedInspectorElement = null;
                m_CachedContextWidthElement = null;

                var currentElement = parent;
                while (currentElement != null)
                {
                    if (currentElement.ClassListContains("unity-inspector-element"))
                    {
                        m_CachedInspectorElement = currentElement;
                    }

                    if (currentElement.ClassListContains("unity-inspector-main-container"))
                    {
                        m_CachedContextWidthElement = currentElement;
                        break;
                    }

                    currentElement = currentElement.parent;
                }
            });
        }

        void OnAddNewEntry(Button button)
        {
            var menu = new GenericMenu();

            if (typeof(TCollection) == typeof(StringTableCollection))
                AddCollectionsToMenu(menu, LocalizationEditorSettings.GetStringTableCollections() as IReadOnlyCollection<TCollection>);
            else
                AddCollectionsToMenu(menu, LocalizationEditorSettings.GetAssetTableCollections() as IReadOnlyCollection<TCollection>);

            menu.DropDown(button.worldBound);
        }

        void AddCollectionsToMenu(GenericMenu menu, IReadOnlyCollection<TCollection> collections)
        {
            foreach (var collection in collections)
            {
                menu.AddItem(new GUIContent(collection.TableCollectionName), false, _ =>
                {
                    using (new UndoScope("Add table entry", true))
                    {
                        var key = collection.SharedData.AddKey();
                        var tableRef = new SerializedTableReference(m_TableProperty);
                        var entryRef = new SerializedTableEntryReference(m_EntryProperty);
                        tableRef.SetReference(collection);
                        entryRef.SetReference(key);
                        m_LocalizedStringProperty.serializedObject.ApplyModifiedProperties();

                        EditorUtility.SetDirty(collection.SharedData);
                        Refresh();
                    }
                }, collection);
            }
        }

        void OnTrackedPropertyChanged(SerializedProperty property) => Refresh();

        void OnEntryNameChanged(ChangeEvent<string> evt)
        {
            using (new UndoScope("Rename entry", true))
            {
                Undo.RecordObject(m_Collection.SharedData, "Rename entry");
                EditorUtility.SetDirty(m_Collection.SharedData);

                m_Collection.SharedData.RenameKey(m_Entry.Key, evt.newValue);

                // Update the entry reference
                var entryRef = new SerializedTableEntryReference(m_EntryProperty);
                entryRef.SetReference(m_Entry);
                m_LocalizedStringProperty.serializedObject.ApplyModifiedProperties();
                Refresh();
            }
        }

        void AlignLabel()
        {
            if (m_CachedInspectorElement == null)
                return;

            var labelExtraPadding = 48.0f;
            var labelBaseMinWidth = 123.0f;
            var labelExtraContextWidth = 1.0f;
            var labelWidthRatio = 0.45f;

            // Not all visual input controls have the same padding so we can't base our total padding on
            // that information.  Instead we add a flat value to totalPadding to best match the hard coded
            // calculation in IMGUI
            var totalPadding = labelExtraPadding;
            var spacing = worldBound.x - m_CachedInspectorElement.worldBound.x - m_CachedInspectorElement.resolvedStyle.paddingLeft;

            totalPadding += spacing;
            totalPadding += resolvedStyle.paddingLeft;

            var minWidth = labelBaseMinWidth - spacing - resolvedStyle.paddingLeft;
            var contextWidthElement = m_CachedContextWidthElement ?? m_CachedInspectorElement;

            m_Label.style.minWidth = Mathf.Max(minWidth, 0);

            // Formula to follow IMGUI label width settings
            var newWidth = (contextWidthElement.resolvedStyle.width + labelExtraContextWidth) * labelWidthRatio - totalPadding;
            m_Label.style.width = Mathf.Max(0f, newWidth);
        }

        void Refresh()
        {
            m_ProjectLocaleDetails.Clear();
            m_OtherLocaleDetails.Clear();

            var tableRef = new SerializedTableReference(m_TableProperty);
            var entryRef = new SerializedTableEntryReference(m_EntryProperty);

            if (tableRef.HasMultipleDifferentValues || entryRef.HasMultipleDifferentValues)
            {
                m_Menu.text = k_MixedValues;
                return;
            }

            if (typeof(TCollection) == typeof(StringTableCollection))
                m_Collection = LocalizationEditorSettings.GetStringTableCollection(tableRef.Reference) as TCollection;
            else
                m_Collection = LocalizationEditorSettings.GetAssetTableCollection(tableRef.Reference) as TCollection;

            m_Entry = m_Collection?.SharedData.GetEntryFromReference(entryRef.Reference);
            UpdateLabel();
            RefreshTableDetails();
        }

        void UpdateLabel()
        {
            if (m_Collection == null || m_Entry == null)
            {
                m_Menu.text = k_None;
                return;
            }

            m_Menu.text = $"{m_Collection.TableCollectionName}/{m_Entry.Key}";
        }

        void RefreshTableDetails()
        {
            if (m_Collection == null || m_Entry == null)
            {
                m_EntryNameField.style.display = DisplayStyle.None;
                m_TableDetails.style.display = DisplayStyle.None;
                return;
            }

            m_EntryNameField.style.display = DisplayStyle.Flex;
            m_TableDetails.style.display = DisplayStyle.Flex;
            m_EntryNameField.SetValueWithoutNotify(m_Entry.Key);

            var projectLocale = LocalizationSettings.ProjectLocale;
            if (projectLocale != null)
            {
                var table = m_Collection.GetTable(projectLocale.Identifier);
                var tableElement = CreateLocaleField(projectLocale, m_Collection, table, m_Entry);
                m_ProjectLocaleDetails.Add(tableElement);
            }

            foreach (var table in m_Collection.Tables)
            {
                var locale = LocalizationEditorSettings.GetLocale(table.asset.LocaleIdentifier);
                if (locale == null || locale == projectLocale)
                    continue;

                var tableElement = CreateLocaleField(locale, m_Collection, table.asset, m_Entry);
                m_OtherLocaleDetails.Add(tableElement);
            }
        }

        protected abstract VisualElement CreateLocaleField(Locale locale, TCollection collection, LocalizationTable table, SharedTableData.SharedTableEntry entry);

        protected abstract SearchProvider CreateSearchProvider();

        void OnSelectEntry()
        {
            var provider = CreateSearchProvider();
            var context = UnityEditor.Search.SearchService.CreateContext(provider);

            var picker = new LocalizedReferencePicker<TCollection>(context, "table entry", m_TableProperty, m_EntryProperty);
            picker.Show();
        }
    }
}

#endif
