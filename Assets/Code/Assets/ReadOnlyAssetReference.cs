using System;
using UnityEngine;

namespace Awaken.TG.Assets {
    [Serializable]
    public struct ReadOnlyAssetReference {
        // it's drawer makes it unable to be change in inspector
        [SerializeField] ARAssetReference reference;
        
        [UnityEngine.Scripting.Preserve] public ARAssetReference Reference => reference;
        
        public ReadOnlyAssetReference(ARAssetReference reference) {
            this.reference = reference;
        }
    }
}