using System;

namespace UnityEditor.Localization
{
    /// <summary>
    /// Provides support for adding a menu item to the Localization Tables window Import dropdown.
    /// The method must be static and have the parameters <see cref="LocalizationTableCollection"/> and <see cref="UnityEngine.UIElements.DropdownMenu"/>.
    /// </summary>
    /// <example>
    /// The following example shows how to export and import a <see cref="StringTableCollection"/> as JSON.
    /// <code source="../../DocCodeSamples.Tests/LocalizationTablesWindowPopulateMenu.cs"/>
    /// </example>
    [AttributeUsage(AttributeTargets.Method)]
    public class LocalizationImportMenuAttribute : Attribute {}

    /// <summary>
    /// Provides support for adding a menu item to the Localization Tables window Export dropdown.
    /// The method must be static and have the parameters <see cref="LocalizationTableCollection"/> and <see cref="UnityEngine.UIElements.DropdownMenu"/>.
    /// </summary>
    /// <example>
    /// The following example shows how to export and import a <see cref="StringTableCollection"/> as JSON.
    /// <code source="../../DocCodeSamples.Tests/LocalizationTablesWindowPopulateMenu.cs"/>
    /// </example>
    [AttributeUsage(AttributeTargets.Method)]
    public class LocalizationExportMenuAttribute : Attribute {}

    /// <summary>
    /// Provides support for adding a menu item to the Localization Tables window entry menu.
    /// </summary>
    /// <example>
    /// The following example shows how to add a menu item that can be used to toggle a requires translating tag on a <see cref="StringTableEntry"/>.
    /// <code source="../../DocCodeSamples.Tests/LocalizationTablesWindowPopulateEntryMenu.cs"/>
    /// </example>
    [AttributeUsage(AttributeTargets.Method)]
    public class LocalizationEntryMenuAttribute : Attribute {}
}
