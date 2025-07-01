using System;

namespace UnityEngine.Localization.Metadata
{
    /// <summary>
    /// Used to prevent an entry from being exported to Google Sheets, XLIFF and CSV.
    /// </summary>
    [Metadata(AllowedTypes = MetadataType.AllSharedTableEntries)]
    [Serializable]
    public class ExcludeEntryFromExport : IMetadata {}
}
