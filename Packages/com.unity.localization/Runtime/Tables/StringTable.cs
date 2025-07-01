using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Pseudo;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat;
using UnityEngine.Localization.SmartFormat.Core.Formatting;
using UnityEngine.Localization.SmartFormat.GlobalVariables;

namespace UnityEngine.Localization.Tables
{
    /// <summary>
    /// A <see cref="StringTable"/> entry.
    /// </summary>
    public class StringTableEntry : TableEntry
    {
        FormatCache m_FormatCache;

        /// <summary>
        /// Used when <see cref="IsSmart"/> is true and <see cref="GetLocalizedString"/> is called.
        /// Contains information about the format including any <see cref="IGlobalVariableValueChanged"/> that were used.
        /// </summary>
        public FormatCache FormatCache
        {
            get => m_FormatCache;
            set => m_FormatCache = value;
        }

        /// <summary>
        /// The raw localized value without any formatting applied.
        /// </summary>
        public string Value
        {
            get => Data.Localized;
            set
            {
                Data.Localized = value;

                if (m_FormatCache != null)
                {
                    FormatCachePool.Release(m_FormatCache);
                    m_FormatCache = null;
                }
            }
        }

        /// <summary>
        /// Is the entry marked with the <see cref="SmartFormatTag"/>?
        /// Entries that are smart will use <see cref="SmartFormat"/> to format the localized text.
        /// </summary>
        public bool IsSmart
        {
            get => HasTagMetadata<SmartFormatTag>() || Data.Metadata.GetMetadata<SmartFormatTag>() != null;
            set
            {
                if (value)
                {
                    if (m_FormatCache != null)
                    {
                        FormatCachePool.Release(m_FormatCache);
                        m_FormatCache = null;
                    }
                    AddTagMetadata<SmartFormatTag>();
                }
                else
                {
                    RemoveTagMetadata<SmartFormatTag>();
                }
            }
        }

        internal StringTableEntry()
        {
        }

        /// <summary>
        /// Attempts to remove the entry from the <see cref="StringTable"/> that it belongs to.
        /// If <see cref="TableEntry.Table"/> is null then a warning will be produced.
        /// </summary>
        public void RemoveFromTable()
        {
            var stringTable = Table as StringTable;
            if (stringTable == null)
            {
                Debug.LogWarning($"Failed to remove {nameof(StringTableEntry)} with id {KeyId} and value `{Value}` as it does not belong to a table.");
            }
            else
            {
                stringTable.Remove(KeyId);
            }
        }

        internal FormatCache GetOrCreateFormatCache()
        {
            if (!IsSmart)
                return null;

            if (m_FormatCache == null && !string.IsNullOrEmpty(Data.Localized))
            {
                m_FormatCache = FormatCachePool.Get(LocalizationSettings.StringDatabase.SmartFormatter.Parser.ParseFormat(Data.Localized, LocalizationSettings.StringDatabase.SmartFormatter.GetNotEmptyFormatterExtensionNames()));
                m_FormatCache.Table = Table;
            }
            return m_FormatCache;
        }

        /// <summary>
        /// Returns the localized text after formatting has been applied.
        /// This will use SmartFormat if <see cref="IsSmart"/> is true else it will return the raw unformatted value.
        /// </summary>
        /// <remarks>
        /// The following process is applied when generating a localized string:
        /// ![](../manual/images/scripting/ProcessStringEntry.dot.svg)
        /// </remarks>
        /// <returns></returns>
        public string GetLocalizedString() => GetLocalizedString(null, null, LocalizationSettings.SelectedLocaleAsync.Result as PseudoLocale);

        /// <summary>
        /// Returns the localized text after formatting has been applied.
        /// Formatting will use SmartFormat if <see cref="IsSmart"/> is true else it will default to String.Format.
        /// </summary>
        /// <param name="args">Arguments that will be applied to Smart Format or <c>String.Format.</c></param>
        /// <returns></returns>
        public string GetLocalizedString(params object[] args) => GetLocalizedString(null, args, LocalizationSettings.SelectedLocaleAsync.Result as PseudoLocale);

        /// <summary>
        /// Returns the localized text after formatting has been applied.
        /// Formatting will use SmartFormat if <see cref="IsSmart"/> is true else it will default to String.Format.
        /// </summary>
        /// <param name="args">Arguments that will be applied to Smart Format or <c>String.Format.</c></param>
        /// <returns></returns>
        public string GetLocalizedString(IList<object> args) => GetLocalizedString(null, args, LocalizationSettings.SelectedLocaleAsync.Result as PseudoLocale);

