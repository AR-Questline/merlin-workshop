using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Utility.RichEnums;

namespace Awaken.TG.Main.General.StatTypes {
    [RichEnumDisplayCategory("_RPG")]
    public class HeroRPGStatType : HeroStatType {

        public Func<ShareableSpriteReference> icon;

        protected HeroRPGStatType(string id, string displayName, string statDescription, Func<Hero, Stat> getter, Func<ShareableSpriteReference> icon,
            string inspectorCategory = "") : base(id, displayName, getter, inspectorCategory, new Param { Description = statDescription }) {
            this.icon = icon;
        }

        public static readonly HeroRPGStatType
            Strength = new(nameof(Strength), LocTerms.RPGStrength, LocTerms.RPGStrengthDescription, static h => h.HeroRPGStats.Strength, static () => CommonReferences.Get.strengthIcon),
            Dexterity = new(nameof(Dexterity), LocTerms.RPGDexterity, LocTerms.RPGDexterityDescription, static h => h.HeroRPGStats.Dexterity, static () => CommonReferences.Get.dexterityIcon),
            Spirituality = new(nameof(Spirituality), LocTerms.RPGSpirituality, LocTerms.RPGSpiritualityDescription, static h => h.HeroRPGStats.Spirituality, static () => CommonReferences.Get.spiritualityIcon),
            
            Perception = new(nameof(Perception), LocTerms.RPGPerception, LocTerms.RPGPerceptionDescription, static h => h.HeroRPGStats.Perception, static () => CommonReferences.Get.perceptionIcon),
            Endurance = new(nameof(Endurance), LocTerms.RPGEndurance, LocTerms.RPGEnduranceDescription, static h => h.HeroRPGStats.Endurance, static () => CommonReferences.Get.enduranceIcon),
            Practicality = new(nameof(Practicality), LocTerms.RPGPracticality, LocTerms.RPGPracticalityDescription, static h => h.HeroRPGStats.Practicality, static () => CommonReferences.Get.practicalityIcon);
    }
}