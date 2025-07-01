using System;
using UnityEngine.Localization.Tables;

namespace UnityEngine.Localization.Metadata
{
    /// <summary>
    /// Indicates that the <see cref="StringTableEntry"/> should use SmartFormat when localizing.
    /// </summary>
    [Serializable]
    [HideInInspector] // Used to indicate the item should be readonly.
    public class SmartFormatTag : SharedTableEntryMetadata
    {
    }
}
