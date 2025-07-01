using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace UnityEditor.Localization
{
    /// <summary>
    /// Common events sent out during interaction with the localization system through the Editor.
    /// </summary>
    public class LocalizationEditorEvents
    {
        /// <summary>
        /// Event that is sent when a new <see cref="Locale"/> is added to the project.
        /// </summary>
        public event Action<Locale> LocaleAdded;
        internal virtual void RaiseLocaleAdded(Locale locale) => LocaleAdded?.Invoke(locale);

        /// <summary>
        /// Event that is sent when a <see cref="Locale"/> is removed from the project.
        /// </summary>
        public event Action<Locale> LocaleRemoved;
        internal virtual void RaiseLocaleRemoved(Locale locale) => LocaleRemoved?.Invoke(locale);

        /// <summary>
        /// Event that is sent when the <see cref="Locale"/> sort order is changed.
        /// </summary>
        public event EventHandler<Locale> LocaleSortOrderChanged;
        internal virtual void RaiseLocaleSortOrderChanged(object sender, Locale locale) => LocaleSortOrderChanged?.Invoke(sender, locale);

        /// <summary>
        /// Event that is sent when a table entry is modified.
        /// </summary>
        public event Action<SharedTableData.SharedTableEntry> TableEntryModified;
        internal virtual void RaiseTableEntryModified(SharedTableData.SharedTableEntry tableEntry) => TableEntryModified?.Invoke(tableEntry);

        /// <summary>
        /// Event that is sent when a new entry is added to a table.
        /// </summary>
        public event Action<LocalizationTableCollection, SharedTableData.SharedTableEntry> TableEntryAdded;
        internal virtual void RaiseTableEntryAdded(LocalizationTableCollection collection, SharedTableData.SharedTableEntry entry) => TableEntryAdded?.Invoke(collection, entry);

        /// <summary>
        /// Event that is sent when an entry is removed from a a table.
        /// </summary>
        public event Action<LocalizationTableCollection, SharedTableData.SharedTableEntry> TableEntryRemoved;
        internal virtual void RaiseTableEntryRemoved(LocalizationTableCollection collection, SharedTableData.SharedTableEntry entry) => TableEntryRemoved?.Invoke(collection, entry);

        /// <summary>
        /// Event that is sent when new asset table entry is added.
        /// </summary>
        public event Action<AssetTableCollection, AssetTable, AssetTableEntry> AssetTableEntryAdded;
        internal virtual void RaiseAssetTableEntryAdded(AssetTableCollection collection, AssetTable table, AssetTableEntry entry) => AssetTableEntryAdded?.Invoke(collection, table, entry);

        /// <summary>
        /// Event that is sent when an asset table entry is removed from a table.
        /// </summary>
        public event Action<AssetTableCollection, AssetTable, AssetTableEntry, string> AssetTableEntryRemoved;
        internal virtual void RaiseAssetTableEntryRemoved(AssetTableCollection collection, AssetTable table, AssetTableEntry entry, string assetGuid) => AssetTableEntryRemoved?.Invoke(collection, table, entry, assetGuid);

        /// <summary>
        /// Event that is sent when a table collection is modified.
        /// </summary>
        public event EventHandler<LocalizationTableCollection> CollectionModified;

        /// <summary>
        /// Sends an event to <see cref="CollectionModified"/> that indicates a table collection has been modified.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="collection"></param>
        /// <example>
        /// This shows how to export and import a <see cref="StringTableCollection"/> as JSON.
        /// <code source="../../DocCodeSamples.Tests/LocalizationTablesWindowPopulateMenu.cs"/>
        /// </example>
        public virtual void RaiseCollectionModified(object sender, LocalizationTableCollection collection) => CollectionModified?.Invoke(sender, collection);

        /// <summary>
        /// Event that is sent when a new table collection is added to the project.
        /// </summary>
        public event Action<LocalizationTableCollection> CollectionAdded;
        internal virtual void RaiseCollectionAdded(LocalizationTableCollection collection) => CollectionAdded?.Invoke(collection);

        /// <summary>
        /// Event that is sent when a table collection is removed from the project.
        /// </summary>
        public event Action<LocalizationTableCollection> CollectionRemoved;
        internal virtual void RaiseCollectionRemoved(LocalizationTableCollection collection) => CollectionRemoved?.Invoke(collection);

        /// <summary>
        /// Event that is sent when a table is added to a collection in the project.
        /// </summary>
        public event Action<LocalizationTableCollection, LocalizationTable> TableAddedToCollection;
        internal virtual void RaiseTableAddedToCollection(LocalizationTableCollection collection, LocalizationTable table) => TableAddedToCollection?.Invoke(collection, table);

        /// <summary>
        /// Event that is sent when a table is removed from a collection in the project.
        /// </summary>
        public event Action<LocalizationTableCollection, LocalizationTable> TableRemovedFromCollection;
        internal virtual void RaiseTableRemovedFromCollection(LocalizationTableCollection collection, LocalizationTable table) => TableRemovedFromCollection?.Invoke(collection, table);

        /// <summary>
        /// Event that is sent when a collection extension is added to a table collection in the project.
        /// </summary>
        public event Action<LocalizationTableCollection, CollectionExtension> ExtensionAddedToCollection;
        internal virtual void RaiseExtensionAddedToCollection(LocalizationTableCollection collection, CollectionExtension collectionExtension) => ExtensionAddedToCollection?.Invoke(collection, collectionExtension);

        /// <summary>
        /// Event that is sent when a collection extension is removed from a table collection in the project.
        /// </summary>
        public event Action<LocalizationTableCollection, CollectionExtension> ExtensionRemovedFromCollection;
        internal virtual void RaiseExtensionRemovedFromCollection(LocalizationTableCollection collection, CollectionExtension collectionExtension) => ExtensionRemovedFromCollection?.Invoke(collection, collectionExtension);
    }
}
