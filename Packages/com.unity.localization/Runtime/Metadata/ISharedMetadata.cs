namespace UnityEngine.Localization.Metadata
{
    /// <summary>
    /// Interface for any <see cref="IMetadata"/> that applies to multiple entries in a table.
    /// </summary>
    [HideInInspector]
    public interface ISharedMetadata : IMetadata
    {
        /// <summary>
        /// Is the entry using this shared metadata?
        /// </summary>
        /// <param name="keyId"></param>
        /// <returns></returns>
        bool Contains(long keyId);

        /// <summary>
        /// Mark the entry as using this shared metadata.
        /// </summary>
        /// <param name="keyId"></param>
        void AddEntry(long keyId);

        /// <summary>
        /// Remove the entry from the shared metadata.
        /// </summary>
        /// <param name="keyId"></param>
        void RemoveEntry(long keyId);
    }
}
