using System;
using UnityEngine;
using UnityEngine.Localization.Metadata;

namespace Awaken.TG.Main.Localization {
    /// <summary>
    /// Used as Comment field
    /// </summary>
    [Metadata]
    [Serializable]
    public class GestureMetadata : IMetadata {
        [SerializeField] [TextArea(1, int.MaxValue)]
        string description = "";

        public GestureMetadata() {
            GestureKey = string.Empty;
        }

        public GestureMetadata(string value = "") {
            GestureKey = value;
        }
        
        public static implicit operator string(GestureMetadata s) {
            return s?.GestureKey ?? string.Empty;
        }

        /// <summary>
        /// The comment text.
        /// </summary>
        public string GestureKey {
            get => description;
            set => description = value;
        }
    }
}