using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Localization.Addressables;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Pseudo;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using Object = UnityEngine.Object;

namespace UnityEditor.Localization
{
    /// <summary>
    /// Provides methods for configuring Localization settings including tables and Locales.
    /// </summary>
    public class LocalizationEditorSettings
    {
        static readonly char[] k_UnityInvalidFileNameChars = { '/', '?', '<', '>', '\\', ':', '|', '\"' };
        static readonly IEnumerable<char> k_InvalidFileNameChars = Path.GetInvalidFileNameChars().Concat(k_UnityInvalidFileNameChars);

        internal const string k_GameViewPref = "Localization-ShowLocaleMenuInGameView";
        internal const string k_StringPicker = "Localization-UseSearchStringPicker";
        internal const string k_AssetPicker = "Localization-UseSearchAssetPicker";
        internal const string k_TableRefMethod = "Localization-TableRefMethod";
        internal const string k_EntryRefMethod = "Localization-EntryRefMethod";

        static LocalizationEditorSettings s_Instance;

        // Cached searches to help performance.
        ReadOnlyCollection<Locale> m_ProjectLocales;
        ReadOnlyCollection<PseudoLocale> m_ProjectPseudoLocales;

        // Allows for overriding the default behavior, used for testing.
        internal static LocalizationEditorSettings Instance
        {
            get => s_Instance ?? (s_Instance = new LocalizationEditorSettings());
            set => s_Instance = value;
        }

        internal virtual LocalizationTableCollectionCache TableCollectionCache { get; set; } = new LocalizationTableCollectionCache();

        /// <summary>
        /// The <see cref="LocalizationSettings"/> used for this project and available in the player and editor.
        /// </summary>
        public static LocalizationSettings ActiveLocalizationSettings
        {
            get => Instance.ActiveLocalizationSettingsInternal;
            set => Instance.ActiveLocalizationSettingsInternal = value;
        }

        /// <summary>
        /// During play mode, in the editor a menu can be shown to allow for quickly changing the <see cref="LocalizationSettings.SelectedLocale"/>.
        /// </summary>
        public static bool ShowLocaleMenuInGameView
        {
            get => EditorPrefs.GetBool(k_GameViewPref, true);
            set => EditorPrefs.SetBool(k_GameViewPref, value);
        }

        /// <summary>
        /// When <see langword="true"/> the advanced Unity Search picker will be used when selecting <see cref="LocalizedString"/> table entries.
        /// When <see langword="false"/> the tree view picker will be used.
        /// </summary>
        public static bool UseLocalizedStringSearchPicker
        {
            get => EditorPrefs.GetBool(k_StringPicker, true);
            set => EditorPrefs.SetBool(k_StringPicker, value);
        }

        /// <summary>
        /// When <see langword="true"/> the advanced Unity Search picker will be used when selecting <see cref="LocalizedAsset{TObject}"/> table entries.
        /// When <see langword="false"/> the tree view picker will be used.
        /// </summary>
        public static bool UseLocalizedAssetSearchPicker
        {
            get => EditorPrefs.GetBool(k_AssetPicker, true);
            set => EditorPrefs.SetBool(k_AssetPicker, value);
        }

        /// <summary>
        /// Adds a reference to a table in the Editor.
        /// </summary>
        public static TableReferenceMethod TableReferenceMethod
        {
            get => (TableReferenceMethod)EditorPrefs.GetInt(k_TableRefMethod, (int)TableReferenceMethod.Guid);
            set => EditorPrefs.SetInt(k_TableRefMethod, (int)value);
        }

        /// <summary>
        /// Adds a reference to a table entry in the Editor.
        /// </summary>
        public static EntryReferenceMethod EntryReferenceMethod
        {
            get => (EntryReferenceMethod)EditorPrefs.GetInt(k_EntryRefMethod, (int)EntryReferenceMethod.Id);
            set => EditorPrefs.SetInt(k_EntryRefMethod, (int)value);
        }

        /// <summary>
        /// Localization modification events that can be used when building editor components.
        /// </summary>
        public static LocalizationEditorEvents EditorEvents { get; internal set; } = new LocalizationEditorEvents();

