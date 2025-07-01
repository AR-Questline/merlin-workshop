using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;
using TreeView = UnityEditor.IMGUI.Controls.TreeView;

namespace UnityEditor.Localization.UI
{
    /// <summary>
    /// Window used to edit localization tables.
    /// </summary>
    public class LocalizationTablesWindow : EditorWindow, IHasCustomMenu
    {
        const string k_WindowTitle = "Localization Tables";
        const string k_Search = "Localization-TablesView-Search";

        internal static LocalizationTablesWindow s_Instance;

        static readonly Vector2 k_MinSize = new Vector2(900, 600);

        internal ToolbarMenu m_ImportMenu;
        internal ToolbarMenu m_ExportMenu;
        internal ProjectTablesPopup m_ProjectTablesPopup;
        Foldout m_CurrentTableFoldout;
        string m_CurrentTableFoldoutText;
        VisualElement m_TableDetails;
        internal TextField m_NameField;
        Toggle m_PreloadTables;
        VisualElement m_TableNameHelpBoxContainer;
        VisualElement m_TableContents;
        internal ToolbarSearchField m_ToolbarSearchField;
        IMGUIContainer m_TableContentsImguiContainer;
        internal TreeView m_TableView;
        bool m_SelectionChanged;

        LocalizationTableCollection SelectedCollection
        {
            get
            {
                if (m_ProjectTablesPopup.value is ProjectTablesPopup.NoTables)
                    return null;
                return m_ProjectTablesPopup.value;
            }
        }

        /// <summary>
        /// Opens the window in the Editor.
        /// </summary>
        [MenuItem("Window/Asset Management/Localization Tables")]
        public static void ShowWindow()
        {
            s_Instance = GetWindow<LocalizationTablesWindow>(false, null, true);
            s_Instance.titleContent = EditorGUIUtility.TrTextContent(k_WindowTitle, EditorIcons.TableWindow);
            s_Instance.Show();
        }

        /// <summary>
        /// Opens the window in the Editor and selects <paramref name="selectedcollection"/> for editing.
        /// </summary>
        /// <param name="selectedcollection"></param>
        public static void ShowWindow(LocalizationTableCollection selectedcollection)
        {
            s_Instance = GetWindow<LocalizationTablesWindow>(false, null, true);
            s_Instance.titleContent = EditorGUIUtility.TrTextContent(k_WindowTitle, EditorIcons.TableWindow);
            s_Instance.Show(true);
            s_Instance.EditCollection(selectedcollection);
        }

        /// <summary>
        /// Opens the window in the Editor and selects the table collection and entry for editing.
        /// </summary>
        /// <param name="tableEntryReference">The table to select for editing.</param>
        /// <param name="tableReference">The entry to highlight for editing.</param>
        public static void ShowWindow(TableReference tableReference, TableEntryReference tableEntryReference)
        {
            s_Instance = GetWindow<LocalizationTablesWindow>(false, null, true);
            s_Instance.titleContent = EditorGUIUtility.TrTextContent(k_WindowTitle, EditorIcons.TableWindow);
            s_Instance.Show();
            s_Instance.EditCollection(tableReference, tableEntryReference);
        }

        /// <summary>
        /// Opens the window in the Editor with the table creator tab active.
        /// </summary>
        public static void ShowTableCreator()
        {
            ShowWindow();
        }

        /// <summary>
        /// Selects the collection for editing.
        /// </summary>
        /// <param name="selectedCollection"></param>
        public void EditCollection(LocalizationTableCollection selectedCollection)
        {
            if (LocalizationEditorSettings.ActiveLocalizationSettings == null)
                return;
            m_ProjectTablesPopup.value = selectedCollection;
        }

