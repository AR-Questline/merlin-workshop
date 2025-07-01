# Metadata

Metadata is data that can be used to provide additional information about the item it is attached to. Metadata is serialized using the [SerializeReference](https://docs.unity3d.com/ScriptReference/SerializeReference.html) attribute, which means that it must be marked as serializable, can be shared across the same asset and does **not** inherit from [UnityEngine.Object](https://docs.unity3d.com/ScriptReference/Object.html). Metadata does not have to include serialized fields, it can also be empty and used to tag fields as having certain attributes.

Metadata can be attached to:

- [Locales](Locale.md)
- [String Tables](StringTables.md) and the Table Entries.
- [Asset Tables](AssetTables.md) and the Table Entries.

Metadata can be used for many purposes, such as the following:

- Embedding Font data for strings.
- Storing Component specific values for each Locale, such as different [RectTransform](https://docs.unity3d.com/ScriptReference/RectTransform.html) values to ensure the text is correct for each locale.
- Attaching comments and contextual information which can then be used by translators to provide better localization services.
- Running custom code/scripts. Metadata can contain code to be executed, which can be overridden for specific locales.
- Tagging. Metadata can be used to tag or indicate that the item has particular properties.

## Creating Metadata

Add new Metadata by implementing the [IMetadata](xref:UnityEngine.Localization.Metadata.IMetadata) interface. Any class can be Metadata if it is **serializable** and does not inherit from `UnityEngine.Object`.

`UnityEngine.Objects` can be included in Metadata.
For example, the following Metadata could be used to attach information to a Locale:

[!code-cs[continent-sample]](../DocCodeSamples.Tests/MetadataSamples.cs#continent)]

The **Metadata** attribute is used to add the Continent Metadata to the **Add** list for Locales.

![The Continent Metadata can now be added to Locales.](images/Metadata_Continent.png)
