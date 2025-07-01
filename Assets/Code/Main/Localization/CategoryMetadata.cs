using System;
using UnityEngine;
using UnityEngine.Localization.Metadata;

namespace Awaken.TG.Main.Localization {
    /// <summary>
    /// Used to categorize localizations for translators
    /// </summary>
    [Metadata]
    [Serializable]
    public class CategoryMetadata : IMetadata {
        [SerializeField] [TextArea(1, int.MaxValue)]
        string category = "";

        public CategoryMetadata() {
            CategoryText = string.Empty;
        }

        public CategoryMetadata(string value = "") {
            CategoryText = value;
        }
        
        public static implicit operator string(CategoryMetadata s) {
            return s?.CategoryText ?? string.Empty;
        }

        /// <summary>
        /// The comment text.
        /// </summary>
        public string CategoryText {
            get => category;
            set => category = value;
        }
    }
}