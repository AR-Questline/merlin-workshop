using System;

namespace UnityEngine.Localization.Settings
{
    /// <summary>
    /// Uses the Player Prefs to keep track of the last used locale.
    /// Whenever the locale is changed, the new Locale is recorded in the Player prefs.
    /// </summary>
    [Serializable]
    public class PlayerPrefLocaleSelector : IStartupLocaleSelector, IInitialize
    {
        [SerializeField]
        string m_PlayerPreferenceKey = "selected-locale";

        /// <summary>
        /// The Player Pref key to use.
        /// </summary>
        public string PlayerPreferenceKey
        {
            get => m_PlayerPreferenceKey;
            set => m_PlayerPreferenceKey = value;
        }

        /// <summary>
        /// Registers a callback to <see cref="LocalizationSettings.SelectedLocaleChanged"/> in order to save changes made to the Locale.
        /// </summary>
        /// <param name="settings"></param>
        public void PostInitialization(LocalizationSettings settings)
        {
            if (LocalizationSettings.Instance.IsPlayingOrWillChangePlaymode)
            {
                // Record the new selected locale so it can persist between runs
                var selectedLocale = settings.GetSelectedLocale();
                if (selectedLocale != null)
                    PlayerPrefs.SetString(PlayerPreferenceKey, selectedLocale.Identifier.Code);
            }
        }

        /// <summary>
        /// Returns the last locale set or null if no value has been recorded yet.
        /// </summary>
        /// <param name="availableLocales"></param>
        /// <returns></returns>
        public Locale GetStartupLocale(ILocalesProvider availableLocales)
        {
            if (PlayerPrefs.HasKey(PlayerPreferenceKey))
            {
                var code = PlayerPrefs.GetString(PlayerPreferenceKey);
                if (!string.IsNullOrEmpty(code))
                {
                    return availableLocales.GetLocale(code);
                }
            }

            // No locale could be found.
            return null;
        }
    }
}