        internal LocalizationEditorSettings()
        {
            EditorEvents.LocaleSortOrderChanged += (sender, locale) => SortLocales();
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        ~LocalizationEditorSettings()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        /// <summary>
        /// Add the Locale so that it can be used by the Localization system.
        /// </summary>
        /// <example>
        /// This shows how to create a Locale and add it to the project.
        /// <code source="../../DocCodeSamples.Tests/LocalizationEditorSettingsSamples.cs" region="add-locale"/>
        /// </example>
        /// <param name="locale">The <see cref="Locale"/> to add to the project so it can be used by the Localization system.</param>
        /// <param name="createUndo">Used to indicate if an Undo operation should be created.</param>
        public static void AddLocale(Locale locale, bool createUndo = false) => Instance.AddLocaleInternal(locale, createUndo);

        /// <summary>
        /// Removes the locale from the Localization system.
        /// </summary>
        /// <example>
        /// This shows how to remove a Locale from the project.
        /// <code source="../../DocCodeSamples.Tests/LocalizationEditorSettingsSamples.cs" region="remove-locale"/>
        /// </example>
        /// <param name="locale">The <see cref="Locale"/> to remove so that it is no longer used by the Localization system.</param>
        /// <param name="createUndo">Used to indicate if an Undo operation should be created.</param>
        public static void RemoveLocale(Locale locale, bool createUndo = false) => Instance.RemoveLocaleInternal(locale, createUndo);

        /// <summary>
        /// Returns all <see cref="Locale"/> that are part of the Localization system and will be included in the player.
        /// To Add Locales use <seealso cref="AddLocale"/> and <seealso cref="RemoveLocale"/> to remove them.
        /// Note this does not include <see cref="PseudoLocale"/> which can be retrieved by using <seealso cref="GetPseudoLocales"/>.
        /// </summary>
        /// <example>
        /// This example prints the names of the Locales.
        /// <code source="../../DocCodeSamples.Tests/LocalizationEditorSettingsSamples.cs" region="get-locales"/>
        /// </example>
        /// <returns>A collection of all Locales in the project.</returns>
        public static ReadOnlyCollection<Locale> GetLocales() => Instance.GetLocalesInternal();

        /// <summary>
        /// Returns all <see cref="PseudoLocale"/> that are part of the Localization system and will be included in the player.
        /// </summary>
        /// <returns></returns>
        public static ReadOnlyCollection<PseudoLocale> GetPseudoLocales() => Instance.GetPseudoLocalesInternal();

        /// <summary>
        /// Returns the locale that matches the <see cref="LocaleIdentifier"/>"/> in the project.
        /// </summary>
        /// <param name="localeId"></param>
        /// <example>
        /// This example shows how to find a <see cref="Locale"/> using a [SystemLanguage](https://docs.unity3d.com/ScriptReference/SystemLanguage.html) or code.
        /// <code source="../../DocCodeSamples.Tests/LocalizationEditorSettingsSamples.cs" region="get-locale"/>
        /// </example>
        /// <returns>The found <see cref="Locale"/> or null if one could not be found.</returns>
        public static Locale GetLocale(LocaleIdentifier localeId) => Instance.GetLocaleInternal(localeId.Code);

        /// <summary>
        /// Returns all <see cref="StringTableCollection"/> that are in the project.
        /// </summary>
        /// <example>
        /// This example shows how to print out the contents of all the <see cref="StringTableCollection"/> in the project.
        /// <code source="../../DocCodeSamples.Tests/LocalizationEditorSettingsSamples.cs" region="get-string-tables"/>
        /// </example>
        /// <example>
        /// This example adds missing tables for locales in the project.
        /// <code source="../../DocCodeSamples.Tests/LocalizationEditorSettingsSamples.cs" region="add-missing-tables"/>
        /// </example>
        /// <returns></returns>
        public static ReadOnlyCollection<StringTableCollection> GetStringTableCollections() => Instance.GetStringTableCollectionsInternal();

        /// <summary>
        /// Returns a <see cref="StringTableCollection"/> with the matching <see cref="TableReference"/>.
        /// </summary>
        /// <param name="tableNameOrGuid"></param>
        /// <example>
        /// This example shows how to update a collection by adding support for a new Locale.
        /// <code source="../../DocCodeSamples.Tests/LocalizationEditorSettingsSamples.cs" region="get-string-table"/>
        /// </example>
        /// <returns>Found collection or null if one could not be found.</returns>
        public static StringTableCollection GetStringTableCollection(TableReference tableNameOrGuid) => Instance.TableCollectionCache.FindStringTableCollection(tableNameOrGuid);

        /// <summary>
        /// Returns all <see cref="AssetTableCollection"/> assets that are in the project.
        /// </summary>
        /// <example>
        /// This example shows how to print out the contents of all the <see cref="AssetTableCollection"/> in the project.
        /// <code source="../../DocCodeSamples.Tests/LocalizationEditorSettingsSamples.cs" region="get-asset-tables"/>
        /// </example>
        /// <returns></returns>
        public static ReadOnlyCollection<AssetTableCollection> GetAssetTableCollections() => Instance.TableCollectionCache.AssetTableCollections.AsReadOnly();

        /// <summary>
        /// Returns a <see cref="AssetTableCollection"/> with the matching <see cref="TableReference"/>.
        /// </summary>
        /// <param name="tableNameOrGuid"></param>
        /// <example>
        /// This example shows how to update a collection by adding a new localized asset.
        /// <code source="../../DocCodeSamples.Tests/LocalizationEditorSettingsSamples.cs" region="get-asset-table"/>
        /// </example>
        /// <returns>Found collection or null if one could not be found.</returns>
        public static AssetTableCollection GetAssetTableCollection(TableReference tableNameOrGuid) => Instance.TableCollectionCache.FindAssetTableCollection(tableNameOrGuid);

        /// <summary>
        /// Returns the <see cref="LocalizationTableCollection"/> that the table is part of or null if the table has no collection.
        /// </summary>
        /// <param name="table">The table to find the collection for.</param>
        /// <returns>The found collection or null if one could not be found.</returns>
        public static LocalizationTableCollection GetCollectionFromTable(LocalizationTable table) => Instance.TableCollectionCache.FindCollectionForTable(table);

        /// <summary>
        /// Returns the <see cref="LocalizationTableCollection"/> that the <see cref="SharedTableData"/> is part of or null if one could not be found.
        /// </summary>
        /// <param name="sharedTableData">The shared table data to match against a collection.</param>
        /// <returns>The found collection or null if one could not be found.</returns>
        public static LocalizationTableCollection GetCollectionForSharedTableData(SharedTableData sharedTableData) => Instance.TableCollectionCache.FindCollectionForSharedTableData(sharedTableData);

        /// <summary>
        /// If a table does not belong to a <see cref="LocalizationTableCollection"/> then it is considered to be loose, it has no parent collection and will be ignored.
        /// This returns all loose tables that use the same <see cref="SharedTableData"/>, they could then be converted into a <see cref="LocalizationTableCollection"/> using <seealso cref="CreateCollectionFromLooseTables"/>.
        /// </summary>
        /// <param name="sharedTableData"></param>
        /// <param name="foundTables"></param>
        public static void FindLooseStringTablesUsingSharedTableData(SharedTableData sharedTableData, IList<LocalizationTable> foundTables) => Instance.TableCollectionCache.FindLooseTablesUsingSharedTableData(sharedTableData, foundTables);

        /// <summary>
        /// Creates a <see cref="StringTableCollection"/> or <see cref="AssetTableCollection"/> from the provided loose tables.
        /// </summary>
        /// <param name="looseTables">Tables to create the collection from. All tables must be of the same type.</param>
        /// <param name="path">The path to save the new assets to.</param>
        /// <returns>The created <see cref="StringTableCollection"/> or <see cref="AssetTableCollection"/>.</returns>
        public static LocalizationTableCollection CreateCollectionFromLooseTables(IList<LocalizationTable> looseTables, string path) => Instance.CreateCollectionFromLooseTablesInternal(looseTables, path);

        /// <summary>
        /// Creates a <see cref="StringTableCollection"/> using the project Locales.
        /// </summary>
        /// <example>
        /// This example shows how to create a new <see cref="StringTableCollection"/> and add some English values.
        /// <code source="../../DocCodeSamples.Tests/LocalizationEditorSettingsSamples.cs" region="create-string-collection-1"/>
        /// </example>
        /// <param name="tableName">The name of the new collection. Cannot be blank or whitespace, cannot contain invalid filename characters, and cannot contain "[]".</param>
        /// <param name="assetDirectory">The directory to save the generated assets, must be in the project Assets directory.</param>
        /// <returns>The created <see cref="StringTableCollection"/> collection.</returns>
        public static StringTableCollection CreateStringTableCollection(string tableName, string assetDirectory) => CreateStringTableCollection(tableName, assetDirectory, GetLocales());

        /// <summary>
        /// Creates a <see cref="StringTableCollection"/> using the provided Locales.
        /// </summary>
        /// <example>
        /// This example shows how to create a new <see cref="StringTableCollection"/> which contains an English and Japanese <see cref="StringTable"/>.
        /// <code source="../../DocCodeSamples.Tests/LocalizationEditorSettingsSamples.cs" region="create-string-collection-2"/>
        /// </example>
        /// <param name="tableName">The name of the new collection. Cannot be blank or whitespace, cannot contain invalid filename characters, and cannot contain "[]".</param>
        /// <param name="assetDirectory">The directory to save the generated assets, must be in the project Assets directory.</param>
        /// <param name="selectedLocales">The locales to generate the collection with. A <see cref="StringTable"/> will be created for each Locale.</param>
        /// <returns>The created <see cref="StringTableCollection"/> collection.</returns>
        public static StringTableCollection CreateStringTableCollection(string tableName, string assetDirectory, IList<Locale> selectedLocales) => Instance.CreateCollection(typeof(StringTableCollection), tableName, assetDirectory, selectedLocales) as StringTableCollection;

        /// <summary>
        /// Creates a <see cref="AssetTableCollection"/> using the project Locales.
        /// </summary>
        /// <example>
        /// This example shows how to update a collection by adding a new localized asset.
        /// <code source="../../DocCodeSamples.Tests/LocalizationEditorSettingsSamples.cs" region="get-asset-table"/>
        /// </example>
        /// <param name="tableName">The Table Collection Name to use.</param>
        /// <param name="assetDirectory">The directory to store the generated assets.</param>
        /// <returns>The created <see cref="AssetTableCollection"/> collection.</returns>
        public static AssetTableCollection CreateAssetTableCollection(string tableName, string assetDirectory) => CreateAssetTableCollection(tableName, assetDirectory, GetLocales());

        /// <summary>
        /// Creates a <see cref="AssetTableCollection"/> using the provided Locales.
        /// </summary>
        /// <param name="tableName">The name of the new collection. Cannot be blank or whitespace, cannot contain invalid filename characters, and cannot contain "[]".</param>
        /// <param name="assetDirectory">The directory to store the generated assets.</param>
        /// <param name="selectedLocales">The locales to generate the collection with. A <see cref="AssetTable"/> will be created for each Locale</param>
        /// <returns>The created <see cref="AssetTableCollection"/> collection.</returns>
        public static AssetTableCollection CreateAssetTableCollection(string tableName, string assetDirectory, IList<Locale> selectedLocales) => Instance.CreateCollection(typeof(AssetTableCollection), tableName, assetDirectory, selectedLocales) as AssetTableCollection;

        /// <summary>
        /// Adds or Remove the preload flag for the selected table.
        /// </summary>
        /// <example>
        /// This example shows how to set the preload flag for a single collection.
        /// <code source="../../DocCodeSamples.Tests/LocalizationEditorSettingsSamples.cs" region="set-preload-flag"/>
        /// </example>
        /// <param name="table">The table to mark as preload.</param>
        /// <param name="preload"><see langword="true"/> ifd the table should be preloaded or <see langword="false"/> if it should be loaded on demand.</param>
        /// <param name="createUndo">Should an Undo record be created?</param>
        public static void SetPreloadTableFlag(LocalizationTable table, bool preload, bool createUndo = false)
        {
            Instance.SetPreloadTableInternal(table, preload, createUndo);
        }

        /// <summary>
        /// Returns <see langword="true"/> if the table is marked for preloading.
        /// </summary>
        /// <example>
        /// This example shows how to query if a table is marked as preload.
        /// <code source="../../DocCodeSamples.Tests/LocalizationEditorSettingsSamples.cs" region="get-preload-flag"/>
        /// </example>
        /// <param name="table">The table to query.</param>
        /// <returns><see langword="true"/> if preloading is enable otherwise <see langword="false"/>.</returns>
        public static bool GetPreloadTableFlag(LocalizationTable table)
        {
            // TODO: We could just use the instance id so we dont need to load the whole table
            return Instance.GetPreloadTableFlagInternal(table);
        }

        /// <summary>
        /// Returns the <see cref="AssetTableCollection"/> that contains a table entry with the closest match to the provided text.
        /// Uses the Levenshtein distance method.
        /// </summary>
        /// <param name="keyName"></param>
        /// <returns></returns>
        [Obsolete("FindSimilarKey will be removed in the future, please use Unity Search. See TableEntrySearchData class for further details.")]
        public static (StringTableCollection collection, SharedTableData.SharedTableEntry entry, int matchDistance) FindSimilarKey(string keyName)
        {
            throw new NotSupportedException("FindSimilarKey is obsolete. please use Unity Search instead.");
        }

        internal static void RefreshEditorPreview()
        {
            // Only update the preview in edit mode (LOC-1025)
            if (LocalizationSettings.Instance.IsPlayingOrWillChangePlaymode)
                return;

            if (ActiveLocalizationSettings != null && ActiveLocalizationSettings.GetSelectedLocale() != null)
            {
                LocalizationPropertyDriver.UnregisterProperties();
                VariantsPropertyDriver.UnregisterProperties();

                ActiveLocalizationSettings.SendLocaleChangedEvents(LocalizationSettings.SelectedLocale);
            }
        }

        internal string GetUniqueTableCollectionName(Type collectionType, string name)
        {
            int suffix = 1;
            var nameToTest = name;
            while (true)
            {
                if (TableCollectionCache.FindTableCollection(collectionType, nameToTest) == null)
                    return nameToTest;

                nameToTest = $"{name} {suffix}";
                suffix++;
            }
        }

        internal virtual LocalizationTableCollection CreateCollection(Type collectionType, string tableName, string assetDirectory, IList<Locale> selectedLocales)
        {
            if (!typeof(LocalizationTableCollection).IsAssignableFrom(collectionType))
                throw new ArgumentException($"{collectionType.Name} Must be derived from {nameof(LocalizationTableCollection)}", nameof(collectionType));
            if (string.IsNullOrEmpty(assetDirectory))
                throw new ArgumentException("Must not be null or empty.", nameof(assetDirectory));
            var tableNameError = IsTableNameValid(collectionType, tableName);
            if (tableNameError != null)
            {
                throw new ArgumentException(tableNameError, nameof(tableName));
            }

            var collection = ScriptableObject.CreateInstance(collectionType) as LocalizationTableCollection;

            AssetDatabase.StartAssetEditing();

            // TODO: Check that no tables already exist with the same name, locale and type.
            var relativePath = PathHelper.MakePathRelative(assetDirectory);
            Directory.CreateDirectory(relativePath);

            var sharedDataPath = Path.Combine(relativePath, AddressHelper.GetSharedTableAddress(tableName) + ".asset");
            var sharedTableData = ScriptableObject.CreateInstance<SharedTableData>();
            sharedTableData.TableCollectionName = tableName;
            CreateAsset(sharedTableData, sharedDataPath);
            collection.SharedData = sharedTableData;
            collection.AddSharedTableDataToAddressables();

            // Extract the SharedTableData Guid and assign it so we can use it as a unique id for the table collection name.
            var sharedDataGuid = GetAssetGuid(sharedTableData);
            sharedTableData.TableCollectionNameGuid = Guid.Parse(sharedDataGuid);
            EditorUtility.SetDirty(sharedTableData); // We need to set it dirty so the change to TableCollectionNameGuid is saved.

            if (selectedLocales?.Count > 0)
            {
                var createdTables = new List<LocalizationTable>(selectedLocales.Count);
                foreach (var locale in selectedLocales)
                {
                    var table = ScriptableObject.CreateInstance(collection.TableType) as LocalizationTable;
                    table.SharedData = sharedTableData;
                    table.LocaleIdentifier = locale.Identifier;
                    table.name = AddressHelper.GetTableAddress(tableName, locale.Identifier);
                    createdTables.Add(table);
                }

                for (int i = 0; i < createdTables.Count; ++i)
                {
                    var tbl = createdTables[i];

                    var assetPath = Path.Combine(relativePath, tbl.name + ".asset");
                    assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
                    CreateAsset(tbl, assetPath);
                    collection.AddTable(tbl, postEvent: false);
                }
            }

            // Save the collection
            collection.name = tableName;
            var collectionPath = Path.Combine(relativePath, collection.name + ".asset");
            CreateAsset(collection, collectionPath);

            AssetDatabase.StopAssetEditing();

            return collection;
        }

        internal virtual LocalizationTableCollection CreateCollectionFromLooseTablesInternal(IList<LocalizationTable> looseTables, string path)
        {
            if (looseTables == null || looseTables.Count == 0)
                return null;

            var isStringTable = looseTables[0] is StringTable;

            var collectionType = isStringTable ? typeof(StringTableCollection) : typeof(AssetTableCollection);
            var collection = ScriptableObject.CreateInstance(collectionType) as LocalizationTableCollection;
            collection.SharedData = looseTables[0].SharedData;

            foreach (var table in looseTables)
            {
                if (table.SharedData != collection.SharedData)
                {
                    Debug.LogError($"Table {table.name} does not share the same Shared Table Data and can not be part of the new collection", table);
                    continue;
                }
                collection.AddTable(table, postEvent: false); // Don't post the event, we will send the Collection added only event
            }

            var relativePath = PathHelper.MakePathRelative(path);
            CreateAsset(collection, relativePath);
            EditorEvents.RaiseCollectionAdded(collection);
            return collection;
        }

        internal virtual LocalizationSettings ActiveLocalizationSettingsInternal
        {
            get
            {
                EditorBuildSettings.TryGetConfigObject(LocalizationSettings.ConfigName, out LocalizationSettings settings);
                return settings;
            }
            set
            {
                if (value == null)
                {
                    EditorBuildSettings.RemoveConfigObject(LocalizationSettings.ConfigName);
                }
                else
                {
                    EditorBuildSettings.AddConfigObject(LocalizationSettings.ConfigName, value, true);
                }
            }
        }

        internal virtual AddressableAssetSettings GetAddressableAssetSettings(bool create)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(create);
            if (settings != null)
                return settings;

            // By default Addressables wont return the settings if updating or compiling. This causes issues for us, especially if we are trying to get the Locales.
            // We will just ignore this state and try to get the settings regardless.
            if (EditorApplication.isUpdating || EditorApplication.isCompiling)
            {
                // Legacy support
                if (EditorBuildSettings.TryGetConfigObject(AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, out settings))
                {
                    return settings;
                }

                AddressableAssetSettingsDefaultObject so;
                if (EditorBuildSettings.TryGetConfigObject(AddressableAssetSettingsDefaultObject.kDefaultConfigObjectName, out so))
                {
                    // Extract the guid
                    var serializedObject = new SerializedObject(so);
                    var guid = serializedObject.FindProperty("m_AddressableAssetSettingsGuid")?.stringValue;
                    if (!string.IsNullOrEmpty(guid))
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        return AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(path);
                    }
                }
            }
            return null;
        }

