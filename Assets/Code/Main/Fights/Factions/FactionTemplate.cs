using System;
using System.Collections.Generic;
using Awaken.TG.Assets;
using Awaken.TG.Main.Fights.Factions.FactionEffects;
using Awaken.TG.Main.General;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Stories;
using Awaken.TG.Main.Templates;
using Awaken.TG.Utility.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Awaken.TG.Main.Fights.Factions {
    public class FactionTemplate : Template {
        public FactionTemplate parent;
        
        [Space]
        [SerializeField, LocStringCategory(Category.Faction)] public LocString factionName;
        [SerializeField, LocStringCategory(Category.Faction)] public LocString factionDescription;
        [ShowAssetPreview, ARAssetReferenceSettings(new[] {typeof(Texture2D), typeof(Sprite)}, true, AddressableGroup.StatusEffects)]
        public ShareableSpriteReference iconReference;
        
        [Title("Combat Logic")]
        public List<FactionTemplate> friendly = new();
        public List<FactionTemplate> neutral = new();
        public List<FactionTemplate> hostile = new();
    }
}