using System;
using UnityEngine;

namespace Awaken.TG.Main.Templates {
    [Serializable]
    public struct ReadOnlyTemplateReference {
        // it's drawer makes it unable to be change in inspector
        [SerializeField] TemplateReference reference;
        
        [UnityEngine.Scripting.Preserve] public TemplateReference Reference => reference;
        
        public ReadOnlyTemplateReference(TemplateReference reference) {
            this.reference = reference;
        }
    }
}