using System;
using System.Collections.Generic;
using UnityEngine.Localization.Metadata;
using UnityEngine.Serialization;

namespace UnityEngine.Localization.Tables
{
    /// <summary>
    /// Abstract base class for all tables.
    /// Contains common functionality.
    /// </summary>
    public abstract class LocalizationTable : ScriptableObject, IMetadataCollection, IComparable<LocalizationTable>
    {
        [SerializeField]
        LocaleIdentifier m_LocaleId;

        [FormerlySerializedAs("m_KeyDatabase")]
        [SerializeField, HideInInspector]
        SharedTableData m_SharedData;

        [SerializeField]
        MetadataCollection m_Metadata = new MetadataCollection();

        [SerializeField]
        List<TableEntryData> m_TableData = new List<TableEntryData>();

        /// <summary>
        /// The locale this asset table supports.
        /// </summary>
        public LocaleIdentifier LocaleIdentifier
        {
            get => m_LocaleId;
            set => m_LocaleId = value;
        }

        /// <summary>
        /// The name of this asset table collection.
        /// </summary>
        public string TableCollectionName
        {
            get
            {
                VerifySharedTableDataIsNotNull();
                return SharedData.TableCollectionName;
            }
        }

        /// <summary>
        /// Data shared across all tables.
        /// </summary>
        public SharedTableData SharedData
        {
            get => m_SharedData;
            set => m_SharedData = value;
        }

        /// <summary>
        /// All entries stored within the table.
        /// </summary>
        internal List<TableEntryData> TableData
        {
            get => m_TableData;
        }

        /// <summary>
        /// Table Metadata.
        /// </summary>
        public IList<IMetadata> MetadataEntries
        {
            get => m_Metadata.MetadataEntries;
        }

        /// <summary>
        /// Returns the first Metadata item from <see cref="MetadataEntries"/> of type TObject.
        /// </summary>
        /// <typeparam name="TObject"></typeparam>
        /// <returns></returns>
        public TObject GetMetadata<TObject>() where TObject : IMetadata
        {
            return m_Metadata.GetMetadata<TObject>();
        }

        /// <summary>
        /// Populates the list with all Metadata from <see cref="MetadataEntries"/> that is of type TObject.
        /// </summary>
        /// <typeparam name="TObject"></typeparam>
        /// <param name="foundItems"></param>
        public void GetMetadatas<TObject>(IList<TObject> foundItems) where TObject : IMetadata
        {
            m_Metadata.GetMetadatas(foundItems);
        }

        /// <summary>
        /// Returns all Metadata from <see cref="MetadataEntries"/> that is of type TObject.
        /// </summary>
        /// <typeparam name="TObject"></typeparam>
        /// <returns></returns>
        public IList<TObject> GetMetadatas<TObject>() where TObject : IMetadata
        {
            return m_Metadata.GetMetadatas<TObject>();
        }

        /// <summary>
        /// Add an entry to <see cref="MetadataEntries"/>.
        /// </summary>
        /// <param name="md"></param>
        public void AddMetadata(IMetadata md)
        {
            m_Metadata.AddMetadata(md);
        }

        /// <summary>
        /// Remove an entry from <see cref="MetadataEntries"/>.
        /// </summary>
        /// <param name="md"></param>
        /// <returns></returns>
        public bool RemoveMetadata(IMetadata md)
        {
            return m_Metadata.RemoveMetadata(md);
        }

        /// <summary>
        /// Checks if the Metadata is contained within <see cref="MetadataEntries"/>.
        /// </summary>
        /// <param name="md"></param>
        /// <returns></returns>
        public bool Contains(IMetadata md)
        {
            return m_Metadata.Contains(md);
        }

        /// <summary>
        /// Create an empty entry in the table at the specified entry.
        /// </summary>
        /// <param name="entryReference"></param>
        public abstract void CreateEmpty(TableEntryReference entryReference);

        /// <summary>
        /// Returns the key with the matching name from the <see cref="SharedTableData"/>, if one exists.
        /// </summary>
        /// <param name="key">The key to search for.</param>
        /// <param name="addKey">Should a new key be added if one can not be found?</param>
        /// <exception cref="Exception">Thrown if the <see cref="SharedTableData"/> is null.</exception>
        /// <returns>The found key or null if one could not be found.</returns>
        protected long FindKeyId(string key, bool addKey)
        {
            VerifySharedTableDataIsNotNull();
            return SharedData.GetId(key, addKey);
        }

        void VerifySharedTableDataIsNotNull()
        {
            if (SharedData == null)
                throw new NullReferenceException($"The Table \"{name}\" does not have a {nameof(SharedTableData)}.");
        }

        /// <summary>
        /// Returns a string representation of the table in the format "{TableCollectionName}({LocaleIdentifier})".
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{TableCollectionName}({LocaleIdentifier})";

        /// <summary>
        /// Compare to another LocalizationTable.
        /// Performs a comparison against the <see cref="LocaleIdentifier"/> property.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(LocalizationTable other)
        {
            if (other == null)
                return 1;
            return LocaleIdentifier.CompareTo(other.LocaleIdentifier);
        }
    }
}
