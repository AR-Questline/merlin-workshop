using System;

namespace UnityEngine.Localization.Settings
{
    /// <summary>
    /// Returns the a specific locale if it exists.
    /// </summary>
    [Serializable]
    public class SpecificLocaleSelector : IStartupLocaleSelector
    {
        [SerializeField]
        LocaleIdentifier m_LocaleId = new LocaleIdentifier(SystemLanguage.English);

        /// <summary>
        /// The Id of the Locale that should be used if possible when selecting.
        /// </summary>
        public LocaleIdentifier LocaleId
        {
            get => m_LocaleId;
            set => m_LocaleId = value;
        }

        /// <summary>
        /// Returns the <see cref="Locale"/> that matches <see cref="LocaleId"/> or null if one could not be found.
        /// </summary>
        /// <param name="availableLocales"></param>
        /// <returns></returns>
        public Locale GetStartupLocale(ILocalesProvider availableLocales) => availableLocales.GetLocale(LocaleId);
    }
}
