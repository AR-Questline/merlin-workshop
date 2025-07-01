using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Localization.Addressables;
using UnityEditor.Localization.UI;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.Pool;

namespace UnityEditor.Localization
{
    /// <summary>
    /// Editor interface to a collection of tables which all share the same <see cref="SharedTableData"/>.
    /// </summary>
    public abstract class LocalizationTableCollection : ScriptableObject, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Represents a single Key and its localized values when using GetRowEnumerator.
        /// </summary>
        /// <typeparam name="TEntry"></typeparam>
        public class Row<TEntry> where TEntry : TableEntry
        {
            /// <summary>
            /// The <see cref="LocaleIdentifier"/> for each table value in <see cref="TableEntries"/>.
            /// The order of the tables is guaranteed not to change.
            /// </summary>
            public LocaleIdentifier[] TableEntriesReference { get; internal set; }

            /// <summary>
            /// The Key for the current row.
            /// </summary>
            public SharedTableData.SharedTableEntry KeyEntry { get; internal set; }

            /// <summary>
            /// The entries taken from all the tables for the current <see cref="KeyEntry"/>.
            /// The value may be null, such as when the table does not have a value for the current key.
            /// </summary>
            public TEntry[] TableEntries { get; internal set; }
        }

        [SerializeField]
        LazyLoadReference<SharedTableData> m_SharedTableData;

        [SerializeField]
        List<LazyLoadReference<LocalizationTable>> m_Tables = new List<LazyLoadReference<LocalizationTable>>();

        [SerializeReference]
        List<CollectionExtension> m_Extensions = new List<CollectionExtension>();

        [SerializeField]
        string m_Group;

        ReadOnlyCollection<LazyLoadReference<LocalizationTable>> m_ReadOnlyTables;
        ReadOnlyCollection<CollectionExtension> m_ReadOnlyExtensions;

        /// <summary>
        /// The type of table stored in the collection.
        /// </summary>
        protected internal abstract Type TableType { get; }

        /// <summary>
        /// The required attribute for an extension to be added to this collection through the Editor.
        /// </summary>
        protected internal abstract Type RequiredExtensionAttribute { get; }

        /// <summary>
        /// Removes the entry from the <see cref="SharedTableData"/> and all tables that are part of this collection.
        /// </summary>
        /// <param name="entryReference"></param>
        public abstract void RemoveEntry(TableEntryReference entryReference);

        /// <summary>
        /// The default value to use for <see cref="Group"/>.
        /// </summary>
        protected internal abstract string DefaultGroupName { get; }
        internal (bool valid, string error) IsValid
        {
            get
            {
                if (SharedData != null)
                    return (true, null);
                return (false, "SharedTableData is null");
            }
        }

        /// <summary>
        /// All tables that are part of this collection.
        /// Tables are stored as LazyLoadReferences so that they only load when required and not when the collection loads.
        /// </summary>
        public ReadOnlyCollection<LazyLoadReference<LocalizationTable>> Tables
        {
            get
            {
                if (m_ReadOnlyTables == null)
                {
                    RemoveBrokenTables();
                    m_ReadOnlyTables = m_Tables.AsReadOnly();
                }
                return m_ReadOnlyTables;
            }
        }

        /// <summary>
        /// Extensions attached to the collection. Extensions can be used to attach additional data or functionality to a collection.
        /// </summary>
        public virtual ReadOnlyCollection<CollectionExtension> Extensions
        {
            get
            {
                if (m_ReadOnlyExtensions == null)
                    m_ReadOnlyExtensions = m_Extensions.AsReadOnly();
                return m_ReadOnlyExtensions;
            }
        }

        /// <summary>
        /// The name of this collection of Tables.
        /// </summary>
        public virtual string TableCollectionName { get => SharedData.TableCollectionName; set => SharedData.TableCollectionName = value; }

        /// <summary>
        /// Reference to use to refer to this table collection.
        /// </summary>
        public TableReference TableCollectionNameReference => SharedData.TableCollectionNameGuid;

