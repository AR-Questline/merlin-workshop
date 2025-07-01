using System;
using UnityEngine;

namespace Awaken.TG.Main.Templates {
    [Serializable]
    public class TemplateMetadata {
        [SerializeField] bool verifiedArt;
        [SerializeField] bool verifiedDesign;
        [SerializeField] string notes;
    }
}