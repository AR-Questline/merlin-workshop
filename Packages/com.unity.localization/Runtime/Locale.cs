using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Pseudo;
using UnityEngine.Localization.Settings;
using UnityEngine.Pool;

namespace UnityEngine.Localization
{
    /// <summary>
    /// Represents identification information for a language or its regional variant.
    /// Also includes access to the CultureInfo which provides culture-specific instances
    /// of the DateTimeFormatInfo, NumberFormatInfo, CompareInfo, and TextInfo objects.
    /// </summary>
    /// <example>
    /// This example shows the various ways to create a LocaleIdentifier.
    /// <code source="../DocCodeSamples.Tests/LocaleSamples.cs" region="locale-identifier-1"/>
    /// </example>
    /// <example>
    /// This shows how to create a Locale for English and a regional Locale for English(UK).
    /// <code source="../DocCodeSamples.Tests/LocaleSamples.cs" region="locale-identifier-2"/>
    /// </example>
    [Serializable]
    public struct LocaleIdentifier : IEquatable<LocaleIdentifier>, IComparable<LocaleIdentifier>
    {
        [SerializeField] string m_Code;
        CultureInfo m_CultureInfo;

        /// <summary>
        /// The culture name in the format [language]-[region].
        /// The name is a combination of an ISO 639 two-letter lowercase culture code associated with a language and an ISO 3166
        /// two-letter uppercase subculture code associated with a country or region.
        /// For example, Language English would be 'en', Regional English(UK) would be 'en-GB' and Regional English(US) would be 'en-US'.
        /// It is possible to use any string value when representing a non-standard identifier.
        /// </summary>
        public string Code => m_Code;

        /// <summary>
        /// A [CultureInfo](https://docs.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo) representation of the Locale.
        /// The <see cref="Code"/> is used to query for a valid [CultureInfo}(https://docs.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo).
        /// If a value can not be determined from the <see cref="Code"/> then <see langword="null"/> will be returned.
        /// </summary>
        /// <example>
        /// This example shows how the CultureInfo can be retrieved after creating a LocaleIdentifier using a Code.
        /// <code source="../DocCodeSamples.Tests/LocaleSamples.cs" region="culture-info"/>
        /// </example>
        public CultureInfo CultureInfo
        {
            get
            {
                if (m_CultureInfo == null && !string.IsNullOrEmpty(m_Code))
                {
                    try
                    {
                        m_CultureInfo = CultureInfo.GetCultureInfo(m_Code);
                    }
                    catch (CultureNotFoundException)
                    {
                        // If a culture info can not be found then we do not consider this an error. It could be a custom locale.
                    }
                }
                return m_CultureInfo;
            }
        }

        /// <summary>
        /// Create a LocaleIdentifier from a culture code string.
        /// </summary>
        /// <param name="code"></param>
        public LocaleIdentifier(string code)
        {
            m_Code = code;
            m_CultureInfo = null;
        }

        /// <summary>
        /// Create a LocaleIdentifier from a CultureInfo instance.
        /// </summary>
        /// <param name="culture"></param>
        /// <exception cref="ArgumentNullException">Thrown if the culture is null.</exception>
        public LocaleIdentifier(CultureInfo culture)
        {
            if (culture == null)
                throw new ArgumentNullException(nameof(culture));

            m_Code = culture.Name;
            m_CultureInfo = culture;
        }

        /// <summary>
        /// Create a LocaleIdentifier from a [SystemLanguage](https://docs.unity3d.com/ScriptReference/SystemLanguage.html) enum value.
        /// </summary>
        /// <param name="systemLanguage"></param>
        public LocaleIdentifier(SystemLanguage systemLanguage)
            : this(SystemLanguageConverter.GetSystemLanguageCultureCode(systemLanguage))
        {
        }

        #pragma warning disable CA2225 // CA2225: Operator overloads have named alternates

        /// <summary>
        /// Create a LocaleIdentifier from a culture code string.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static implicit operator LocaleIdentifier(string code) => new LocaleIdentifier(code);

