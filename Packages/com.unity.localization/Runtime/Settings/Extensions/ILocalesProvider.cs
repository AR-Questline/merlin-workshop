using System.Collections.Generic;

namespace UnityEngine.Localization.Settings
{
    /// <summary>
    /// Holds the Locale's that the project supports.
    /// </summary>
    public interface ILocalesProvider
    {
        /// <summary>
        /// The list of Locales available.
        /// </summary>
        List<Locale> Locales { get; }

        /// <summary>
        /// Attempt to retrieve a Locale using the identifier.
        /// </summary>
        /// <param name="id"><see cref="LocaleIdentifier"/> to find.</param>
        /// <returns>If no Locale can be found then null is returned.</returns>
        Locale GetLocale(LocaleIdentifier id);

        /// <summary>
        /// Add a Locale to allow support for a specific language.
        /// </summary>
        /// <param name="locale">The locale to add.</param>
        void AddLocale(Locale locale);

        /// <summary>
        /// Removes support for a specific Locale.
        /// </summary>
        /// <param name="locale">The locale that should be removed if possible.</param>
        /// <returns>true if the locale was removed or false if the locale did not exist.</returns>
        bool RemoveLocale(Locale locale);
    }
}
