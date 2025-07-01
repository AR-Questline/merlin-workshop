using System;

namespace UnityEditor.Localization
{
    /// <summary>
    /// This exception is thrown when an asset is expected to be part of the Addressables system.
    /// The Localization system uses Addressables for loading Locales, Tables and Table Assets.
    /// </summary>
    public class AddressableEntryNotFoundException : Exception
    {
        /// <summary>
        /// Creates a new instance of the exception.
        /// </summary>
        /// <param name="target">The target of the exception.</param>
        public AddressableEntryNotFoundException(UnityEngine.Object target) :
            base($"{target.name} could not find an Addressable asset.")
        {
        }
    }
}
