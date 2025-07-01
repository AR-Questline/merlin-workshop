using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Pseudo;
using UnityEngine.UIElements;

namespace UnityEditor.Localization.UI
{
    class TableCreatorWindow : EditorWindow
    {
        class LocaleLabel : Label
        {
            public Locale boundLocale;
        }

        static readonly Vector2 k_MinSize = new Vector2(400, 300);

        VisualElement m_CollectionTypeContainer;
        TextField m_TableCollectionName;
        ScrollView m_LocalesList;
        Button m_CreateTableCollectionButton;
        PopupField<Type> m_CollectionTypePopup;
        VisualElement m_LocaleHelpBox;
        VisualElement m_LocaleHelpBoxContainer;
        VisualElement m_TableNameHelpBoxContainer;
        VisualElement m_TableNameHelpBox;

        public static void ShowWindow()
        {
            var window = GetWindow<TableCreatorWindow>(true, "New Table Collection", true);
            window.Show();
        }

        void OnEnable()
        {
            minSize = k_MinSize;
            var root = rootVisualElement;

            var asset = Resources.GetTemplateAsset(nameof(TableCreatorWindow));
            asset.CloneTree(rootVisualElement);

            m_LocaleHelpBox = HelpBoxFactory.CreateDefaultHelpBox(L10n.Tr("Must have at least one Locale selected."));

            var locales = LocalizationEditorSettings.GetLocales();
            m_LocalesList = root.Q<ScrollView>("locales-list");
            foreach (var locale in locales)
            {
                AddLocaleElement(locale);
            }
            m_LocaleHelpBoxContainer = root.Q("locale-help-box-container");

            var items = new List<Type> { typeof(StringTableCollection), typeof(AssetTableCollection) };
            m_CollectionTypeContainer = root.Q<VisualElement>("table-collection-type-container");
            m_CollectionTypePopup = new PopupField<Type>("Type", items, 0)
            {
                formatListItemCallback = type => ObjectNames.NicifyVariableName(type.Name),
                formatSelectedValueCallback = type => ObjectNames.NicifyVariableName(type.Name)
            };
            m_CollectionTypePopup.RegisterValueChangedCallback(it => UpdateCreateButtonState());
            m_CollectionTypeContainer.Add(m_CollectionTypePopup);

            m_CreateTableCollectionButton = root.Q<Button>("create-table-collection-button");
            m_CreateTableCollectionButton.clickable.clicked += CreateCollection;

            root.Q<Button>("select-all-button").clickable.clicked += () => SelectAllLocales(true);
            root.Q<Button>("select-none-button").clickable.clicked += () => SelectAllLocales(false);
            root.Q<Button>("locale-generator-button").clickable.clicked += () => LocaleGeneratorWindow.ShowWindow();

            m_TableCollectionName = root.Q<TextField>("new-table-name-field");
            m_TableCollectionName.RegisterValueChangedCallback(it => UpdateCreateButtonState());
            InitializeTableName();

            m_TableNameHelpBoxContainer = root.Q("table-name-help-box-container");

            LocalizationEditorSettings.EditorEvents.LocaleAdded += OnLocaleAdded;
            LocalizationEditorSettings.EditorEvents.LocaleRemoved += OnLocaleRemoved;
        }

        void OnDisable()
        {
            LocalizationEditorSettings.EditorEvents.LocaleAdded -= OnLocaleAdded;
            LocalizationEditorSettings.EditorEvents.LocaleRemoved -= OnLocaleRemoved;
        }

        void OnLocaleAdded(Locale locale)
        {
            AddLocaleElement(locale);
            UpdateCreateButtonState();
        }

        void OnLocaleRemoved(Locale locale)
        {
            for (int i = 0; i < m_LocalesList.childCount; ++i)
            {
                var localeLabel = m_LocalesList[i].Q<Label>();
                if (localeLabel != null && localeLabel.text == locale.name)
                {
                    m_LocalesList.Remove(m_LocalesList[i]);
                    UpdateCreateButtonState();
                }
            }
        }

        void AddLocaleElement(Locale locale)
        {
            if (locale is PseudoLocale) // Don't include pseudo locales
                return;

            var so = new SerializedObject(locale);

            var visualElement = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center }, };
            var toggle = new Toggle() { value = true };
            toggle.RegisterValueChangedCallback((evt) => UpdateCreateButtonState());
            var label = new LocaleLabel() { boundLocale = locale, bindingPath = "m_LocaleName" };
            visualElement.Add(toggle);
            visualElement.Add(label);
            visualElement.Bind(so);

            m_LocalesList.Add(visualElement);
        }

        void UpdateCreateButtonState()
        {
            RemoveHelpBoxes();

            var atLeastOneActiveLocale = m_LocalesList.Children().Any(it => it.Q<Toggle>().value);
            var tableNameError = LocalizationEditorSettings.Instance.IsTableNameValid(m_CollectionTypePopup.value, m_TableCollectionName?.value);

            var createEnabled = atLeastOneActiveLocale && tableNameError == null;
            if (!createEnabled)
            {
                if (!atLeastOneActiveLocale)
                {
                    m_LocaleHelpBoxContainer.Add(m_LocaleHelpBox);
                }
                if (tableNameError != null)
                {
                    m_TableNameHelpBox = HelpBoxFactory.CreateDefaultHelpBox(tableNameError);
                    m_TableNameHelpBoxContainer.Add(m_TableNameHelpBox);
                }
            }
            m_CreateTableCollectionButton.SetEnabled(createEnabled);
        }

        void RemoveHelpBoxes()
        {
            m_TableNameHelpBox?.RemoveFromHierarchy();
            m_LocaleHelpBox.RemoveFromHierarchy();
        }

        void SelectAllLocales(bool selected)
        {
            foreach (var localeItem in m_LocalesList.Children())
            {
                var toggle = localeItem.Q<Toggle>();
                toggle.value = selected;
            }
        }

        List<Locale> GetSelectedLocales()
        {
            var selectedLocales = new List<Locale>();

            foreach (var localeItem in m_LocalesList.Children())
            {
                var toggle = localeItem.Q<Toggle>();
                var label = localeItem.Q<LocaleLabel>();

                if (toggle.value)
                {
                    if (label.boundLocale != null)
                        selectedLocales.Add(label.boundLocale);
                    else
                        Debug.LogError($"Expected locale to match toggle. Expected {label.boundLocale.name} but got {label.text}");
                }
            }

            return selectedLocales;
        }

        void CreateCollection()
        {
            var assetDirectory = EditorUtility.SaveFolderPanel("Create Table Collection", "Assets/", "");
            if (string.IsNullOrEmpty(assetDirectory))
                return;

            LocalizationTableCollection createdCollection = null;
            if (m_CollectionTypePopup.value == typeof(StringTableCollection))
            {
                createdCollection = LocalizationEditorSettings.CreateStringTableCollection(m_TableCollectionName.value, assetDirectory, GetSelectedLocales());
            }
            if (m_CollectionTypePopup.value == typeof(AssetTableCollection))
            {
                createdCollection = LocalizationEditorSettings.CreateAssetTableCollection(m_TableCollectionName.value, assetDirectory, GetSelectedLocales());
            }

            // Select the root asset and open the table editor window.
            Selection.activeObject = createdCollection;
            LocalizationTablesWindow.ShowWindow(createdCollection);
            Close();
        }

        void InitializeTableName()
        {
            m_TableCollectionName.value = LocalizationEditorSettings.Instance.GetUniqueTableCollectionName(
                m_CollectionTypePopup.value,
                "New Table");
            UpdateCreateButtonState();
        }
    }
}