        internal virtual AddressableAssetEntry GetAssetEntry(Object asset) => GetAssetEntry(asset.GetInstanceID());

        internal virtual AddressableAssetEntry GetAssetEntry(int instanceId)
        {
            var settings = GetAddressableAssetSettings(false);
            if (settings == null)
                return null;

            var guid = GetAssetGuid(instanceId);
            return settings.FindAssetEntry(guid);
        }

        internal virtual string FindUniqueAssetAddress(string address)
        {
            var aaSettings = GetAddressableAssetSettings(false);
            if (aaSettings == null)
                return address;

            var validAddress = address;
            var index = 1;
            var foundExisting = true;
            while (foundExisting)
            {
                if (index > 1000)
                {
                    Debug.LogError("Unable to create valid address for new Addressable Asset.");
                    return address;
                }
                foundExisting = false;
                foreach (var g in aaSettings.groups)
                {
                    if (g.Name == validAddress)
                    {
                        foundExisting = true;
                        validAddress = address + index;
                        index++;
                        break;
                    }
                }
            }

            return validAddress;
        }

        internal virtual void AddLocaleInternal(Locale locale, bool createUndo)
        {
            if (locale == null)
                throw new ArgumentNullException(nameof(locale));

            if (!EditorUtility.IsPersistent(locale))
                throw new AssetNotPersistentException(locale);

            var aaSettings = GetAddressableAssetSettings(true);
            if (aaSettings == null)
                return;

            using (new UndoScope("Add Locale", createUndo))
            {
                var assetEntry = AddressableGroupRules.AddLocaleToGroup(locale, aaSettings, createUndo);
                assetEntry.address = locale.LocaleName;

                // Clear the locales cache.
                m_ProjectLocales = null;
                m_ProjectPseudoLocales = null;
                if (!LocalizationSettings.Instance.IsPlayingOrWillChangePlaymode)
                    LocalizationSettings.Instance.ResetState();

                if (!assetEntry.labels.Contains(LocalizationSettings.LocaleLabel))
                {
                    if (createUndo)
                        Undo.RecordObjects(new Object[] { aaSettings, assetEntry.parentGroup }, "Add locale");
                    assetEntry.SetLabel(LocalizationSettings.LocaleLabel, true, true);
                    EditorEvents.RaiseLocaleAdded(locale);
                }
            }
        }