        /// <summary>
        /// Returns the localized text after formatting has been applied.
        /// Formatting will use SmartFormat is <see cref="IsSmart"/> is true else it will default to String.Format.
        /// </summary>
        /// <param name="formatProvider">Custom format provider used with String.Format and smart strings.
        /// If formatProvider is <see langword="null"/>, RemoveFromTable uses the <see cref="LocalizationTable.LocaleIdentifier"/>'s <see cref="Locale.Formatter"/>.</param>
        /// <param name="args">Arguments that will be applied to Smart Format or <c>String.Format.</c></param>
        /// <returns></returns>
        public string GetLocalizedString(IFormatProvider formatProvider, IList<object> args) => GetLocalizedString(formatProvider, args, LocalizationSettings.SelectedLocaleAsync.Result as PseudoLocale);

        /// <summary>
        /// Returns the localized text after formatting has been applied.
        /// Formatting will use SmartFormat is <see cref="IsSmart"/> is true else it will default to String.Format.
        /// </summary>
        /// <param name="formatProvider">Custom format provider used with String.Format and smart strings.
        /// If formatProvider is <see langword="null"/>, RemoveFromTable uses the <see cref="LocalizationTable.LocaleIdentifier"/>'s <see cref="Locale.Formatter"/>.</param>
        /// <param name="args">Arguments that are be applied to Smart Format or <c>String.Format.</c></param>
        /// <param name="pseudoLocale">Optional <see cref="PseudoLocale"/> that will be applied to the final string.</param>
        /// <returns></returns>
        public string GetLocalizedString(IFormatProvider formatProvider, IList<object> args, PseudoLocale pseudoLocale)
        {
            if (formatProvider == null)
                formatProvider = LocalizationSettings.AvailableLocales?.GetLocale(Table.LocaleIdentifier)?.Formatter;

            string translatedText = null;

            if (IsSmart)
            {
                #if UNITY_EDITOR
                if (!LocalizationSettings.Instance.IsPlayingOrWillChangePlaymode)
                {
                    var localVariables = m_FormatCache?.LocalVariables;
                    m_FormatCache = null;
                    m_FormatCache = GetOrCreateFormatCache();
                    if (m_FormatCache != null)
                        m_FormatCache.LocalVariables = localVariables;
                }
                #endif

                if (m_FormatCache == null)
                    m_FormatCache = GetOrCreateFormatCache();

                translatedText = LocalizationSettings.StringDatabase.SmartFormatter.FormatWithCache(ref m_FormatCache, Data.Localized, formatProvider, args);
            }
            else if (!string.IsNullOrEmpty(Data.Localized))
            {
                if (args != null && args.Count > 0)
                {
                    try
                    {
                        translatedText = formatProvider == null ? string.Format(Data.Localized, args as object[] ?? args.ToArray()) : string.Format(formatProvider, Data.Localized, args as object[] ?? args.ToArray());
                    }
                    catch (FormatException fe)
                    {
                        // Supplement with a better error message as its likely that the string was a Smart String.
                        throw new FormatException($"Input string was not in the correct format for String.Format. Ensure that the string is marked as Smart if you intended to use Smart Format.\n`{Data.Localized}`\n{fe}", fe);
                    }
                }
                else
                    translatedText = Data.Localized;
            }

            if (pseudoLocale != null && !string.IsNullOrEmpty(translatedText))
                translatedText = pseudoLocale.GetPseudoString(translatedText);

            return translatedText;
        }
    }

    /// <summary>
    /// A table that stores localized strings for a specific <see cref="Locale"/>.
    /// </summary>
    public class StringTable : DetailedLocalizationTable<StringTableEntry>
    {
        /// <summary>
        /// Returns the unique characters used by all entries in this table.
        /// This will also include Smart String entries but will only consider the <see cref="UnityEngine.Localization.SmartFormat.Core.Parsing.LiteralText"/> values,
        /// it will not consider <see cref="UnityEngine.Localization.SmartFormat.Core.Parsing.Placeholder"/> values.
        /// </summary>
        /// <returns></returns>
        public string GenerateCharacterSet()
        {
            var literals = CollectLiteralCharacters();

            // Sort the output so the results are more deterministic.
            var sorted = literals.Distinct().OrderBy(c => c);

            return string.Concat(sorted);
        }

        internal IEnumerable<char> CollectLiteralCharacters()
        {
            IEnumerable<char> e = "";
            var smart = new SmartFormatterLiteralCharacterExtractor(LocalizationSettings.StringDatabase?.SmartFormatter);
            foreach (var entry in Values)
            {
                if (entry.IsSmart)
                {
                    e = e.Concat(smart.ExtractLiteralsCharacters(entry.LocalizedValue));
                }
                else
                {
                    e = e.Concat(entry.LocalizedValue.AsEnumerable());
                }
            }
            return e;
        }

        /// <summary>
        /// Creates a new, empty StringTableEntry.
        /// </summary>
        /// <returns></returns>
        public override StringTableEntry CreateTableEntry()
        {
            return new StringTableEntry() { Table = this, Data = new TableEntryData() };
        }
    }
}
