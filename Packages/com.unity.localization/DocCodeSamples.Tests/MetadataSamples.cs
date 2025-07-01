using System;
using UnityEngine;
using UnityEngine.Localization.Metadata;

#region continent

[Metadata(AllowedTypes = MetadataType.Locale)] // Hint to the editor to only show this type for a Locale
[Serializable]
public class Continent : IMetadata
{
    public string continentName;
    public float areaMillionKm2;
    public float populationSizeInBillions;
}
#endregion

#region region-info

[Metadata(AllowedTypes = MetadataType.Locale)] // Hint to the editor to only show this type for a Locale
[Serializable]
public class RegionInfo : IMetadata
{
    public int population;
    public string capitalCity;
    public string description;
}
#endregion

#region translation-status

[Serializable]
[Metadata(AllowedTypes = MetadataType.StringTableEntry)]
public class TranslationStatus : IMetadata
{
    public enum TranslationState
    {
        Initial,
        Translated,
        Reviewed,
        Final,
    }

    public TranslationState translationStatus = TranslationState.Initial;
}
#endregion

#region locale-icon

[Metadata(AllowedTypes = MetadataType.Locale)]
[Serializable]
public class LocaleIcon : IMetadata
{
    public Texture icon;
}
#endregion