        internal virtual void RemoveLocaleInternal(Locale locale, bool createUndo)
        {
            // Clear the locale cache
            m_ProjectLocales = null;
            m_ProjectPseudoLocales = null;
            if (!LocalizationSettings.Instance)
                LocalizationSettings.Instance.ResetState();

            var aaSettings = GetAddressableAssetSettings(false);
            if (aaSettings == null)
                return;

            var localeAssetEntry = GetAssetEntry(locale);
            if (localeAssetEntry == null)
                return;

            using (new UndoScope("Remove locale", createUndo))
            {
                if (createUndo)
                    Undo.RecordObjects(new Object[] { aaSettings, localeAssetEntry.parentGroup }, "Remove locale");

                aaSettings.RemoveAssetEntry(localeAssetEntry.guid);
                EditorEvents.RaiseLocaleRemoved(locale);
            }
        }

        internal virtual ReadOnlyCollection<Locale> GetLocalesInternal()
        {
            if (m_ProjectLocales != null)
                return m_ProjectLocales;

            var foundLocales = new List<Locale>();

            var aaSettings = GetAddressableAssetSettings(false);
            if (aaSettings == null)
                return new ReadOnlyCollection<Locale>(foundLocales);

            var foundAssets = new List<AddressableAssetEntry>();
            aaSettings.GetAllAssets(foundAssets, false, group => group != null, entry =>
            {
                return entry.labels.Contains(LocalizationSettings.LocaleLabel);
            });

            foreach (var localeAddressable in foundAssets)
            {
                if (!string.IsNullOrEmpty(localeAddressable.guid))
                {
                    var locale = AssetDatabase.LoadAssetAtPath<Locale>(AssetDatabase.GUIDToAssetPath(localeAddressable.guid));
                    if (locale != null && !(locale is PseudoLocale)) // Dont include Pseudo locales.
                        foundLocales.Add(locale);
                }
            }

            foundLocales.Sort();
            m_ProjectLocales = new ReadOnlyCollection<Locale>(foundLocales);
            return m_ProjectLocales;
        }

