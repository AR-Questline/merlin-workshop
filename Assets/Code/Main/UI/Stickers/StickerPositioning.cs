using System;
using UnityEngine;

namespace Awaken.TG.Main.UI.Stickers {
    /// <summary>
    /// Describes where a sticker should appear in relation to its anchor.
    /// </summary>
    [Serializable]
    public struct StickerPositioning {
        /// <summary>
        /// World offset from the center of the anchor.
        /// </summary>
        public Vector3 worldOffset;
        /// <summary>
        /// Screen-space offset, applied after the world offset.
        /// </summary>
        public Vector2 screenOffset;
        /// <summary>
        /// Controls the alignment of the sticker itself related to the anchor point.
        /// </summary>
        public Vector2 pivot;

        /// <summary>
        /// If true, the new sticker will be placed underneath existing ones.
        /// </summary>
        public bool underneath;
    }
}