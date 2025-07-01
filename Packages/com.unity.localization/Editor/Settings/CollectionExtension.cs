using UnityEngine;
using System;

namespace UnityEditor.Localization
{
    /// <summary>
    /// Allows for attaching additional data or functionality to a <see cref="StringTableCollection"/> or <see cref="AssetTableCollection"/>.
    /// </summary>
    [Serializable]
    public class CollectionExtension
    {
        [SerializeField, HideInInspector]
        LocalizationTableCollection m_Collection;

        /// <summary>
        /// The collection this extension is attached to.
        /// </summary>
        public LocalizationTableCollection TargetCollection
        {
            get => m_Collection;
            internal set => m_Collection = value;
        }

        /// <summary>
        /// Called when the Extension is first added to the table collection.
        /// </summary>
        public virtual void Initialize() {}

        /// <summary>
        /// Called when the Extension is removed from the table collection.
        /// </summary>
        public virtual void Destroy() {}
    }
}
