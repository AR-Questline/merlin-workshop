using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;

namespace Awaken.TG.Main.Heroes.Items {
    public class ItemRequirementStatType : StatType<Item> {
        [UnityEngine.Scripting.Preserve] public Func<ShareableSpriteReference> icon;
        
        protected ItemRequirementStatType(string id, string displayName, string statDescription, Func<Item, Stat> getter, Func<ShareableSpriteReference> icon,
            string inspectorCategory = "", Param param = null) : base(id, displayName, getter, inspectorCategory, new Param { Description = statDescription }) {
            this.icon = icon;
        }
        
        public static readonly ItemRequirementStatType
            StrengthRequired = new(nameof(StrengthRequired), LocTerms.RPGStrength, LocTerms.RPGStrengthDescription, static i => i.StatsRequirements.StrengthRequired, static () => CommonReferences.Get.strengthIcon),
            DexterityRequired = new(nameof(DexterityRequired), LocTerms.RPGDexterity, LocTerms.RPGDexterityDescription, static i => i.StatsRequirements.DexterityRequired, static () => CommonReferences.Get.dexterityIcon),
            SpiritualityRequired = new(nameof(SpiritualityRequired), LocTerms.RPGSpirituality, LocTerms.RPGSpiritualityDescription, static i => i.StatsRequirements.SpiritualityRequired, static () => CommonReferences.Get.spiritualityIcon),
            
            PerceptionRequired = new(nameof(PerceptionRequired), LocTerms.RPGPerception, LocTerms.RPGPerceptionDescription, static i => i.StatsRequirements.PerceptionRequired, static () => CommonReferences.Get.perceptionIcon),
            EnduranceRequired = new(nameof(EnduranceRequired), LocTerms.RPGEndurance, LocTerms.RPGEnduranceDescription, static i => i.StatsRequirements.EnduranceRequired, static () => CommonReferences.Get.enduranceIcon),
            PracticalityRequired = new(nameof(PracticalityRequired), LocTerms.RPGPracticality, LocTerms.RPGPracticalityDescription, static i => i.StatsRequirements.PracticalityRequired, static () => CommonReferences.Get.practicalityIcon);
    }
}