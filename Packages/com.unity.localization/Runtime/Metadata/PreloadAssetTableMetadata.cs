using System;
using UnityEngine.Localization.Tables;

namespace UnityEngine.Localization.Metadata
{
    /// <summary>
    /// Used when an <see cref="AssetTable"/> is marked as [preload](../manual/AssetTables.html#preloading) to indicate if the assets within should all be preloaded when the table is loaded or should be loaded on demand.
    /// If no <see cref="PreloadAssetTableMetadata"/> is attached to a <see cref="AssetTable"/> then the default behavior is <see cref="PreloadBehaviour.PreloadAll"/>.
    /// </summary>
    [Metadata(AllowedTypes = MetadataType.AssetTable | MetadataType.SharedTableData, MenuItem = "Preload Assets")]
    [Serializable]
    public class PreloadAssetTableMetadata : IMetadata
    {
        /// <summary>
        /// The preload behavior to be applied to the <see cref="AssetTable"/>.
        /// </summary>
        public enum PreloadBehaviour
        {
            /// <summary>
            /// Override that will stop any preloading on this table including entries that have <see cref="PreloadAssetTableMetadata"/> metadata.
            /// </summary>
            NoPreload,

            /// <summary>
            /// Preload all assets in the table.
            /// </summary>
            PreloadAll,
        }

        [SerializeField]
        PreloadBehaviour m_PreloadBehaviour;

        /// <summary>
        /// <inheritdoc cref="PreloadBehaviour"/>
        /// </summary>
        public PreloadBehaviour Behaviour
        {
            get => m_PreloadBehaviour;
            set => m_PreloadBehaviour = value;
        }
    }
}