        public void EditCollection(TableReference tableReference, TableEntryReference tableEntryReference)
        {
            if (LocalizationEditorSettings.ActiveLocalizationSettings == null)
                return;

            var stc = LocalizationEditorSettings.GetStringTableCollection(tableReference);
            var atc = LocalizationEditorSettings.GetAssetTableCollection(tableReference);

            SharedTableData.SharedTableEntry entry = null;
            LocalizationTableCollection collection = stc;
            if (stc != null)
            {
                entry = stc.SharedData.GetEntryFromReference(tableEntryReference);
            }
            if (atc != null && entry == null)
            {
                collection = atc;
                entry = atc.SharedData.GetEntryFromReference(tableEntryReference);
            }

            m_ProjectTablesPopup.value = collection;
            m_ToolbarSearchField.value = entry != null ? entry.Id.ToString() : string.Empty;
        }

        void OnEnable()
        {
            minSize = k_MinSize;
            var root = rootVisualElement;
            if (LocalizationEditorSettings.ActiveLocalizationSettings == null)
            {
                var asset = Resources.GetTemplateAsset("CreateLocalizationSettings");
                asset.CloneTree(root);
                var button = root.Q<Button>("create-settings-button");
                button.clickable.clicked += () =>
                {
                    var created = LocalizationSettingsMenuItems.CreateLocalizationAsset();
                    if (created != null)
                    {
                        LocalizationEditorSettings.ActiveLocalizationSettings = created;
                        root.Clear();
                        OnEnable();
                    }
                };
            }
            else
            {
                var asset = Resources.GetTemplateAsset(nameof(LocalizationTablesWindow));
                asset.CloneTree(root);
                Init();
            }
        }

        void OnDisable()
        {
            s_Instance = null;
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            MetadataEditorWindow.CloseWindow();
        }

