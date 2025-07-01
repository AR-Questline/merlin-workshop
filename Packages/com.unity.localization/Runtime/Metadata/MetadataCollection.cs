using System;
using System.Collections.Generic;

namespace UnityEngine.Localization.Metadata
{
    /// <summary>
    /// Interface to indicate that the class contains Metadata.
    /// </summary>
    public interface IMetadataCollection
    {
        /// <summary>
        /// All Metadata entries contained in this collection.
        /// </summary>
        IList<IMetadata> MetadataEntries { get; }

        /// <summary>
        /// Returns the first Metadata of type TObject or null if one does not exist.
        /// </summary>
        /// <typeparam name="TObject">A type that implements <see cref="IMetadata"/></typeparam>
        /// <returns></returns>
        TObject GetMetadata<TObject>() where TObject : IMetadata;

        /// <summary>
        /// Populates <param name="foundItems"/> list with all Metadata`s that are of type TObject.
        /// </summary>
        /// <param name="foundItems">List to add the found types to.</param>
        /// <typeparam name="TObject">A type that implements <see cref="IMetadata"/></typeparam>
        void GetMetadatas<TObject>(IList<TObject> foundItems) where TObject : IMetadata;

        /// <summary>
        /// Returns a list of all Metadata`s that are of type TObject.
        /// </summary>
        /// <typeparam name="TObject">A type that implements <see cref="IMetadata"/></typeparam>
        /// <returns></returns>
        IList<TObject> GetMetadatas<TObject>() where TObject : IMetadata;

        /// <summary>
        /// Add the Metadata to the collection.
        /// </summary>PreloadAssetTableMetadata
        /// <param name="md"></param>
        void AddMetadata(IMetadata md);

        /// <summary>
        /// Removes the Metadata if it is in the collection.
        /// </summary>
        /// <param name="md"></param>
        /// <returns>True if the collection contained the Metadata.</returns>
        bool RemoveMetadata(IMetadata md);

        /// <summary>
        /// Returns true if the collection contains the Metadata.
        /// </summary>
        /// <param name="md"></param>
        /// <returns></returns>
        bool Contains(IMetadata md);
    }

    /// <summary>
    /// Holds a collection of <see cref="IMetadata"/> that can be serialized.
    /// </summary>
    [Serializable]
    public class MetadataCollection : IMetadataCollection
    {
        [SerializeReference]
        List<IMetadata> m_Items = new List<IMetadata>();

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public IList<IMetadata> MetadataEntries => m_Items;

        /// <summary>
        /// Does the collection contain any Metadata?
        /// </summary>
        public bool HasData => MetadataEntries != null && MetadataEntries.Count > 0;

        /// <summary>
        /// Returns true if any metadata of type TObject contains this entry.
        /// </summary>
        /// <typeparam name="TObject"></typeparam>
        /// <returns></returns>
        public bool HasMetadata<TObject>() where TObject : IMetadata
        {
            return GetMetadata<TObject>() != null;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <typeparam name="TObject"></typeparam>
        /// <returns></returns>
        public TObject GetMetadata<TObject>() where TObject : IMetadata
        {
            foreach (var md in m_Items)
            {
                if (md is TObject obj)
                {
                    return obj;
                }
            }
            return default(TObject);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="foundItems">List that will be populated with the metadata that was found.</param>
        /// <typeparam name="TObject"></typeparam>
        public void GetMetadatas<TObject>(IList<TObject> foundItems) where TObject : IMetadata
        {
            foundItems.Clear();
            foreach (var md in m_Items)
            {
                if (md is TObject obj)
                {
                    foundItems.Add(obj);
                }
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <typeparam name="TObject"></typeparam>
        /// <returns></returns>
        public IList<TObject> GetMetadatas<TObject>() where TObject : IMetadata
        {
            var foundItems = new List<TObject>();
            GetMetadatas(foundItems);
            return foundItems;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="md"></param>
        public void AddMetadata(IMetadata md)
        {
            MetadataEntries.Add(md);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="md"></param>
        /// <returns></returns>
        public bool RemoveMetadata(IMetadata md)
        {
            return m_Items.Remove(md);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="md"></param>
        /// <returns></returns>
        public bool Contains(IMetadata md)
        {
            return m_Items.Contains(md);
        }
    }
}
