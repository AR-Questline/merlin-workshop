using System.Globalization;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;

namespace UnityEditor.Localization.Platform.Utility
{
    internal class FallbackLocaleHelper
    {
        public static Locale GetLocaleFallback(Locale locale)
        {
            // Use fallback?
            var fallBackLocale = locale.Metadata?.GetMetadata<FallbackLocale>()?.Locale;
            if (fallBackLocale != null)
                return fallBackLocale;

            var cultureInfo = locale.Identifier.CultureInfo;
            if (cultureInfo == null)
                return fallBackLocale;

            while (cultureInfo != CultureInfo.InvariantCulture && fallBackLocale == null)
            {
                var fb = LocalizationEditorSettings.GetLocale(new LocaleIdentifier(cultureInfo).Code);
                if (locale != fb)
                    fallBackLocale = fb;
                cultureInfo = cultureInfo.Parent;
            }

            return fallBackLocale;
        }
    }
}
