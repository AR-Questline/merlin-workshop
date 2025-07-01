using System;

namespace UnityEngine.Localization.Metadata
{
    /// <summary>
    /// Flags that can be used to indicate where the Metadata can be added in the Editor.
    /// </summary>
    [Flags]
    public enum MetadataType
    {
        /// <summary>
        /// Metadata can be added to a <see cref="Locale"/>.
        /// </summary>
        Locale = 1,

        /// <summary>
        /// Metadata can be added to a <see cref="SharedTableData"/>.
        /// </summary>
        SharedTableData = 2,

        /// <summary>
        /// Metadata can be added to a <see cref="StringTable"/>.
        /// </summary>
        StringTable = 4,

        /// <summary>
        /// Metadata can be added to a <see cref="AssetTable"/>.
        /// </summary>
        AssetTable = 8,

        /// <summary>
        /// Metadata can be added to a <see cref="StringTable"/> entry.
        /// </summary>
        StringTableEntry = 16,

        /// <summary>
        /// Metadata can be added to a <see cref="AssetTable"/> entry.
        /// </summary>
        AssetTableEntry = 32,

        /// <summary>
        /// Metadata can be added to the Shared <see cref="StringTable"/> entry.
        /// This will be accessible by all locales for the chosen Entry.
        /// </summary>
        SharedStringTableEntry = 64,

        /// <summary>
        /// Metadata can be added to the Shared <see cref="AssetTable"/> entry.
        /// This will be accessible by all locales for the chosen Entry.
        /// </summary>
        SharedAssetTableEntry = 128,

        /// <summary>
        /// Metadata can be added to the <see cref="LocalizationSettings"/>.
        /// </summary>
        LocalizationSettings = 256,

        /// <summary>
        /// Metadata can be added to any table type.
        /// </summary>
        AllTables = StringTable | AssetTable,

        /// <summary>
        /// Metadata can be added to any table entry.
        /// </summary>
        AllTableEntries = StringTableEntry | AssetTableEntry,

        /// <summary>
        /// Metadata can be added to any shared table entry.
        /// This will be accessible by all locales for the chosen Entry.
        /// </summary>
        AllSharedTableEntries = SharedStringTableEntry | SharedAssetTableEntry,

        /// <summary>
        /// Metadata can be added to anything.
        /// </summary>
        All = AllTables | AllTableEntries | Locale | SharedTableData | AllSharedTableEntries | LocalizationSettings
    }

    [AttributeUsage(AttributeTargets.Field)]
    class MetadataTypeAttribute : PropertyAttribute
    {
        /// <summary>
        /// <inheritdoc cref="MetadataType"/>
        /// </summary>
        public MetadataType Type { get; set; }

        /// <summary>
        /// <inheritdoc cref="MetadataTypeAttribute"/>
        /// </summary>
        /// <param name="type"></param>
        public MetadataTypeAttribute(MetadataType type)
        {
            Type = type;
        }
    }

    /// <summary>
    /// Provides a hint to the editor on where this metadata can be used.
    /// This is only used in the Editor and not considered when adding Metadata through script.
    /// </summary>
    /// <example>
    /// This example shows how a Translation Status Metadata could be created.
    /// This will only appear in the Metadata menu for String Table Entries.
    /// ![](../manual/images/scripting/TranslationStatusMetadata.png)
    /// <code source="../../DocCodeSamples.Tests/MetadataSamples.cs" region="translation-status"/>
    /// </example>
    [AttributeUsage(AttributeTargets.Class)]
    public class MetadataAttribute : Attribute
    {
        /// <summary>
        /// Name to use in the add Metadata menu. If empty then the class name will be used.
        /// </summary>
        public string MenuItem { get; set; }

        /// <summary>
        /// Should it be possible to have more than 1 instance?
        /// </summary>
        public bool AllowMultiple { get; set; } = true;

        /// <summary>
        /// Describes where the Metadata can be added. This is used when generating the add Metadata menu.
        /// </summary>
        public MetadataType AllowedTypes { get; set; } = MetadataType.All;
    }
}
