using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;

/// <summary>
/// When displayed in the Inspector, such as the add metadata menu, the name will be displayed as "My Custom Name".
/// By default the name would have been "Metadata With Display Name".
/// </summary>
[DisplayName("My Custom Name")]
[Serializable]
public class MetadataWithDisplayName : IMetadata {}
