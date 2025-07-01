using System;

namespace UnityEditor.Localization
{
    /// <summary>
    /// Indicates that the <see cref="CollectionExtension"/> can be added to a <see cref="StringTableCollection"/>
    /// and will appear in the add extension menu.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class StringTableCollectionExtensionAttribute : Attribute {}

    /// <summary>
    /// Indicates that the <see cref="CollectionExtension"/> can be added to a <see cref="AssetTableCollection"/>
    /// and will appear in the add extension menu.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AssetTableCollectionExtensionAttribute : Attribute {}
}
