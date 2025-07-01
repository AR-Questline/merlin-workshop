using System;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;
using UnityEngine;

namespace Awaken.TG.Main.Locations.Actions {
    [Serializable]
    public struct ItemRequirementData {
        [TemplateType(typeof(ItemTemplate))] public TemplateReference itemReference;
        public bool requireOnce;
        public bool consumeOnUse;
        public bool hideInteractionUntilMet;
        [Min(1)] public int quantity;
    }
}