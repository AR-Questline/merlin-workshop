using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Utility.RichEnums;

namespace Awaken.TG.Main.General.StatTypes {
    // === Stats

    [RichEnumDisplayCategory("Character")]
    public class CharacterStatType : StatType<ICharacter> {
        [UnityEngine.Scripting.Preserve]
        public static readonly CharacterStatType
            Level = new(nameof(Level), LocTerms.Level, c => c.CharacterStats.Level, "General", new Param {Tweakable = false}),
            TalentPoints = new(nameof(TalentPoints), LocTerms.TalentPoints, c => c.CharacterStats.TalentPoints, "General", new Param {Tweakable = false}),
            BaseStatPoints = new(nameof(BaseStatPoints), LocTerms.BaseStatPoints, c => c.CharacterStats.BaseStatPoints, "General", new Param {Tweakable = false}),
            
            Stamina = new(nameof(Stamina), LocTerms.Stamina, c => c.CharacterStats.Stamina, "General", new Param { Abbreviation = "s", Tweakable = false }),
            MaxStamina = new(nameof(MaxStamina), LocTerms.MaxStamina, c => c.CharacterStats.MaxStamina, "General", new Param { Abbreviation = "s" }),
            StaminaRegen = new(nameof(StaminaRegen), LocTerms.StaminaRegen, c => c.CharacterStats.StaminaRegen, "General", new Param { Abbreviation = "s" }),
            StaminaUsageMultiplier = new(nameof(StaminaUsageMultiplier), LocTerms.StaminaUsageMultiplier, c => c.CharacterStats.StaminaUsageMultiplier, "General"),
            SprintCostMultiplier = new(nameof(SprintCostMultiplier), LocTerms.SprintCostMultiplier, c => c.CharacterStats.SprintCostMultiplier, "General"),

            Mana = new(nameof(Mana), LocTerms.Mana, c => c.CharacterStats.Mana, "General", new Param {Abbreviation = "m", Tweakable = false}),
            MaxMana = new(nameof(MaxMana), LocTerms.MaxMana, c => c.CharacterStats.MaxMana, "General", new Param {Abbreviation = "m"}),
            ManaUsageMultiplier = new(nameof(ManaUsageMultiplier), LocTerms.ManaUsageMultiplier, c => c.CharacterStats.ManaUsageMultiplier, "General"),
            ManaRegen = new(nameof(ManaRegen), LocTerms.ManaRegen, c => c.CharacterStats.ManaRegen, "General"),
            ManaRegenPercentage = new(nameof(ManaRegenPercentage), LocTerms.ManaRegenPercentage, c => c.CharacterStats.ManaRegenPercentage, "General"),
            ManaShield = new(nameof(ManaShield), LocTerms.ManaShield, c => c.CharacterStats.ManaShield, "General"),
            ManaShieldRetaliation = new(nameof(ManaShieldRetaliation), LocTerms.ManaShieldRetaliation, c => c.CharacterStats.ManaShieldRetaliation, "General"),
            
            MeleeRetaliation = new(nameof(MeleeRetaliation), LocTerms.MeleeRetaliation, c => c.CharacterStats.MeleeRetaliation, "General"),
            
            MovementSpeedMultiplier = new(nameof(MovementSpeedMultiplier), LocTerms.MovementSpeedMultiplier, c => c.CharacterStats.MovementSpeedMultiplier, "General"),

            // Combat
            Strength = new(nameof(Strength), LocTerms.Strength, c => c.CharacterStats.Strength, "Fight", new Param {Abbreviation = "str"}),
            StrengthLinear = new(nameof(StrengthLinear), LocTerms.CombatDamage, c => c.CharacterStats.StrengthLinear, "Fight", new Param {Abbreviation = "Dmg"}), 
            IncomingDamage = new(nameof(IncomingDamage), LocTerms.IncomingDamage, c => c.CharacterStats.IncomingDamage, "Fight"),

            IncomingHealing = new(nameof(IncomingHealing), LocTerms.IncomingHealing, c => c.CharacterStats.IncomingHealing, "Fight"),
            ConsumableHealingBonus = new(nameof(ConsumableHealingBonus), LocTerms.ConsumableHealingBonus, c => c.CharacterStats.ConsumableHealingBonus, "Fight", new Param {Abbreviation = "Heal"}),
            PotionHealingBonus = new(nameof(PotionHealingBonus), LocTerms.PotionHealingBonus, c => c.CharacterStats.PotionHealingBonus, "Fight", new Param {Abbreviation = "Heal"}),

            Evasion = new(nameof(Evasion), LocTerms.Evasion, c => c.CharacterStats.Evasion, "Fight",new Param {Abbreviation = "ev"}),
            Resistance = new(nameof(Resistance), LocTerms.Resistance, c => c.CharacterStats.Resistance, "Fight",new Param {Abbreviation = "res"}),
            LifeSteal = new(nameof(LifeSteal), LocTerms.Lifesteal, c => c.CharacterStats.LifeSteal, "Fight",new Param {Abbreviation = "lifesteal"}),
            
