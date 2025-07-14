using System;
using Awaken.TG.Assets;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Scenes.SceneConstructors;
using Awaken.TG.Main.Utility.RichEnums;
using Awaken.TG.MVC;

namespace Awaken.TG.Main.General.StatTypes {
    [RichEnumDisplayCategory("Character")]
    public class ProfStatType : HeroStatType {
        static CommonReferences CommonReferences => World.Services.Get<CommonReferences>();
        
        public bool UseStrength { get; }
        public CharacterStatType MultiStat { get; }
        public ProficiencyCategory Category { get; }
        public Func<ShareableSpriteReference> GetIcon { get; }

        ProfStatType(string id, string displayName, string description, Func<Hero, Stat> getter, ProficiencyCategory category = null, Func<ShareableSpriteReference> getIcon = null, bool useStrength = false, CharacterStatType multiStat = null,
            string inspectorCategory = "Proficiencies") : base(id, displayName, getter, inspectorCategory, new Param {Description = description}) {
            UseStrength = useStrength;
            MultiStat = multiStat;
            Category = category;
            GetIcon = getIcon;
        }

        public static readonly ProfStatType
            // === Proficiency stats

            // Strength
            OneHanded = new(nameof(OneHanded), LocTerms.OneHanded, LocTerms.OneHandedDescription, h => h.ProficiencyStats.OneHanded, ProficiencyCategory.Strength, () => CommonReferences.oneHandedIcon, true, multiStat: CharacterStatType.OneHandedMeleeDamageMultiplier),
            TwoHanded = new(nameof(TwoHanded), LocTerms.TwoHanded, LocTerms.TwoHandedDescription, h => h.ProficiencyStats.TwoHanded, ProficiencyCategory.Strength, () => CommonReferences.twoHandedIcon, true, multiStat: CharacterStatType.TwoHandedMeleeDamageMultiplier),
            Unarmed = new(nameof(Unarmed), LocTerms.Unarmed, LocTerms.UnarmedDescription, h => h.ProficiencyStats.Unarmed, ProficiencyCategory.Strength, () => CommonReferences.unarmedIcon, true, multiStat: CharacterStatType.UnarmedMeleeDamageMultiplier),
            Shield = new(nameof(Shield), LocTerms.Shield, LocTerms.ShieldDescription, h => h.ProficiencyStats.Shield, ProficiencyCategory.Strength, () => CommonReferences.shieldIcon, true),
            Athletics = new(nameof(Athletics), LocTerms.Athletics, LocTerms.AthleticsDescription, h => h.ProficiencyStats.Athletics, ProficiencyCategory.Strength, () => CommonReferences.athleticsIcon),

            // Armors
            LightArmor = new(nameof(LightArmor), LocTerms.LightArmor, LocTerms.LightArmorDescription, h => h.ProficiencyStats.LightArmor, ProficiencyCategory.Endurance, () => CommonReferences.lightArmorIcon),
            MediumArmor = new(nameof(MediumArmor), LocTerms.MediumArmor, LocTerms.MediumArmorDescription, h => h.ProficiencyStats.MediumArmor, ProficiencyCategory.Endurance, () => CommonReferences.mediumArmorIcon),
            HeavyArmor = new(nameof(HeavyArmor), LocTerms.HeavyArmor, LocTerms.HeavyArmorDescription, h => h.ProficiencyStats.HeavyArmor, ProficiencyCategory.Endurance, () => CommonReferences.heavyArmorIcon),

            // Dexterity
            Archery = new(nameof(Archery), LocTerms.Archery, LocTerms.ArcheryDescription, h => h.ProficiencyStats.Archery, ProficiencyCategory.Dexterity, () => CommonReferences.archeryIcon, true, multiStat: CharacterStatType.RangedDamageMultiplier),
            Evasion = new(nameof(Evasion), LocTerms.ProficiencyEvasion, LocTerms.EvasionDescription, h => h.ProficiencyStats.Evasion, ProficiencyCategory.Dexterity, () => CommonReferences.evasionIcon),
            Acrobatics = new(nameof(Acrobatics), LocTerms.Acrobatics, LocTerms.AcrobaticsDescription, h => h.ProficiencyStats.Acrobatics, ProficiencyCategory.Dexterity, () => CommonReferences.acrobaticsIcon),

            Sneak = new(nameof(Sneak), LocTerms.Sneak, LocTerms.SneakDescription, h => h.ProficiencyStats.Sneak, ProficiencyCategory.Dexterity, () => CommonReferences.sneakIcon),
            Theft = new(nameof(Theft), LocTerms.Theft, LocTerms.TheftDescription, h => h.ProficiencyStats.Theft, ProficiencyCategory.Dexterity, () => CommonReferences.theftIcon),

            // Intelligence
            Magic = new(nameof(Magic), LocTerms.Magic, LocTerms.MagicDescription, h => h.ProficiencyStats.Magic, ProficiencyCategory.Spirituality, () => CommonReferences.magicIcon, multiStat: CharacterStatType.MagicStrength),

            // Crafting
            Alchemy = new(nameof(Alchemy), LocTerms.Alchemy, LocTerms.AlchemyDescription, h => h.ProficiencyStats.Alchemy, ProficiencyCategory.Practicality, () => CommonReferences.alchemyIcon),
            Cooking = new(nameof(Cooking), LocTerms.Cooking, LocTerms.CookingDescription, h => h.ProficiencyStats.Cooking, ProficiencyCategory.Practicality, () => CommonReferences.cookingIcon),
            Handcrafting = new(nameof(Handcrafting), LocTerms.Handcrafting, LocTerms.HandcraftingDescription, h => h.ProficiencyStats.Handcrafting, ProficiencyCategory.Practicality, () => CommonReferences.handcraftingIcon),
            
            // Animals
            AnimalWeapon = new(nameof(AnimalWeapon), string.Empty, string.Empty, h => h.ProficiencyStats.AnimalWeapon, useStrength: true);

        public static readonly ProfStatType[] HeroProficiencies = {
            OneHanded,
            TwoHanded,
            Unarmed,
            Shield,
            Athletics,
            LightArmor,
            MediumArmor,
            HeavyArmor,
            Archery,
            Evasion,
            Acrobatics,
            Sneak,
            Theft,
            Magic,
            Alchemy,
            Cooking,
            Handcrafting,
        };
    }  
}