        void Init()
        {
            var root = rootVisualElement;

            var newCollectionButton = root.Q<ToolbarButton>("new-table-collection-btn");

            newCollectionButton.clickable.clicked += TableCreatorWindow.ShowWindow;

            m_ImportMenu = rootVisualElement.Q<ToolbarMenu>("import-btn");
            m_ExportMenu = rootVisualElement.Q<ToolbarMenu>("export-btn");

            m_ProjectTablesPopup = root.Q<ProjectTablesPopup>();
            m_ProjectTablesPopup.RegisterValueChangedCallback(_ => m_SelectionChanged = true);

            m_CurrentTableFoldout = root.Q<Foldout>("current-table");
            m_CurrentTableFoldoutText = m_CurrentTableFoldout.text;
            UpdateCurrentTableFoldoutLabel();

            m_TableDetails = root.Q("table-details");
            m_NameField = root.Q<TextField>("collection-name");
            m_NameField.RegisterValueChangedCallback(TableCollectionNameChanged);
            m_TableNameHelpBoxContainer = root.Q("table-name-help-box-container");

            m_PreloadTables = root.Q<Toggle>("preload-tables");
            m_PreloadTables.RegisterValueChangedCallback(evt => SelectedCollection.SetPreloadTableFlag(evt.newValue, true));

            m_ToolbarSearchField = root.Q<ToolbarSearchField>("table-search");
            m_ToolbarSearchField.SetValueWithoutNotify(SessionState.GetString(k_Search, null));
            m_ToolbarSearchField.RegisterValueChangedCallback(SearchChanged);

            m_TableContents = root.Q("table-contents");
            m_TableContentsImguiContainer = new IMGUIContainer(OnTableContentsIMGUI);
            m_TableContents.Add(m_TableContentsImguiContainer);
            m_TableContentsImguiContainer.StretchToParentSize();

            m_SelectionChanged = true;

            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        void UndoRedoPerformed()
        {
            SelectedCollection.RefreshAddressables();

            var name = SelectedCollection.TableCollectionName;
            m_NameField.SetValueWithoutNotify(name);
            m_PreloadTables.SetValueWithoutNotify(SelectedCollection.IsPreloadTableFlagSet());
            m_ProjectTablesPopup.RefreshLabels();
            m_TableView?.Reload();
            UpdateCurrentTableFoldoutLabel();
        }

        void SearchChanged(ChangeEvent<string> evt)
        {
            SessionState.SetString(k_Search, evt.newValue);

            if (m_TableView != null)
                m_TableView.searchString = evt.newValue;
        }

        void TableCollectionNameChanged(ChangeEvent<string> evt)
        {
            m_TableNameHelpBoxContainer.Clear();

            if (SelectedCollection.TableCollectionName == evt.newValue)
                return;

            var tableNameError = LocalizationEditorSettings.Instance.IsTableNameValid(SelectedCollection.GetType(), evt.newValue);
            if (tableNameError != null)
            {
                var helpbox = HelpBoxFactory.CreateDefaultHelpBox(tableNameError);
                m_TableNameHelpBoxContainer.Add(helpbox);
                return;
            }

            SelectedCollection.SetTableCollectionName(evt.newValue, true);
            m_ProjectTablesPopup.RefreshLabels();
            UpdateCurrentTableFoldoutLabel();
        }

        void TableCollectionSelectionChanged()
        {
            MetadataEditorWindow.CloseWindow();

            if (m_TableView is IDisposable disposable)
                disposable.Dispose();
            m_TableView = null;

            UpdateCurrentTableFoldoutLabel();
            UpdateImportExportMenuItems();

            if (SelectedCollection == null)
            {
                m_TableDetails.style.display = DisplayStyle.None;
                return;
            }

            m_TableDetails.style.display = DisplayStyle.Flex;
            m_NameField.SetValueWithoutNotify(SelectedCollection.TableCollectionName);
            m_PreloadTables.SetValueWithoutNotify(SelectedCollection.IsPreloadTableFlagSet());

            if (SelectedCollection is StringTableCollection stringTableCollection)
            {
                var stringTableListView = new StringTableListView(stringTableCollection);
                stringTableListView.Initialize();
                m_TableView = stringTableListView;
            }
            else if (SelectedCollection is AssetTableCollection assetTableCollection)
            {
                var assetTableListView = new LocalizedAssetTableListView(assetTableCollection);
                assetTableListView.Initialize();
                m_TableView = assetTableListView;
            }

            m_TableView.searchString = m_ToolbarSearchField.value;
        }

        void UpdateCurrentTableFoldoutLabel()
        {
            var label = L10n.Tr(m_CurrentTableFoldoutText);
            if (SelectedCollection != null)
                label += $" ({SelectedCollection.TableCollectionName})";
            m_CurrentTableFoldout.text = label;
        }

        void OnTableContentsIMGUI()
        {
            if (m_SelectionChanged)
            {
                // We need to do selection inside of the IMGUI container as it uses
                // EditorStyles and they may not be initialized outside of the callback.
                TableCollectionSelectionChanged();
                m_SelectionChanged = false;
            }

            m_TableView?.OnGUI(m_TableContentsImguiContainer.layout);
        }

        internal void UpdateImportExportMenuItems()
        {
            PopulateMenuWithAttribute<LocalizationImportMenuAttribute>(m_ImportMenu);
            PopulateMenuWithAttribute<LocalizationExportMenuAttribute>(m_ExportMenu);
        }

        void PopulateMenuWithAttribute<T>(ToolbarMenu toolbarMenu) where T : Attribute
        {
            toolbarMenu.menu.MenuItems().Clear();

            object[] args = { SelectedCollection, toolbarMenu.menu };
            foreach (var t in TypeCache.GetMethodsWithAttribute<T>())
            {
                var parameters = t.GetParameters();
                if (!t.IsStatic ||
                    t.ReturnType != typeof(void) ||
                    parameters.Length != 2 ||
                    parameters[0].ParameterType != typeof(LocalizationTableCollection) ||
                    parameters[1].ParameterType != typeof(DropdownMenu))
                {
                    Debug.LogError($"{typeof(T).Name} method {t.Name} must have the following signature: static void {t.Name}(LocalizationTableCollection, DropdownMenu)");
                    continue;
                }

                t.Invoke(null, args);
            }

            toolbarMenu.SetEnabled(toolbarMenu.menu.MenuItems().Count > 0);
        }

        /// <summary>
        /// Adds menu items to the table context window.
        /// </summary>
        /// <param name="menu"></param>
        public void AddItemsToMenu(GenericMenu menu)
        {
            // Not used, however removing this API will require a major version increase.
        }
    }
}