            // Statuses
            BuffStrength = new(nameof(BuffStrength), LocTerms.BuffStrength, c => c.CharacterStats.BuffStrength, "Fight"),
            BuffDuration = new(nameof(BuffDuration), LocTerms.BuffDuration, c => c.CharacterStats.BuffDuration, "Fight"),
            DebuffStrength = new(nameof(DebuffStrength), LocTerms.DebuffStrength, c => c.CharacterStats.DebuffStrength, "Fight"),
            DebuffDuration = new(nameof(DebuffDuration), LocTerms.DebuffDuration, c => c.CharacterStats.DebuffDuration, "Fight"),
            
            // Speeds
            AttackSpeed = new(nameof(AttackSpeed), LocTerms.AttackSpeed, c => c.CharacterStats.AttackSpeed, "Fight"),
            BowDrawSpeed = new(nameof(BowDrawSpeed), LocTerms.BowDrawSpeed, c => c.CharacterStats.BowDrawSpeed, "Fight"),
            OneHandedLightAttackSpeed = new(nameof(OneHandedLightAttackSpeed), LocTerms.LightAttackSpeed1H, c => c.CharacterStats.OneHandedLightAttackSpeed, "Fight"),
            OneHandedHeavyAttackSpeed = new(nameof(OneHandedHeavyAttackSpeed), LocTerms.HeavyAttackSpeed1H, c => c.CharacterStats.OneHandedHeavyAttackSpeed, "Fight"),
            TwoHandedLightAttackSpeed = new(nameof(TwoHandedLightAttackSpeed), LocTerms.LightAttackSpeed2H, c => c.CharacterStats.TwoHandedLightAttackSpeed, "Fight"),
            TwoHandedHeavyAttackSpeed = new(nameof(TwoHandedHeavyAttackSpeed), LocTerms.HeavyAttackSpeed2H, c => c.CharacterStats.TwoHandedHeavyAttackSpeed, "Fight"),
            DualHandedLightAttackSpeed = new(nameof(DualHandedLightAttackSpeed), LocTerms.LightAttackSpeedDual, c => c.CharacterStats.DualHandedLightAttackSpeed, "Fight"),
            DualHandedHeavyAttackSpeed = new(nameof(DualHandedHeavyAttackSpeed), LocTerms.HeavyAttackSpeedDual, c => c.CharacterStats.DualHandedHeavyAttackSpeed, "Fight"),
            BlockPrepareSpeed = new(nameof(BlockPrepareSpeed), LocTerms.BlockPrepareSpeed, c => c.CharacterStats.BlockPrepareSpeed, "Fight"),
            FistLightAttackSpeed = new(nameof(FistLightAttackSpeed), LocTerms.FistLightAttackSpeed, c => c.CharacterStats.FistLightAttackSpeed, "Fight"),
            FistHeavyAttackSpeed = new(nameof(FistHeavyAttackSpeed), LocTerms.FistHeavyAttackSpeed, c => c.CharacterStats.FistHeavyAttackSpeed, "Fight"),
            SpellChargeSpeed = new(nameof(SpellChargeSpeed), LocTerms.SpellChargeSpeed, c => c.CharacterStats.SpellChargeSpeed, "Fight"),
            DeflectPrecision = new(nameof(DeflectPrecision), LocTerms.DeflectPrecision, c => c.CharacterStats.DeflectPrecision, "Fight"),
            
            MeleeDamageMultiplier = new(nameof(MeleeDamageMultiplier), LocTerms.MeleeDamageMultiplier, c => c.CharacterStats.MeleeDamageMultiplier, "Fight",new Param {Abbreviation = "melee"}),
            OneHandedMeleeDamageMultiplier = new(nameof(OneHandedMeleeDamageMultiplier), LocTerms.OneHandedMeleeDamageMultiplier, c => c.CharacterStats.OneHandedMeleeDamageMultiplier, "Fight",new Param {Abbreviation = "melee"}),
            TwoHandedMeleeDamageMultiplier = new(nameof(TwoHandedMeleeDamageMultiplier), LocTerms.TwoHandedMeleeDamageMultiplier, c => c.CharacterStats.TwoHandedMeleeDamageMultiplier, "Fight",new Param {Abbreviation = "melee"}),
            UnarmedMeleeDamageMultiplier = new(nameof(UnarmedMeleeDamageMultiplier), LocTerms.UnarmedMeleeDamageMultiplier, c => c.CharacterStats.UnarmedMeleeDamageMultiplier, "Fight",new Param {Abbreviation = "melee"}),
            RangedDamageMultiplier = new(nameof(RangedDamageMultiplier), LocTerms.RangedDamageMultiplier, c => c.CharacterStats.RangedDamageMultiplier, "Fight",new Param {Abbreviation = "ranged"}),
            
            MagicStrength = new(nameof(MagicStrength), LocTerms.MagicStrength, c => c.CharacterStats.MagicStrength, "Damage"),
            HoldBlockCostReduction = new(nameof(HoldBlockCostReduction), LocTerms.HoldBlockCostReduction, c => c.CharacterStats?.HoldBlockCostReduction, "Block"),
            CompoundDamageCalculation = new(nameof(CompoundDamageCalculation), LocTerms.CombatDamage, c => null, "Ignore");

        protected CharacterStatType(string id, string displayName, Func<ICharacter, Stat> getter,
                                    string inspectorCategory = "", Param param = null)
            : base(id, displayName, getter, inspectorCategory, param) { }

        public override string ToString() => DisplayName;
    }
}
