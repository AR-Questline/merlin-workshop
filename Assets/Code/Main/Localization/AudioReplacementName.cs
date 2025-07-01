using System;
using UnityEngine;
using UnityEngine.Localization.Metadata;

namespace Awaken.TG.Main.Localization {
    [Metadata]
    [Serializable]
    public class AudioReplacementName : IMetadata {
        [SerializeField] [TextArea(1, int.MaxValue)]
        string audioReplacementName = "";
        [SerializeField]
        int translationHash;

        public AudioReplacementName() {
            AudioReplacement = string.Empty;
        }

        public static implicit operator string(AudioReplacementName s) {
            return s?.AudioReplacement ?? string.Empty;
        }

        /// <summary>
        /// The comment text.
        /// </summary>
        public string AudioReplacement {
            get => audioReplacementName;
            set => audioReplacementName = value;
        }
        
        public int TranslationHash {
            get => translationHash;
            set => translationHash = value;
        }
    }
}