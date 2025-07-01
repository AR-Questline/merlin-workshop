using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.MVC;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.General.StatTypes {
    public class ProficiencyCategory : RichEnum {
        static CommonReferences CommonReferences => World.Services.Get<CommonReferences>();
        
        public LocString DisplayName { get; }
        public Func<ShareableSpriteReference> Icon { get; }

        private ProficiencyCategory(string id, string displayName, Func<ShareableSpriteReference> icon, string inspectorCategory = "") : base(id, inspectorCategory) {
            DisplayName = new LocString { ID = displayName };
            Icon = icon;
        }

        public static readonly ProficiencyCategory
            Strength = new(nameof(Strength), LocTerms.RPGStrength, () => CommonReferences.strengthIcon),
            Endurance = new(nameof(Endurance), LocTerms.RPGEndurance, () => CommonReferences.enduranceIcon),
            Dexterity = new(nameof(Dexterity), LocTerms.RPGDexterity, () => CommonReferences.dexterityIcon),
            Spirituality = new(nameof(Spirituality), LocTerms.RPGSpirituality, () => CommonReferences.spiritualityIcon),
            Practicality = new(nameof(Practicality), LocTerms.RPGPracticality, () => CommonReferences.practicalityIcon);
    }
}