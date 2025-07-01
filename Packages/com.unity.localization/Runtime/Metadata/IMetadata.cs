using UnityEngine.Localization.Tables;

namespace UnityEngine.Localization.Metadata
{
    /// <summary>
    /// Interface to indicate a class can be used as Metadata.
    /// Metadata is data that can be used to provide additional information about the item it is attached to.
    /// Metadata can be attached to a <see cref="Locale"/>, a <see cref="LocalizationTable"/>, a table entry or a <see cref="SharedTableData"/>.
    ///
    /// Metadata is serialized using the SerializeReference feature, this means that it must be marked as serializable,
    /// can be shared across the same asset and does not inherit from UnityEngine.Object.
    /// Metadata does not have to include serialized fields, it can also be empty and used to tag fields as having certain attributes.
    /// See also <seealso cref="MetadataAttribute"/>
    /// </summary>
    /// <example>
    /// This example shows how Metadata can be created to attach additional region data to a Locale.
    /// <code source="../../DocCodeSamples.Tests/MetadataSamples.cs" region="translation-status"/>
    /// </example>
    /// <example>
    /// This example shows how Metadata can be created to attach an icon to a Locale.
    /// <code source="../../DocCodeSamples.Tests/MetadataSamples.cs" region="locale-icon"/>
    /// </example>
    #pragma warning disable CA1040 // CA1040: Avoid empty interfaces
    public interface IMetadata {}
    #pragma warning restore CA1040
}
