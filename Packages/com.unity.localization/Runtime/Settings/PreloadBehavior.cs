namespace UnityEngine.Localization.Settings
{
    /// <summary>
    /// Determines which tables, that have been marked as preload, will be loaded during the preloading step.
    /// </summary>
    public enum PreloadBehavior
    {
        /// <summary>
        /// No preloading will be performed. 
        /// </summary>
        NoPreloading,

        /// <summary>
        /// Preloads tables for the <see cref="LocalizationSettings.SelectedLocale"/>.
        /// </summary>
        PreloadSelectedLocale,

        /// <summary>
        /// Preloads tables for the <see cref="LocalizationSettings.SelectedLocale"/> and all fallback <see cref="Locale"/>.
        /// </summary>
        PreloadSelectedLocaleAndFallbacks,

        /// <summary>
        /// Preloads tables for all <see cref="Locale"/> that are in <see cref="LocalizationSettings.AvailableLocales"/>.
        /// </summary>
        PreloadAllLocales
    }
}
