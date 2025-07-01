using System.Collections.Generic;
using UnityEngine.Localization.Settings;

namespace UnityEngine.Localization.Samples
{
    /// <summary>
    /// This class shows how a custom <see cref="ILocalesProvider"/> can be created to provide the Locales that are supported by
    /// the project at runtime. By default the <see cref="LocalesProvider"/> uses the Addressable asset system to Load the Locales
    /// that have been configured in the project however it is possible to replace or customize this behavior to load
    /// Locales from a different source or append to the existing ones.
    /// </summary>
    public class SimpleLocalesProvider : ILocalesProvider
    {
        /// <summary>
        /// For this example we just store a List of Locales that we create in memory.
        /// </summary>
        public List<Locale> Locales { get; } = new List<Locale>();

        public Locale GetLocale(LocaleIdentifier id) => Locales.Find(l => l.Identifier == id);
        public void AddLocale(Locale locale) => Locales.Add(locale);
        public bool RemoveLocale(Locale locale) => Locales.Remove(locale);
    }
}