        internal virtual ReadOnlyCollection<PseudoLocale> GetPseudoLocalesInternal()
        {
            if (m_ProjectPseudoLocales == null)
                CollectProjectLocales();
            return m_ProjectPseudoLocales;
        }

        void CollectProjectLocales()
        {
            var foundLocales = new List<Locale>();
            var foundPseudoLocales = new List<PseudoLocale>();

            var aaSettings = GetAddressableAssetSettings(false);
            if (aaSettings != null)
            {
                var foundAssets = new List<AddressableAssetEntry>();
                aaSettings.GetAllAssets(foundAssets, false, group => group != null, entry =>
                {
                    return entry.labels.Contains(LocalizationSettings.LocaleLabel);
                });

                foreach (var localeAddressable in foundAssets)
                {
                    if (localeAddressable.MainAsset != null && localeAddressable.MainAsset is Locale locale)
                    {
                        if (locale is PseudoLocale pseudoLocale)
                        {
                            foundPseudoLocales.Add(pseudoLocale);
                        }
                        else
                        {
                            foundLocales.Add(locale);
                        }
                    }
                }
            }

            foundLocales.Sort();
            foundPseudoLocales.Sort();
            m_ProjectLocales = foundLocales.AsReadOnly();
            m_ProjectPseudoLocales = foundPseudoLocales.AsReadOnly();
        }