        /// <summary>
        /// Create a LocaleIdentifier from a CultureInfo instance.
        /// </summary>
        /// <param name="culture"></param>
        /// <exception cref="ArgumentNullException">Thrown if the culture is null.</exception>
        /// <returns></returns>
        public static implicit operator LocaleIdentifier(CultureInfo culture) => new LocaleIdentifier(culture);

        /// <summary>
        /// Create a LocaleIdentifier from a [SystemLanguage](https://docs.unity3d.com/ScriptReference/SystemLanguage.html) enum value.
        /// </summary>
        /// <param name="systemLanguage"></param>
        /// <returns></returns>
        public static implicit operator LocaleIdentifier(SystemLanguage systemLanguage) => new LocaleIdentifier(systemLanguage);

        #pragma warning restore CA2225

        /// <summary>
        /// Returns a string representation.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (string.IsNullOrEmpty(m_Code))
            {
                return "undefined";
            }

            return $"{(CultureInfo != null ? CultureInfo.EnglishName : "Custom")}({Code})";
        }

        /// <summary>
        /// Compare the LocaleIdentifier to another LocaleIdentifier.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            return obj is LocaleIdentifier identifier && Equals(identifier);
        }

        /// <summary>
        /// Compare the LocaleIdentifier to another LocaleIdentifier.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(LocaleIdentifier other)
        {
            // Treat null and empty as the same
            if (string.IsNullOrEmpty(other.Code) && string.IsNullOrEmpty(Code))
                return true;

            return string.Equals(Code, other.Code, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns the hash code of [CultureInfo}(https://docs.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo) or <see cref="Code"/> if it is null.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return !string.IsNullOrEmpty(Code) ? Code.GetHashCode() : base.GetHashCode();
        }

        /// <summary>
        /// Compare to another <see cref="LocaleIdentifier"/>.
        /// Performs a comparison against [CultureInfo.EnglishName](https://docs.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo.englishname) property.
        /// </summary>
        /// <param name="other">Value to compare against.</param>
        /// <returns></returns>
        public int CompareTo(LocaleIdentifier other)
        {
            if (CultureInfo == null || other.CultureInfo == null)
                return 1;
            return string.CompareOrdinal(CultureInfo.EnglishName, other.CultureInfo.EnglishName);
        }

        /// <summary>
        /// Compare the LocaleIdentifier to another LocaleIdentifier.
        /// </summary>
        /// <param name="l1"></param>
        /// <param name="l2"></param>
        /// <returns></returns>
        public static bool operator==(LocaleIdentifier l1, LocaleIdentifier l2) => l1.Equals(l2);

        /// <summary>
        /// Compare the LocaleIdentifier to another LocaleIdentifier.
        /// </summary>
        /// <param name="l1"></param>
        /// <param name="l2"></param>
        /// <returns></returns>
        public static bool operator!=(LocaleIdentifier l1, LocaleIdentifier l2) => !l1.Equals(l2);
    }

    /// <summary>
    /// A Locale represents a language. It supports regional variations and can be configured with an optional fallback Locale via metadata.
    /// </summary>
    public class Locale : ScriptableObject, IEquatable<Locale>, IComparable<Locale>, ISerializationCallbackReceiver
    {
        [SerializeField]
        LocaleIdentifier m_Identifier;

        [SerializeField]
        [MetadataType(MetadataType.Locale)]
        MetadataCollection m_Metadata = new MetadataCollection();

        [SerializeField]
        string m_LocaleName;

        [SerializeField]
        string m_CustomFormatCultureCode;

        [SerializeField]
        bool m_UseCustomFormatter;

        [SerializeField]
        ushort m_SortOrder = 10000; // Default to a large value so new Locales are always at the end of a list.

        IFormatProvider m_Formatter;

        /// <summary>
        /// The identifier contains the identifying information such as the id and culture Code for this Locale.
        /// </summary>
        public LocaleIdentifier Identifier
        {
            get => m_Identifier;
            set => m_Identifier = value;
        }

        /// <summary>
        /// Optional Metadata. It is possible to attach additional data to the Locale providing
        /// it implements the <see cref="IMetadata"/> interface and is serializable.
        /// </summary>
        public MetadataCollection Metadata
        {
            get => m_Metadata;
            set => m_Metadata = value;
        }

        /// <summary>
        /// The sort order can be used to override the order of Locales when sorted in a list.
        /// If Locales both have the same SortOrder then they will be sorted by name.
        /// </summary>
        public ushort SortOrder
        {
            get => m_SortOrder;
            set => m_SortOrder = value;
        }

        /// <summary>
        /// The name of the Locale.
        /// This can be used to customize how the Locale name should be presented to the user, such as in a language selection menu.
        /// </summary>
        public string LocaleName
        {
            get
            {
                if (!string.IsNullOrEmpty(m_LocaleName))
                    return m_LocaleName;
                if (Identifier.CultureInfo != null)
                    return Identifier.CultureInfo.EnglishName;
                return name;
            }
            set => m_LocaleName = value;
        }

        /// <summary>
        /// Returns the first fallback locale or <see langword="null"/> if one does not exist or it could not be found.
        /// </summary>
        /// <returns>The fallback locale or <see langword="null"/>.</returns>
        [Obsolete("GetFallback is obsolete, please use GetFallbacks.")]
        public virtual Locale GetFallback() => GetFallbacks().GetEnumerator().Current;

        /// <summary>
        /// Returns the fallbacks in order or priority. If the locale does not contain any <see cref="FallbackLocale"/> metadata then the CultureInfo will be used to find a fallback.
        /// </summary>
        /// <returns>The fallback locale or <see langword="null"/>.</returns>
        public IEnumerable<Locale> GetFallbacks()
        {
            if (Metadata == null)
                yield break;

            // Ensure we only return each locale once.
            using (HashSetPool<Locale>.Get(out var processedLocales))
            {
                var entries = Metadata.MetadataEntries;
                for (int i = 0; i < entries.Count; ++i)
                {
                    if (entries[i] is FallbackLocale fallbackLocale)
                    {
                        if (fallbackLocale.Locale != null && !processedLocales.Contains(fallbackLocale.Locale))
                        {
                            processedLocales.Add(fallbackLocale.Locale);
                            yield return fallbackLocale.Locale;
                        }
                    }
                }

                // If we did not find any then revert to using the culture info fallback data.
                if (processedLocales.Count == 0)
                {
                    Locale fallBack = null;
                    var cultureInfo = Identifier.CultureInfo;
                    if (cultureInfo != null)
                    {
                        while (cultureInfo != CultureInfo.InvariantCulture && fallBack == null)
                        {
                            var fb = LocalizationSettings.AvailableLocales.GetLocale(cultureInfo);
                            if (fb != this)
                                fallBack = fb;
                            cultureInfo = cultureInfo.Parent;
                        }
                    }

                    if (fallBack != null)
                        yield return fallBack;
                }
            }
        }

        /// <summary>
        /// When <see langword="true"/>, <see cref="CustomFormatterCode"/> will be used for any culture sensitive formatting instead of <see cref="Identifier"/>.
        /// </summary>
        public bool UseCustomFormatter
        {
            get => m_UseCustomFormatter;
            set
            {
                m_UseCustomFormatter = value;
                m_Formatter = null;
            }
        }

        /// <summary>
        /// The Language code to use when applying any culture specific string formatting, such as date, time, currency.
        /// By default, the <see cref="Identifier"/> Code will be used however this field can be used to override this such as when you
        /// are using a custom Locale which has no known formatter.
        /// </summary>
        public string CustomFormatterCode
        {
            get => m_CustomFormatCultureCode;
            set
            {
                m_CustomFormatCultureCode = value;
                m_Formatter = null;
            }
        }

        /// <summary>
        /// The Formatter that will be applied to any Smart Strings for this Locale.
        /// By default, the <see cref="Identifier"/> [CultureInfo](https://docs.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo) will be used when <see cref="CustomFormatterCode"/> is not set.
        /// </summary>
        public virtual IFormatProvider Formatter
        {
            get
            {
                if (m_Formatter == null)
                    m_Formatter = GetFormatter(UseCustomFormatter, Identifier, CustomFormatterCode);
                return m_Formatter;
            }
            set => m_Formatter = value;
        }

        internal static CultureInfo GetFormatter(bool useCustom, LocaleIdentifier localeIdentifier, string customCode)
        {
            CultureInfo cultureInfo = null;
            if (useCustom)
                cultureInfo = string.IsNullOrEmpty(customCode) ? CultureInfo.InvariantCulture : new LocaleIdentifier(customCode).CultureInfo;

            if (cultureInfo == null)
                cultureInfo = localeIdentifier.CultureInfo;

            return cultureInfo;
        }

        /// <summary>
        /// Create a new <see cref="Locale"/> using the culture code.
        /// </summary>
        /// <param name="code">Culture code.</param>
        /// <returns></returns>
        public static Locale CreateLocale(string code)
        {
            var locale = CreateInstance<Locale>();
            locale.m_Identifier = new LocaleIdentifier(code);
            if (locale.m_Identifier.CultureInfo != null)
            {
                locale.name = locale.m_Identifier.CultureInfo.EnglishName;
            }
            return locale;
        }

        /// <summary>
        /// Create a new <see cref="Locale"/> using the provided <see cref="LocaleIdentifier"/>.
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public static Locale CreateLocale(LocaleIdentifier identifier)
        {
            var locale = CreateInstance<Locale>();
            locale.m_Identifier = identifier;
            if (locale.m_Identifier.CultureInfo != null)
            {
                locale.LocaleName = locale.m_Identifier.CultureInfo.EnglishName;
            }
            return locale;
        }

        /// <summary>
        /// Create a <see cref="Locale"/> using the system language enum value.
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        public static Locale CreateLocale(SystemLanguage language)
        {
            return CreateLocale(new LocaleIdentifier(SystemLanguageConverter.GetSystemLanguageCultureCode(language)));
        }

        /// <summary>
        /// Create a <see cref="Locale"/> using a CultureInfo.
        /// </summary>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public static Locale CreateLocale(CultureInfo cultureInfo)
        {
            return CreateLocale(new LocaleIdentifier(cultureInfo));
        }

        /// <summary>
        /// Compares the Locales properties.
        /// First the sort orders are compared, if they are the same then the <see cref="LocaleName"/> will be considered instead.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(Locale other)
        {
            if (other == null)
                return -1;

            // Sort by the sort order if they are different
            if (SortOrder != other.SortOrder)
            {
                return SortOrder.CompareTo(other.SortOrder);
            }

            // If they are both the same type then use the name to sort
            if (GetType() == other.GetType())
            {
                var result = String.CompareOrdinal(LocaleName, other.LocaleName);
                return result != 0 ? result : GetInstanceID().CompareTo(other.GetInstanceID());
            }

            // Normal Locale's go before PseudoLocale's
            if (other is PseudoLocale)
                return -1;
            return 1;
        }

        public void OnAfterDeserialize()
        {
            m_Formatter = null;
        }

        public void OnBeforeSerialize()
        {
            if (string.IsNullOrEmpty(m_LocaleName))
                m_LocaleName = name;
        }

        /// <summary>
        /// Returns <see cref="LocaleName"/> or name if it is null or empty.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => string.IsNullOrEmpty(LocaleName) ? name : LocaleName;

        /// <summary>
        /// Compares the Locale <see cref="Identifier"/> properties.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Locale other)
        {
            if (other == null)
                return false;
            return LocaleName == other.LocaleName && Identifier.Equals((other.Identifier));
        }
    }
}
