using System;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace UnityEngine.Localization
{
    /// <summary>
    /// Provides runtime access to a <see cref="StringTable"/> for the current selected <see cref="Locale"/>.
    /// When accessing multiple localized strings it may be more convenient to use a <see cref="LocalizedStringTable"/> instead of multiple <see cref="LocalizedString"/>.
    /// This fetches the table on demand or provides a callback when the table finishes loading, such as returning when the selected locale was changed.
    /// </summary>
    /// <example>
    /// This example shows how a <see cref="StringTable"/> can be used directly in order to get translated strings for multiple entries.
    /// <code source="../../DocCodeSamples.Tests/LocalizedStringTableExample.cs"/>
    /// </example>
    [Serializable]
    public class LocalizedStringTable : LocalizedTable<StringTable, StringTableEntry>
    {
        /// <inheritdoc/>
        protected override LocalizedDatabase<StringTable, StringTableEntry> Database => LocalizationSettings.StringDatabase;

        /// <summary>
        /// Initializes and returns an empty instance of a <see cref="LocalizedStringTable"/>.
        /// </summary>
        public LocalizedStringTable() {}

        /// <summary>
        /// Initializes and returns an instance of a <see cref="LocalizedStringTable"/>.
        /// </summary>
        /// <param name="tableReference">Reference to the String Table Collection.
        /// This can either be the name of the collection as a <see langword="string"/> or the Collection Guid as a [System.Guid](https://docs.microsoft.com/en-us/dotnet/api/system.guid).</param>
        public LocalizedStringTable(TableReference tableReference) => TableReference = tableReference;
    }
}
