using System;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.SmartFormat.Core.Extensions;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

[Metadata(AllowedTypes = MetadataType.StringTableEntry)]
[Serializable]
public class ItemGender : IMetadata, IMetadataVariable
{
    public enum Gender
    {
        None,
        Female,
        Male
    }

    public Gender gender = Gender.None;

    /// <summary>
    /// The name used to identify this metadata.
    /// </summary>
    public string VariableName => "gender";

    /// <summary>
    /// Returns the value of <see cref="gender"/>.
    /// </summary>
    /// <param name="_"></param>
    /// <returns></returns>
    public object GetSourceValue(ISelectorInfo _) => gender;
}
