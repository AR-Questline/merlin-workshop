using System;

namespace UnityEngine.Localization
{
    /// <summary>
    /// Specify a custom name to be used when displayed in the Editor.
    /// </summary>
    /// <example>
    /// This example shows how Metadata can be given a custom name.
    /// <code source="../../DocCodeSamples.Tests/DisplayNameAttributeSample.cs"/>
    /// </example>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class DisplayNameAttribute : Attribute
    {
        /// <summary>
        /// The custom name to use when displayed in the Editor.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Path to a Texture file to display as an icon.
        /// </summary>
        public string IconPath { get; set; }

        /// <summary>
        /// Specify a custom name to display in the Editor.
        /// </summary>
        /// <param name="name">The name to display.</param>
        /// <param name="iconPath">Optional icon to display when possible.</param>
        public DisplayNameAttribute(string name, string iconPath = null)
        {
            Name = name;
            IconPath = iconPath;
        }
    }
}
