using System;

namespace UnityEngine.Localization.Metadata
{
    /// <summary>
    /// Provides fallback data with regards to a Locale.
    /// For example you may want some regions to fall back to a default version, such as English(en_uk) may fall back to English(en).
    /// For details on how Unity evaluates the fallbacks see [Locale Fallbacks](../manual/Locale.html#fallbacks).
    /// </summary>
    [Metadata(AllowedTypes = MetadataType.Locale)]
    [Serializable]
    public class FallbackLocale : IMetadata
    {
        [SerializeField]
        Locale m_Locale;

        /// <summary>
        /// Creates a new FallbackLocale.
        /// </summary>
        public FallbackLocale()
        {
        }

        /// <summary>
        /// Create a new FallbackLocale to the specified fallback locale.
        /// </summary>
        /// <param name="fallback"></param>
        public FallbackLocale(Locale fallback)
        {
            Locale = fallback;
        }

        /// <summary>
        /// The Locale to fall back to when searching for a localized value.
        /// When assigning a new value, it will be checked to ensure that the Locales do not fallback to each
        /// other and cause cyclic issues. If an issue is found then a warning will be emitted and the value will be ignored.
        /// </summary>
        public Locale Locale
        {
            get => m_Locale;
            set
            {
                m_Locale = value;
                if (IsCyclic(value))
                    m_Locale = null;
            }
        }

        /// <summary>
        /// Check we don't have a fallback locale chain that leads back to this locale and an infinite loop.
        /// </summary>
        internal bool IsCyclic(Locale locale)
        {
            if (locale == null)
                return false;

            var parentMetadata = locale.Metadata?.GetMetadata<FallbackLocale>();

            while (parentMetadata != null && parentMetadata.Locale != null)
            {
                if (parentMetadata.Locale == locale)
                {
                    Debug.LogWarning($"Cyclic fallback linking detected. Can not set fallback locale '{locale}' as it would create an infinite loop.");
                    return true;
                }

                parentMetadata = parentMetadata.Locale.Metadata?.GetMetadata<FallbackLocale>();
            }
            return false;
        }
    }
}
