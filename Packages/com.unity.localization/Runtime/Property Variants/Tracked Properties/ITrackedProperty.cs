#if ENABLE_PROPERTY_VARIANTS || PACKAGE_DOCS_GENERATION

namespace UnityEngine.Localization.PropertyVariants.TrackedProperties
{
    /// <summary>
    /// Represents a property that contains 1 or more variants.
    /// </summary>
    public interface ITrackedProperty
    {
        /// <summary>
        /// The serialized property path.
        /// </summary>
        string PropertyPath { get; set; }

        /// <summary>
        /// Checks if the property contains an overriden value for the LocaleIdentifier.
        /// </summary>
        /// <param name="localeIdentifier">The LocaleIdentifier to check.</param>
        /// <returns><see langword="true"/> if the property contains a variant or <see langword="false"/> if it does not.</returns>
        bool HasVariant(LocaleIdentifier localeIdentifier);
    }
}

#endif
