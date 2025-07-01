using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.Localization.Pseudo;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityEngine.Localization.Settings
{
    /// <summary>
    /// Responsible for providing the list of locales that are currently available to this application.
    /// </summary>
    [Serializable]
    public class LocalesProvider : ILocalesProvider, IPreloadRequired, IReset, IDisposable
    {
        readonly List<Locale> m_Locales = new List<Locale>();
        AsyncOperationHandle m_LoadOperation;

        /// <summary>
        /// The list of all supported locales.
        /// </summary>
        public List<Locale> Locales
        {
            get
            {
                if (LocalizationSettings.Instance.IsPlayingOrWillChangePlaymode && !PreloadOperation.IsDone)
                {
                    #if !UNITY_WEBGL // WebGL does not support WaitForCompletion
                    PreloadOperation.WaitForCompletion();
                    #else
                    Debug.LogError("Locales PreloadOperation has not been initialized, can not return the available locales.");
                    #endif
                }

                return m_Locales;
            }
        }

        /// <summary>
        /// The Locales loading operation. When set to isDone then all locales have been loaded. Can be Null if the operation has not started yet.
        /// </summary>
        public AsyncOperationHandle PreloadOperation
        {
            get
            {
                if (!m_LoadOperation.IsValid())
                {
                    m_Locales.Clear();
                    m_LoadOperation = AddressablesInterface.LoadAssetsWithLabel<Locale>(LocalizationSettings.LocaleLabel, AddLocale);
                }

                return m_LoadOperation;
            }
        }

        /// <summary>
        /// Attempt to retrieve a Locale or fallback Locale using the identifier.
        /// </summary>
        /// <param name="id"><see cref="LocaleIdentifier"/> to find.</param>
        /// <returns>If no Locale can be found then null is returned.</returns>
        public Locale GetLocale(LocaleIdentifier id)
        {
            foreach (var locale in Locales)
            {
                if (locale == null || locale is PseudoLocale)
                    continue;

                if (locale.Identifier.Equals(id))
                    return locale;
            }
            return FindFallbackLocale(id);
        }

        /// <summary>
        /// Attempt to retrieve a Locale or fallback Locale using a Code.
        /// </summary>
        /// <param name="code">If no Locale can be found then null is returned.</param>
        /// <returns></returns>
        public Locale GetLocale(string code) => GetLocale(new LocaleIdentifier(code));

        /// <summary>
        /// Attempt to retrieve a Locale or fallback Locale using a [SystemLanguage](https://docs.unity3d.com/ScriptReference/SystemLanguage.html).
        /// </summary>
        /// <param name="systemLanguage"></param>
        /// <returns>If no Locale can be found then null is returned.</returns>
        public Locale GetLocale(SystemLanguage systemLanguage) => GetLocale(new LocaleIdentifier(systemLanguage));

        /// <summary>
        /// Add a Locale to allow support for a specific language.
        /// </summary>
        /// <param name="locale"></param>
        public void AddLocale(Locale locale)
        {
            if (locale == null)
                return;

            var isPseudoLocale = locale is PseudoLocale;
            if (!isPseudoLocale)
            {
                // Check if the locale has already been added
                foreach (var l in m_Locales)
                {
                    if (l is PseudoLocale)
                        continue;

                    if (l.Identifier == locale.Identifier)
                    {
                        Debug.LogWarning($"Ignoring locale {locale}. The locale {l} has the same Id `{locale.Identifier}`");
                        return;
                    }
                }
            }

            var index = m_Locales.BinarySearch(locale);
            if (index < 0)
            {
                m_Locales.Insert(~index, locale);
            }
        }

        /// <summary>
        /// Removes support for a specific Locale.
        /// </summary>
        /// <param name="locale">The locale that should be removed if possible.</param>
        /// <returns>true if the locale was removed or false if the locale did not exist.</returns>
        public bool RemoveLocale(Locale locale)
        {
            if (locale == null)
                return false;

            var ret = Locales.Remove(locale);
            var settings = LocalizationSettings.GetInstanceDontCreateDefault();
            settings?.OnLocaleRemoved(locale);
            return ret;
        }

        /// <summary>
        /// Attempt to retrieve a fallback Locale using the identifier.
        /// </summary>
        /// <param name="localeIdentifier"><see cref="LocaleIdentifier"/> to find.</param>
        /// <returns>If no Locale can be found then null is returned.</returns>
        public Locale FindFallbackLocale(LocaleIdentifier localeIdentifier)
        {
            var cultureInfo = localeIdentifier.CultureInfo;
            if (cultureInfo == null)
                return null;

            // Attempt to use CultureInfo fallbacks to find the closest locale
            Locale locale = null;
            cultureInfo = cultureInfo.Parent;
            while (cultureInfo != CultureInfo.InvariantCulture && locale == null)
            {
                locale = GetLocale(cultureInfo);
                cultureInfo = cultureInfo.Parent;
            }
            return locale;
        }

        /// <summary>
        /// Resets the state of the provider by removing all the Locales and clearing the preload operation.
        /// </summary>
        public void ResetState()
        {
            m_Locales.Clear();
            m_LoadOperation = default;
        }

        /// <summary>
        /// Removes and releases internal references to Addressable assets.
        /// </summary>
        ~LocalesProvider()
        {
            AddressablesInterface.SafeRelease(m_LoadOperation);
        }

        /// <summary>
        /// Removes and releases internal references to Addressable assets.
        /// </summary>
        void IDisposable.Dispose()
        {
            m_Locales.Clear();
            AddressablesInterface.SafeRelease(m_LoadOperation);
            GC.SuppressFinalize(this);
        }
    }
}
