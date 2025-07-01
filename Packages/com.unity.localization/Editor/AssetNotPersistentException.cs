using System;

namespace UnityEditor.Localization
{
    /// <summary>
    /// This exception is thrown when an asset is expected to be persistent(saved on disk) but it is not.
    /// </summary>
    public class AssetNotPersistentException : Exception
    {
        /// <summary>
        /// Creates a new instance of the exception.
        /// </summary>
        /// <param name="target"></param>
        public AssetNotPersistentException(UnityEngine.Object target) :
            base($"{target.name} is not persistent. The object needs to be saved to disk.")
        {
        }
    }
}
