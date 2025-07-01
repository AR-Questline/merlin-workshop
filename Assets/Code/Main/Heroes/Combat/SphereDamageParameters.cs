using Awaken.Utility;
using System;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Templates;
using Awaken.TG.Utility.Attributes;
using UnityEngine;

namespace Awaken.TG.Main.Heroes.Combat {
    [Serializable]
    public partial struct SphereDamageParameters {
        public ushort TypeForSerialization => SavedTypes.SphereDamageParameters;

        [Saved(0f)] public float duration, endRadius;
        [Saved] public LayerMask hitMask;
        [Saved] public float? defaultDelay;
        [Saved] public Item item;
        [Saved(false)] public bool disableFriendlyFire;
        [Saved] public float? overridenRandomnessModifier;
        [Saved] public TemplateReference onHitStatusTemplate;
        [Saved(0)] public int onHitStatusBuildup;
        [Saved] public RawDamageData rawDamageData;
        [Saved] public DamageParameters baseDamageParameters;
    }
}