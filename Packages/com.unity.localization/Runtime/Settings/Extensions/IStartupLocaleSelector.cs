namespace UnityEngine.Localization.Settings
{
    /// <summary>
    /// Determines what locale should be used for the <see cref="LocalizationSettings.SelectedLocale"/> when the application starts or does not currently have an active <see cref="LocalizationSettings.SelectedLocale"/>.
    /// </summary>
    /// <example>
    /// This example shows how to implement a custom selector. Once implemented, you can add the custom selector in the Startup Selectors menu.
    /// ![](../manual/images/scripting/StartupSelectorExample.png)
    /// <code source="../../../DocCodeSamples.Tests/IStartupLocaleSelectorSample.cs"/>
    /// </example>
    public interface IStartupLocaleSelector
    {
        /// <summary>
        /// Used to determine which <see cref="Locale"/> should be used.
        /// </summary>
        /// <param name="availableLocales">The available locales to select from.</param>
        /// <returns>The selected locale or null. <see cref="LocalizationSettings.StartupLocaleSelectors"/> will evaluate each selector until a non-null value is returned, this value will become the <see cref="LocalizationSettings.SelectedLocale"/>.</returns>
        Locale GetStartupLocale(ILocalesProvider availableLocales);
    }
}
