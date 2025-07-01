using System;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[DisplayName("My Startup Selector")]
[Serializable]
public class StartupSelectorExample : IStartupLocaleSelector
{
    // Allow the user to select a specific language to use.
    public SystemLanguage language;

    public Locale GetStartupLocale(ILocalesProvider availableLocales)
    {
        // Return the Locale that matches the language field or null if one does not exist.
        return availableLocales.GetLocale(language);
    }
}
