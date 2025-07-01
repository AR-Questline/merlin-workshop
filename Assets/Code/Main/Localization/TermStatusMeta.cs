using System;
using UnityEngine;
using UnityEngine.Localization.Metadata;

namespace Awaken.TG.Main.Localization {
    [Metadata]
    [Serializable]
    public class TermStatusMeta : IMetadata {
        [SerializeField]
        int translationHash;
        [SerializeField] 
        int proofreadHash;
        
        public int TranslationHash {
            get => translationHash;
            set => translationHash = value;
        }
        
        public int ProofreadHash {
            get => proofreadHash;
            set => proofreadHash = value;
        }
    }
}