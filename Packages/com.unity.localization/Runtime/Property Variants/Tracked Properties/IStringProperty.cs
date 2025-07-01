#if ENABLE_PROPERTY_VARIANTS || PACKAGE_DOCS_GENERATION

namespace UnityEngine.Localization.PropertyVariants.TrackedProperties
{
    /// <summary>
    /// Represents a property whose value can be converted from and to a string.
    /// </summary>
    public interface IStringProperty : ITrackedProperty
    {
        /// <summary>
        /// Returns the value for the LocaleIdentifier as a string representation.
        /// </summary>
        /// <param name="localeIdentifier">The LocaleIdentifier whose variant should be returned.</param>
        /// <returns>The variants value a string or <see langword="null"/> if an override does not exist for the LocaleIdentifier.</returns>
        string GetValueAsString(LocaleIdentifier localeIdentifier);

        /// <summary>
        /// Returns the value for the LocaleIdentifier as a string representation, uses the fallback if a variant does not exist.
        /// </summary>
        /// <param name="localeIdentifier">The LocaleIdentifier whose variant should be returned.</param>
        /// <param name="fallback">If no variant exists for the LocaleIdentifier then the fallback will be used.</param>
        /// <returns>The variant or fallback value a string or <see langword="null"/> if an override could not be found.</returns>
        string GetValueAsString(LocaleIdentifier localeIdentifier, LocaleIdentifier fallback);

        /// <summary>
        /// Assigns a value for the chosen LocaleIdentifier.
        /// </summary>
        /// <param name="localeIdentifier">The LocaleIdentifier the variant should be applied to.</param>
        /// <param name="value">The variant value for the LocaleIdentifier.</param>
        void SetValueFromString(LocaleIdentifier localeIdentifier, string value);
    }
}

#endif