        /// <summary>
        /// The <see cref="SharedTableData"/> that is used by all tables in this collection.
        /// </summary>
        public virtual SharedTableData SharedData { get => m_SharedTableData.asset; internal set => m_SharedTableData.asset = value; }

        /// <summary>
        /// Collections can be added to groups which will be used when showing the list of collections in the Localization Window.
        /// For example all collections with the Group "UI" would be shown under a UI menu in the `Selected Table Collection` field.
        /// </summary>
        public string Group { get => m_Group; set => m_Group = value; }

        protected virtual void OnEnable()
        {
            if (string.IsNullOrEmpty(m_Group))
            {
                // Default group if one is not set
                m_Group = DefaultGroupName;
            }
        }

        /// <summary>
        /// Changes the table collection name.
        /// This will change <see cref="SharedTableData.TableCollectionName"/> and update the Addressables data for all tables within the collection.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="createUndo"></param>
        public void SetTableCollectionName(string name, bool createUndo = false)
        {
            SetTableCollectionNameInternal(name, createUndo);
        }

        // virtual for testing
        internal virtual void SetTableCollectionNameInternal(string name, bool createUndo)
        {
            if (name == TableCollectionName)
                return;
            var tableNameError = LocalizationEditorSettings.Instance.IsTableNameValid(GetType(), name);
            if (tableNameError != null)
            {
                throw new ArgumentException(tableNameError, nameof(name));
            }

            var undoGroup = Undo.GetCurrentGroup();
            if (createUndo)
                Undo.RecordObject(SharedData, "Change Table Collection Name");

            EditorUtility.SetDirty(SharedData);
            var OldTableCollectionName = SharedData.TableCollectionName;
            SharedData.TableCollectionName = name;
            RefreshAddressables(createUndo);
            RefreshAssetNames(OldTableCollectionName);

            if (createUndo)
                Undo.CollapseUndoOperations(undoGroup);
        }

        void RefreshAssetNames(string OldTableCollectionName)
        {
            if (SharedData == null || SharedData.TableCollectionName == OldTableCollectionName)
                return;

            //Change Table Name
            foreach (var table in m_Tables)
            {
                ObjectNames.SetNameSmart(table.asset, AddressHelper.GetTableAddress(table.asset.TableCollectionName, table.asset.LocaleIdentifier));
            }

            //change Localization TableCollection Name
            ObjectNames.SetNameSmart(this, SharedData.TableCollectionName);

            //change SharedData TableCollection name.
            ObjectNames.SetNameSmart(SharedData, AddressHelper.GetSharedTableAddress(SharedData.TableCollectionName));
            EditorUtility.SetDirty(SharedData);
        }

        /// <summary>
        /// Sets the preload flag for all tables in this collection.
        /// </summary>
        /// <param name="preload">Should the tables be preloaded? True for preloading or false to load on demand.</param>
        /// <param name="createUndo">Create an undo point?</param>
        public virtual void SetPreloadTableFlag(bool preload, bool createUndo = false)
        {
            RemoveBrokenTables();
            foreach (var table in m_Tables)
            {
                LocalizationEditorSettings.SetPreloadTableFlag(table.asset, preload, createUndo);
            }
        }

        /// <summary>
        /// Are the tables in the collection set to preload?
        /// </summary>
        /// <returns>True if <b>all</b> tables are set to preload else false.</returns>
        public virtual bool IsPreloadTableFlagSet()
        {
            RemoveBrokenTables();
            return m_Tables.Count > 0 && m_Tables.TrueForAll(tbl => LocalizationEditorSettings.GetPreloadTableFlag(tbl.asset));
        }

        /// <summary>
        /// Adds the table to the collection and updates Addressable assets.
        /// The table will not be added if it is not using the same <see cref="SharedTableData"/> as the collection.
        /// </summary>
        /// <param name="table">The table to add to the collection.</param>
        /// <param name="createUndo">Should an Undo operation be created?</param>
        /// <param name="postEvent">Should the <see cref="LocalizationEditorEvents.TableAddedToCollection"/> event be sent after the table was added?</param>
        public virtual void AddTable(LocalizationTable table, bool createUndo = false, bool postEvent = true)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            if (!EditorUtility.IsPersistent(table))
                throw new AssetNotPersistentException(table);