        internal virtual ReadOnlyCollection<StringTableCollection> GetStringTableCollectionsInternal() => Instance.TableCollectionCache.StringTableCollections.AsReadOnly();

        Locale GetLocaleInternal(string code) => GetLocalesInternal()?.FirstOrDefault(loc => loc.Identifier.Code == code);

        void SortLocales()
        {
            if (m_ProjectLocales != null)
            {
                var localesList = m_ProjectLocales.ToList();
                localesList.Sort();
                m_ProjectLocales = localesList.AsReadOnly();
            }

            if (m_ProjectPseudoLocales != null)
            {
                var pseudoLocalesList = m_ProjectPseudoLocales.ToList();
                pseudoLocalesList.Sort();
                m_ProjectPseudoLocales = pseudoLocalesList.AsReadOnly();
            }
        }

        internal virtual void SetPreloadTableInternal(LocalizationTable table, bool preload, bool createUndo = false)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table), "Can not set preload flag on a null table");

            var aaSettings = GetAddressableAssetSettings(true);
            if (aaSettings == null)
                return;

            var tableEntry = GetAssetEntry(table);
            if (tableEntry == null)
                throw new AddressableEntryNotFoundException(table);

            if (createUndo)
                Undo.RecordObjects(new Object[] { aaSettings, tableEntry.parentGroup }, "Set Preload flag");

            tableEntry.SetLabel(LocalizationSettings.PreloadLabel, preload, preload);
        }

        internal virtual bool GetPreloadTableFlagInternal(LocalizationTable table)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table), "Can not get preload flag from a null table");

            var aaSettings = GetAddressableAssetSettings(false);
            if (aaSettings == null)
                return false;

            var tableEntry = GetAssetEntry(table);
            if (tableEntry == null)
                throw new AddressableEntryNotFoundException(table);

            return tableEntry.labels.Contains(LocalizationSettings.PreloadLabel);
        }

        void UndoRedoPerformed()
        {
            // Reset the locales as adding/removing a locale may have been undone.
            m_ProjectLocales = null;
        }

        internal virtual void CreateAsset(Object asset, string path)
        {
            AssetDatabase.CreateAsset(asset, path);
        }

        internal string GetAssetGuid(int instanceId)
        {
            Debug.Assert(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(instanceId, out string guid, out long _), "Failed to extract the asset Guid");
            return guid;
        }

        internal string GetAssetGuid(Object asset)
        {
            Debug.Assert(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long _), "Failed to extract the asset Guid", asset);
            return guid;
        }

        internal string IsTableNameValid(Type collectionType, string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return "Table collection name cannot be blank or whitespace";
            }

            if (tableName != tableName.Trim())
            {
                return "Table collection name cannot contain leading or trailing whitespace";
            }

            // Addressables restriction
            if (tableName.Contains('[') && tableName.Contains(']'))
            {
                return "Table collection name cannot contain both '[' and ']'";
            }

            var values = k_InvalidFileNameChars.Intersect(tableName).ToList();
            if (values.Any())
            {
                return $"Table collection name cannot contain invalid filename characters but contains '{string.Join(", ", values)}'";
            }

            if (TableCollectionCache.FindTableCollection(collectionType, tableName) != null)
            {
                return $"{collectionType.Name} with name '{tableName}' already exists";
            }

            return null;
        }
    }
}
