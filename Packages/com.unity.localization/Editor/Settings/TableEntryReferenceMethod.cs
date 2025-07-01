using UnityEngine;

namespace UnityEditor.Localization
{
    /// <summary>
    /// Adds a reference to a table in the Editor.
    /// </summary>
    public enum TableReferenceMethod
    {
        /// <summary>
        /// References the table by its Guid. This is the default and most robust method.
        /// </summary>
        [Tooltip("References the table by its Guid. This is the default and most robust method.")]
        Guid,

        /// <summary>
        /// References the table by its name. Only use this method if the table name won't change.
        /// </summary>
        [Tooltip("References the table by its name. Only use this method if the table name won't change.")]
        Name,
    }

    /// <summary>
    /// Adds a reference to a table entry in the Editor.
    /// </summary>
    public enum EntryReferenceMethod
    {
        /// <summary>
        /// References the entry by its ID. This is the default and most robust method.
        /// </summary>
        [Tooltip("References the entry by its ID. This is the default and most robust method.")]
        Id,

        /// <summary>
        ///  References the entry by its name. Only use this method if the entry name won't change.
        /// </summary>
        [Tooltip("References the entry by its name. Only use this method if the entry name won't change.")]
        Key,
    }
}