            if (table.SharedData != SharedData)
                throw new Exception($"Can not add table {table}, it has different Shared Data. Table uses {table.SharedData.name} but collection uses {SharedData.name}");

            if (!table.GetType().IsAssignableFrom(TableType))
                return;

            using (new UndoScope("Add table to collection", createUndo))
            {
                if (createUndo)
                    Undo.RegisterCompleteObjectUndo(this, "Add table to collection");

                AddTableToAddressables(table, createUndo);

                // We always run to this point in case we need to fix Addressable issues.
                // We only send the event if the table has been added for the first time though.
                if (!m_Tables.Any(tbl => tbl.asset == table || tbl.asset?.LocaleIdentifier == table.LocaleIdentifier))
                {
                    //Setting the PreloadTableFlag true if PreladAll is set true
                    LocalizationEditorSettings.SetPreloadTableFlag(table, IsPreloadTableFlagSet());

                    m_Tables.Add(new LazyLoadReference<LocalizationTable> { asset = table });

                    // We need to SetDirty after AddTableToAddressables as AddTableToAddressables may call
                    // SaveAssets which would reset the dirty state before we have finished making changes.
                    EditorUtility.SetDirty(this);

                    if (postEvent)
                        LocalizationEditorSettings.EditorEvents.RaiseTableAddedToCollection(this, table);
                }

                if (postEvent)
                    LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(this, this);
            }
        }

        /// <summary>
        /// Creates a table in the collection.
        /// </summary>
        /// <param name="localeIdentifier"></param>
        /// <returns>>The newly created table.</returns>
        public virtual LocalizationTable AddNewTable(LocaleIdentifier localeIdentifier)
        {
            var defaultDirectory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(this));
            var relativePath = PathHelper.MakePathRelative(defaultDirectory);
            var tableName = AddressHelper.GetTableAddress(TableCollectionName, localeIdentifier);
            var path = Path.Combine(relativePath, tableName + ".asset");
            return AddNewTable(localeIdentifier, path);
        }

        /// <summary>
        /// Creates a table in the collection.
        /// </summary>
        /// <param name="localeIdentifier"></param>
        /// <param name="path"></param>
        /// <returns>>The newly created table.</returns>
        public virtual LocalizationTable AddNewTable(LocaleIdentifier localeIdentifier, string path)
        {
            if (ContainsTable(localeIdentifier))
                throw new Exception("Can not add new table. The same LocaleIdentifier is already in use.");

            LocalizationTable table;
            if (File.Exists(path))
                table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(path);
            else
            {
                table = CreateInstance(TableType) as LocalizationTable;
                table.LocaleIdentifier = localeIdentifier;
                table.SharedData = SharedData;

                LocalizationEditorSettings.Instance.CreateAsset(table, path);
            }
            AddTable(table);
            return table;
        }

        /// <summary>
        /// Removes the table from the collection and updates Addressables assets.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="createUndo"></param>
        /// <param name="postEvent">Should the <see cref="LocalizationEditorEvents.TableRemovedFromCollection"/> event be sent after the table has been removed?</param>
        public virtual void RemoveTable(LocalizationTable table, bool createUndo = false, bool postEvent = true)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));

            // We use the instance id so as not to force the tables to be loaded.
            var tableInstanceID = table.GetInstanceID();
            var index = m_Tables.FindIndex(t => t.GetInstanceId() == tableInstanceID);

            if (index == -1)
                return;

            if (createUndo)
                Undo.RecordObject(this, "Remove table from collection");

            RemoveTableFromAddressables(table, false);
            m_Tables.RemoveAt(index);
            EditorUtility.SetDirty(this);
            if (postEvent)
                LocalizationEditorSettings.EditorEvents.RaiseTableRemovedFromCollection(this, table);
        }

        /// <summary>
        /// Removes all the entries from <see cref="SharedTableData"/> and all <see cref="Tables"/> that are part of this collection.
        /// </summary>
        public virtual void ClearAllEntries()
        {
            // Clear all keys
            if (SharedData != null)
            {
                SharedData.Clear();
                EditorUtility.SetDirty(SharedData);
            }

            EditorUtility.SetDirty(this);
            LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(this, this);
        }

        /// <summary>
        /// Returns the table with the matching <see cref="LocaleIdentifier"/>.
        /// </summary>
        /// <param name="localeIdentifier">The locale identifier that the table must have.</param>
        /// <returns>The table with the matching <see cref="LocaleIdentifier"/> or null if one does not exist in the collection.</returns>
        public virtual LocalizationTable GetTable(LocaleIdentifier localeIdentifier)
        {
            foreach (var tbl in m_Tables)
            {
                if (tbl.asset?.LocaleIdentifier == localeIdentifier)
                    return tbl.asset;
            }
            return null;
        }

        // We use this so we can enumerate all the tables and mock it in tests.
        // LazyLoadReference only works with perssistent assets which makes testing temporary assets hard so we use this instead.
        internal virtual IEnumerable<LocalizationTable> GetTableEnumerator()
        {
            foreach (var table in m_Tables)
            {
                yield return table.asset;
            }
        }

        /// <summary>
        /// Forces Addressables data to be updated for this collection.
        /// This will ensure that <see cref="SharedData"/> and <see cref="Tables"/> are both part of Addressables and correctly labeled.
        /// </summary>
        /// <param name="createUndo"></param>
        public void RefreshAddressables(bool createUndo = false)
        {
            RemoveBrokenTables();
            AddSharedTableDataToAddressables();

            using (new UndoScope("Add table to collection", createUndo))
            {
                foreach (var table in m_Tables)
                {
                    AddTableToAddressables(table.asset, createUndo);
                }
            }
        }

        /// <summary>
        /// Checks if a table with the same instance Id exists in the collection.
        /// This check should be fast as a table does not need to be loaded to have its instance Id compared.
        /// </summary>
        /// <param name="table">The table to look for.</param>
        /// <returns></returns>
        public virtual bool ContainsTable(LocalizationTable table)
        {
            // We use the instance id so as not to force the tables to be loaded.
            var tableInstanceID = table.GetInstanceID();
            return m_Tables.Any(t => t.GetInstanceId() == tableInstanceID);
        }

        /// <summary>
        /// Checks if a table with the same <see cref="LocaleIdentifier"/> exists in the collection.
        /// </summary>
        /// <param name="localeIdentifier">The Id to match against.</param>
        /// <returns></returns>
        public bool ContainsTable(LocaleIdentifier localeIdentifier) => GetTable(localeIdentifier) != null;

        /// <summary>
        /// Attaches the provided extension to the collection.
        /// </summary>
        /// <param name="extension">The extension to add to the collection.</param>
        /// <exception cref="ArgumentException">Thrown if the extension does not have correct attribute.</exception>
        public void AddExtension(CollectionExtension extension)
        {
            if (extension == null)
                throw new ArgumentNullException(nameof(extension));

            if (!Attribute.IsDefined(extension.GetType(), RequiredExtensionAttribute))
                throw new ArgumentException($"Can not add extension. It requires the Attribute {RequiredExtensionAttribute}.", nameof(extension));

            extension.TargetCollection = this;
            m_Extensions.Add(extension);
            extension.Initialize();
            LocalizationEditorSettings.EditorEvents.RaiseExtensionAddedToCollection(this, extension);
        }

        /// <summary>
        /// Removes the extension from <see cref="Extensions"/>.
        /// </summary>
        /// <param name="extension">The extension to remove from the collection.</param>
        /// <exception cref="ArgumentNullException">Thrown if the extension is null.</exception>
        public void RemoveExtension(CollectionExtension extension)
        {
            if (extension == null)
                throw new ArgumentNullException(nameof(extension));

            m_Extensions.Remove(extension);
            extension.Destroy();
            extension.TargetCollection = null;
            LocalizationEditorSettings.EditorEvents.RaiseExtensionRemovedFromCollection(this, extension);
        }

        internal void SaveChangesToDisk()
        {
            #if ENABLE_SAVE_ASSET_IF_DIRTY // Added in 2020.3.16 and 2021.2
            foreach (var tbl in m_Tables)
            {
                AssetDatabase.SaveAssetIfDirty(tbl.asset);
            }
            AssetDatabase.SaveAssetIfDirty(SharedData);
            #else
            AssetDatabase.SaveAssets();
            #endif
        }

        /// <summary>
        /// Returns an enumerable for stepping through the rows of the collection. Sorted by the <see cref="SharedData"/> entry Ids.
        /// </summary>
        /// <typeparam name="TTable"></typeparam>
        /// <typeparam name="TEntry"></typeparam>
        /// <param name="tables"></param>
        /// <returns></returns>
        protected static IEnumerable<Row<TEntry>> GetRowEnumerator<TTable, TEntry>(IEnumerable<TTable> tables)
            where TTable : DetailedLocalizationTable<TEntry>
            where TEntry : TableEntry
        {
            if (tables == null)
                throw new ArgumentNullException(nameof(tables));

            SharedTableData sharedTableData = null;

            // Prepare the tables - Sort the keys and table entries
            var sortedTableEntries = new List<IOrderedEnumerable<TEntry>>();
            foreach (var table in tables)
            {
                if (sharedTableData == null)
                {
                    sharedTableData = table.SharedData;
                }
                else if (sharedTableData != table.SharedData)
                {
                    throw new Exception("All tables must share the same SharedData.");
                }

                if (table != null)
                {
                    var s = table.Values.OrderBy(e => e.KeyId);
                    sortedTableEntries.Add(s);
                }
            }

            var sortedKeyEntries = sharedTableData.Entries.OrderBy(e => e.Id);

            var currentTableRowIterator = sortedTableEntries.Select(o =>
            {
                var itr = o.GetEnumerator();
                itr.MoveNext();
                return itr;
            }).ToArray();

            var currentRow = new Row<TEntry>
            {
                TableEntriesReference = tables.Select(t => t.LocaleIdentifier).ToArray(),
                TableEntries = new TEntry[sortedTableEntries.Count]
            };

            using (StringBuilderPool.Get(out var warningMessage))
            {
                // Extract the table row values for this key.
                // If the table has a key value then add it to currentTableRow otherwise use null.
                foreach (var keyRow in sortedKeyEntries)
                {
                    currentRow.KeyEntry = keyRow;

                    // Extract the string table entries for this row
                    for (int i = 0; i < currentRow.TableEntries.Length; ++i)
                    {
                        var tableRowItr = currentTableRowIterator[i];

                        // Skip any table entries that may not not exist in Shared Data
                        while (tableRowItr != null && tableRowItr.Current?.KeyId < keyRow.Id)
                        {
                            warningMessage.AppendLine($"{tableRowItr.Current.Table.name} - {tableRowItr.Current.KeyId} - {tableRowItr.Current.Data.Localized}");
                            if (!tableRowItr.MoveNext())
                            {
                                currentTableRowIterator[i] = null;
                                break;
                            }
                        }

                        if (tableRowItr?.Current?.KeyId == keyRow.Id)
                        {
                            currentRow.TableEntries[i] = tableRowItr.Current;
                            if (!tableRowItr.MoveNext())
                            {
                                currentTableRowIterator[i] = null;
                            }
                        }
                        else
                        {
                            currentRow.TableEntries[i] = null;
                        }
                    }

                    yield return currentRow;
                }

                // Any warning messages?
                if (warningMessage.Length > 0)
                {
                    warningMessage.Insert(0, "Found entries in Tables that were missing a Shared Table Data Entry. These entries were ignored:\n");
                    Debug.LogWarning(warningMessage.ToString(), sharedTableData);
                }
            }
        }

        /// <summary>
        /// Returns an enumerable for stepping through the rows of the collection. Unlike <see cref="GetRowEnumerator"/>,
        /// the items are not sorted by Id and will be returned in the same order as they are stored in <see cref="SharedData"/>.
        /// </summary>
        /// <typeparam name="TTable"></typeparam>
        /// <typeparam name="TEntry"></typeparam>
        /// <param name="tables"></param>
        /// <returns></returns>
        protected static IEnumerable<Row<TEntry>> GetRowEnumeratorUnsorted<TTable, TEntry>(IList<TTable> tables)
            where TTable : DetailedLocalizationTable<TEntry>
            where TEntry : TableEntry
        {
            if (tables == null)
                throw new ArgumentNullException(nameof(tables));

            SharedTableData sharedTableData = null;

            // Prepare the tables - Sort the keys and table entries
            foreach (var table in tables)
            {
                if (sharedTableData == null)
                {
                    sharedTableData = table.SharedData;
                }
                else if (sharedTableData != table.SharedData)
                {
                    throw new Exception("All tables must share the same SharedData.");
                }
            }

            var currentRow = new Row<TEntry>
            {
                TableEntriesReference = tables.Select(t => t.LocaleIdentifier).ToArray(),
                TableEntries = new TEntry[tables.Count]
            };

            foreach (var keyEntry in sharedTableData.Entries)
            {
                currentRow.KeyEntry = keyEntry;
                for (int i = 0; i < tables.Count; ++i)
                {
                    currentRow.TableEntries[i] = tables[i].GetEntry(keyEntry.Id);
                }
                yield return currentRow;
            }
        }

        /// <summary>
        /// Adds <see cref="SharedData"/> to Addressables.
        /// </summary>
        protected internal virtual void AddSharedTableDataToAddressables()
        {
            var aaSettings = LocalizationEditorSettings.Instance.GetAddressableAssetSettings(true);
            if (aaSettings == null)
                return;

            if (TableType == typeof(StringTable))
                AddressableGroupRules.AddStringTableSharedAsset(SharedData, aaSettings, false);
            else
                AddressableGroupRules.AddAssetTableSharedAsset(SharedData, aaSettings, false);
        }

        /// <summary>
        /// Add the table to the Addressable assets system.
        /// </summary>
        /// <param name="table">The table to add.</param>
        /// <param name="createUndo">Should an Undo operation be recorded?</param>
        protected virtual void AddTableToAddressables(LocalizationTable table, bool createUndo)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table), "Can add a null table to Addressables");

            var aaSettings = LocalizationEditorSettings.Instance.GetAddressableAssetSettings(true);
            if (aaSettings == null)
                return;

            var tableEntry = TableType == typeof(StringTable) ? AddressableGroupRules.AddStringTableAsset(table, aaSettings, createUndo) : AddressableGroupRules.AddAssetTableAsset(table, aaSettings, createUndo);

            if (createUndo)
                Undo.RecordObjects(new UnityEngine.Object[] { aaSettings, tableEntry.parentGroup }, "Update table");

            tableEntry.address = AddressHelper.GetTableAddress(table.TableCollectionName, table.LocaleIdentifier);
            tableEntry.labels.RemoveWhere(AddressHelper.IsLocaleLabel); // Locale may have changed so clear the old ones.

            // Label the locale
            var localeLabel = AddressHelper.FormatAssetLabel(table.LocaleIdentifier);
            tableEntry.SetLabel(localeLabel, true, true);
        }

        /// <summary>
        /// Remove the table from the Addressables system.
        /// </summary>
        /// <param name="table">The table to remove.</param>
        /// <param name="createUndo">Should an Undo operation be recorded?</param>
        protected virtual void RemoveTableFromAddressables(LocalizationTable table, bool createUndo)
        {
            if (table == null)
                return;

            var settings = LocalizationEditorSettings.Instance.GetAddressableAssetSettings(false);
            if (settings == null)
                return;

            var tableEntry = LocalizationEditorSettings.Instance.GetAssetEntry(table);
            if (tableEntry == null)
                return;

            if (createUndo)
                Undo.RecordObjects(new UnityEngine.Object[] { settings, tableEntry.parentGroup }, "Remove table");

            settings.RemoveAssetEntry(tableEntry.guid);
        }

        /// <summary>
        /// Called when the asset is created or imported into a project(via OnPostprocessAllAssets).
        /// </summary>
        protected internal virtual void ImportCollectionIntoProject()
        {
            RefreshAddressables();

            var missingLocales = new List<LocaleIdentifier>();
            foreach (var table in Tables)
            {
                var locale = LocalizationEditorSettings.GetLocale(table.asset.LocaleIdentifier.Code);
                if (locale == null)
                {
                    missingLocales.Add(new LocaleIdentifier(table.asset.LocaleIdentifier.Code));
                }
            }

            if (missingLocales.Count > 0)
            {
                // First check that the Locale does not exist in the project but is not marked as Addressable.
                using (DictionaryPool<LocaleIdentifier, Locale>.Get(out var projectLocales))
                {
                    var allLocales = AssetDatabase.FindAssets("t:Locale");
                    foreach (var loc in allLocales)
                    {
                        var loadedLocale = AssetDatabase.LoadAssetAtPath<Locale>(AssetDatabase.GUIDToAssetPath(loc));
                        projectLocales[loadedLocale.Identifier] = loadedLocale;
                    }

                    for (int i = 0; i < missingLocales.Count; ++i)
                    {
                        if (projectLocales.TryGetValue(missingLocales[i], out var foundLocale))
                        {
                            missingLocales.RemoveAt(i);
                            i--;

                            LocalizationEditorSettings.AddLocale(foundLocale);
                        }
                    }
                }

                if (missingLocales.Count > 0)
                {
                    var defaultDirectory = Path.GetDirectoryName(AssetDatabase.GetAssetPath(this));
                    var relativePath = PathHelper.MakePathRelative(defaultDirectory);

                    LocaleGeneratorWindow.ExportSelectedLocales(relativePath, missingLocales);

                    var sb = new StringBuilder();
                    sb.AppendLine($"The following missing Locales have been added to the project because they are used by the Collection {TableCollectionName}:");
                    missingLocales.ForEach(l => sb.AppendLine(l.ToString()));
                    Debug.Log(sb.ToString());
                }
            }

            var isInProject = TableType == typeof(StringTable) ? LocalizationEditorSettings.GetStringTableCollection(TableCollectionName) != null : LocalizationEditorSettings.GetAssetTableCollection(TableCollectionName) != null;
            if (!isInProject)
                LocalizationEditorSettings.EditorEvents.RaiseCollectionAdded(this);
            else
                LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(this, this);
        }

        /// <summary>
        /// Called to remove the asset from a project, such as when it is about to be deleted.
        /// </summary>
        protected internal virtual void RemoveCollectionFromProject()
        {
            foreach (var tbl in m_Tables)
            {
                if (tbl.asset != null)
                    RemoveTableFromAddressables(tbl.asset, false);
            }
            LocalizationEditorSettings.EditorEvents.RaiseCollectionRemoved(this);
        }

        void RemoveBrokenTables()
        {
            // We cant do this in OnBeforeSerialize or OnAfterDeserialize as it uses ForceLoadFromInstanceID and this is not allowed to be called during serialization.
            int brokenCount = 0;
            for (int i = 0; i < m_Tables.Count; ++i)
            {
                if (m_Tables[i].isBroken)
                {
                    m_Tables.RemoveAt(i);
                    --i;
                    ++brokenCount;
                }
            }

            if (brokenCount > 0)
            {
                Debug.LogWarning($"{brokenCount} Broken table reference was found and removed for {TableCollectionName} collection. References to this table or its assets may have not been cleaned up.", this);
                EditorUtility.SetDirty(this);
            }
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            m_ReadOnlyTables = null;
            m_ReadOnlyExtensions = null;
        }

        public override string ToString() => $"{TableCollectionName}({TableType.Name})";
    }
}
