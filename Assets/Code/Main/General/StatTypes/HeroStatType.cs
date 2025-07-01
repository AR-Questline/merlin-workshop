using System;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Utility.RichEnums;

namespace Awaken.TG.Main.General.StatTypes {
    [RichEnumDisplayCategory("Hero")]
    public class HeroStatType : StatType<Hero> {

        const string CategoryMobility = "Mobility";
        const string CategoryPerception = "Perception";
        const string CategoryDamage = "Damage";
        const string CategoryWyrding = "Wyrding";
        const string CategoryGathering = "Gathering";
        const string CategoryCombat = "Combat";
        const string CategoryCrafting = "Crafting";
        const string CategoryOther = "Other";
        
        protected HeroStatType(string id, string displayName, Func<Hero, Stat> getter, string inspectorCategory = "", Param param = null) : base(id,
            displayName, getter, inspectorCategory, param) { }

        public static readonly HeroStatType
            XP = new(nameof(XP), LocTerms.Experience, h => h.HeroStats.XP, "", new Param { Abbreviation = "xp", Tweakable = false }),
            XPForNextLevel = new(nameof(XPForNextLevel), LocTerms.Experience, h => h.HeroStats.XPForNextLevel, "", new Param { Abbreviation = "xp", Tweakable = false }),

            MoveSpeed = new(nameof(MoveSpeed), LocTerms.MoveSpeed, h => h.HeroStats.MoveSpeed, CategoryMobility),
            SprintSpeed = new(nameof(SprintSpeed), LocTerms.SprintSpeed, h => h.HeroStats.SprintSpeed, CategoryMobility),
            CrouchSpeedMultiplier = new(nameof(CrouchSpeedMultiplier), LocTerms.CrouchSpeedMultiplier, h => h.HeroStats.CrouchSpeedMultiplier, CategoryMobility),
            SwimSpeed = new(nameof(SwimSpeed), LocTerms.SwimSpeed, h => h.HeroStats.SwimSpeed, CategoryMobility),
            BlockingMovementMultiplier = new(nameof(BlockingMovementMultiplier), LocTerms.BlockingMovementMultiplier, h => h.HeroStats.BlockingMovementMultiplier, CategoryMobility),
            JumpHeight = new(nameof(JumpHeight), LocTerms.JumpHeight, h => h.HeroStats.JumpHeight, CategoryMobility),
            DashSpeed = new(nameof(DashSpeed), LocTerms.DashSpeed, h => h.HeroStats.DashSpeed, CategoryMobility),
            DashStamina = new(nameof(DashStamina), LocTerms.DashStamina, h => h.HeroStats.DashStamina, CategoryMobility),
            DashCostMultiplier = new(nameof(DashCostMultiplier), LocTerms.DashCostMultiplier, h => h.HeroStats.DashCostMultiplier, CategoryMobility),
            MaxDashOptimalCounter = new(nameof(MaxDashOptimalCounter), LocTerms.MaxDashOptimalCounter, h => h.HeroStats.MaxDashOptimalCounter, CategoryMobility),
            DashRegenDurationMultiplier = new(nameof(DashRegenDurationMultiplier), LocTerms.DashRegenDurationMultiplier, h => h.HeroStats.DashRegenDurationMultiplier, CategoryMobility),
            EncumbranceLimit = new(nameof(EncumbranceLimit), LocTerms.EncumbranceLimit, h => h.HeroStats.EncumbranceLimit, CategoryMobility),
            ArmorWeightMultiplier = new(nameof(ArmorWeightMultiplier), LocTerms.ArmorWeightMultiplier, h => h.HeroStats.ArmorWeightMultiplier, CategoryMobility),
            FootstepsNoisiness = new(nameof(FootstepsNoisiness), LocTerms.FootstepsNoisiness, h => h.HeroStats.FootstepsNoisiness, CategoryMobility),
            OxygenLevel = new(nameof(OxygenLevel), LocTerms.OxygenLevel, h => h.HeroStats.OxygenLevel, CategoryMobility),
            OxygenUsage = new(nameof(OxygenUsage), LocTerms.OxygenUsage, h => h.HeroStats.OxygenUsage, CategoryMobility),
            DamageNullifier = new(nameof(DamageNullifier), LocTerms.DamageNullifier, h => h.HeroStats.DamageNullifier, CategoryMobility),
            FallDamageMultiplier = new(nameof(FallDamageMultiplier), LocTerms.FallDamageMultiplier, h => h.HeroStats.FallDamageMultiplier, CategoryMobility),

            VisibilityMultiplier = new(nameof(VisibilityMultiplier), LocTerms.VisibilityMultiplier, h => h.HeroStats.VisibilityMultiplier, CategoryPerception),
            NoiseMultiplier = new(nameof(NoiseMultiplier), LocTerms.NoiseMultiplier, h => h.HeroStats.NoiseMultiplier, CategoryPerception),
            CrouchNoiseMultiplier = new(nameof(CrouchNoiseMultiplier), LocTerms.CrouchNoiseMultiplier, h => h.HeroStats.CrouchNoiseMultiplier, CategoryPerception),
            CrouchVisibilityMultiplier = new(nameof(CrouchVisibilityMultiplier), LocTerms.CrouchVisibilityMultiplier, h => h.HeroStats.CrouchVisibilityMultiplier, CategoryPerception),
            LockpickDamageMultiplier = new(nameof(LockpickDamageMultiplier), LocTerms.LockpickDamageMultiplier, h => h.HeroStats.LockpickDamageMultiplier, CategoryPerception),
            LockpickToleranceMultiplier = new(nameof(LockpickToleranceMultiplier), LocTerms.LockpickToleranceMultiplier, h => h.HeroStats.LockpickToleranceMultiplier, CategoryPerception),
            LootChanceMultiplier = new(nameof(LootChanceMultiplier), LocTerms.LootChanceMultiplier, h => h.HeroStats.LootChanceMultiplier, CategoryPerception),

            CriticalChance = new(nameof(CriticalChance), LocTerms.CriticalChance, h => h.HeroStats.CriticalChance, CategoryDamage),
            CriticalDamageMultiplier = new(nameof(CriticalDamageMultiplier), LocTerms.CriticalDamageMultiplier, h => h.HeroStats.CriticalDamageMultiplier, CategoryDamage),
            SneakDamageMultiplier = new(nameof(SneakDamageMultiplier), LocTerms.SneakDamageMultiplier, h => h.HeroStats.SneakDamageMultiplier, CategoryDamage),
            BackStabDamageMultiplier = new(nameof(BackStabDamageMultiplier), LocTerms.BackStabDamageMultiplier, h => h.HeroStats.BackStabDamageMultiplier, CategoryDamage),
            MeleeSneakDamageMultiplier = new(nameof(MeleeSneakDamageMultiplier), LocTerms.MeleeSneakDamageMultiplier, h => h.HeroStats.MeleeSneakDamageMultiplier, CategoryDamage),
            WeakSpotDamageMultiplier = new(nameof(WeakSpotDamageMultiplier), LocTerms.WeakSpotDamageMultiplier, h => h.HeroStats.WeakSpotDamageMultiplier, CategoryDamage),
            MeleeWeakSpotDamageMultiplier = new(nameof(MeleeWeakSpotDamageMultiplier), LocTerms.WeakSpotDamageMultiplier, h => h.HeroStats.MeleeWeakSpotDamageMultiplier, CategoryDamage),
            MeleeCriticalChance = new(nameof(MeleeCriticalChance), LocTerms.MeleeCriticalChance, h => h.HeroStats.MeleeCriticalChance, CategoryDamage),
            RangedCriticalChance = new(nameof(RangedCriticalChance), LocTerms.RangedCriticalChance, h => h.HeroStats.RangedCriticalChance, CategoryDamage),
            MagicCriticalChance = new(nameof(MagicCriticalChance), LocTerms.MagicCriticalChance, h => h.HeroStats.MagicCriticalChance, CategoryDamage),
            ItemStaminaCostMultiplier = new(nameof(ItemStaminaCostMultiplier), LocTerms.ItemStaminaCostMultiplier, h => h.HeroStats.ItemStaminaCostMultiplier, CategoryDamage),
            EquipWeaponActionCooldown = new(nameof(EquipWeaponActionCooldown), LocTerms.EquipWeaponActionCooldown, h => h.HeroStats.EquipWeaponActionCooldown, CategoryDamage),
            StaminaDepletedTimeMultiplier = new(nameof(StaminaDepletedTimeMultiplier), LocTerms.StaminaDepletedTimeMultiplier, h => h.HeroStats.StaminaDepletedTimeMultiplier, CategoryDamage),
            SummonsManaDrainMultiplier = new(nameof(SummonsManaDrainMultiplier), LocTerms.SummonsManaDrainMultiplier, h => h.HeroStats.SummonsManaDrainMultiplier, CategoryDamage),
            DualWieldHeavyAttackCostMultiplier = new(nameof(DualWieldHeavyAttackCostMultiplier), LocTerms.DualWieldHeavyAttackCostMultiplier, h => h.HeroStats.DualWieldHeavyAttackCostMultiplier, CategoryDamage),
            MaxManaReservation = new(nameof(MaxManaReservation), LocTerms.MaxManaReservation, h => h.HeroStats.MaxManaReservation, CategoryDamage),
            SummonLimit = new(nameof(SummonLimit), LocTerms.SummonLimit, h => h.HeroStats.SummonLimit, CategoryDamage),
            
            MaxWyrdSkillDuration = new(nameof(MaxWyrdSkillDuration), LocTerms.WyrdSkillDuration, h => h.HeroStats.MaxWyrdSkillDuration, CategoryWyrding),
            WyrdSkillDuration = new(nameof(WyrdSkillDuration), LocTerms.WyrdSkillDuration, h => h.HeroStats.WyrdSkillDuration, CategoryWyrding),
            WyrdWhispers = new(nameof(WyrdWhispers), LocTerms.WyrdWhispers, h => h.HeroStats.WyrdWhispers, CategoryWyrding),
            WyrdMemoryShards = new(nameof(WyrdMemoryShards), LocTerms.WyrdMemoryShards, h => h.HeroStats.WyrdMemoryShards, CategoryWyrding),
            
            ParryStaminaDamageMultiplier = new(nameof(ParryStaminaDamageMultiplier), LocTerms.ParryStaminaDamageMultiplier, c => c.HeroStats.ParryStaminaDamageMultiplier, CategoryCombat),
            ParryWindowBonus = new(nameof(ParryWindowBonus), LocTerms.ParryWindowBonus, c => c.HeroStats.ParryWindowBonus, CategoryCombat),
            BlockingStaminaDamageMultiplier = new(nameof(BlockingStaminaDamageMultiplier), LocTerms.BlockingStaminaDamageMultiplier, c => c.HeroStats.BlockingStaminaDamageMultiplier, CategoryCombat),
            ArrowRetrievalChance = new(nameof(ArrowRetrievalChance), LocTerms.ArrowRetrievalChance, c => c.HeroStats.ArrowRetrievalChance, CategoryCombat),
            BowSwayMultiplier = new(nameof(BowSwayMultiplier), LocTerms.BowSwayMultiplier, c => c.HeroStats.BowSwayMultiplier, CategoryCombat),
            AimSensitivityMultiplier = new(nameof(AimSensitivityMultiplier), string.Empty, c => c.HeroStats.AimSensitivityMultiplier, CategoryCombat),
            
            MinimumHeavyDamageAdd = new(nameof(MinimumHeavyDamageAdd), LocTerms.MinimumHeavyDamageAdd, c => c.HeroStats.MinimumHeavyDamageAdd, CategoryCombat),
            MaximumHeavyDamageAdd = new(nameof(MaximumHeavyDamageAdd), LocTerms.MaximumHeavyDamageAdd, c => c.HeroStats.MaximumHeavyDamageAdd, CategoryCombat),
            
            AdditionalScrapChance = new(nameof(AdditionalScrapChance), LocTerms.AdditionalScrapChance, h => h.HeroStats.AdditionalScrapChance, CategoryGathering),
            TheftHoldTimeModifier = new(nameof(TheftHoldTimeModifier), LocTerms.TheftHoldTimeModifier, h => h.HeroStats.TheftHoldTimeModifier, CategoryGathering),
            PickpocketHoldTimeModifier = new(nameof(PickpocketHoldTimeModifier), LocTerms.PickpocketHoldTimeModifier, h => h.HeroStats.PickpocketHoldTimeModifier, CategoryGathering),
            PickpocketRecoveryChance = new(nameof(PickpocketRecoveryChance), LocTerms.PickpocketRecoveryChance, h => h.HeroStats.PickpocketRecoveryChance, CategoryGathering),
            
            EquipmentLevelBonus = new(nameof(EquipmentLevelBonus), LocTerms.EquipmentLevelBonus, c => c.HeroStats.EquipmentLevelBonus, CategoryCrafting),
            CookingLevelBonus = new(nameof(CookingLevelBonus), LocTerms.CookingLevelBonus, c => c.HeroStats.CookingLevelBonus, CategoryCrafting),
            AlchemyLevelBonus = new(nameof(AlchemyLevelBonus), LocTerms.AlchemyLevelBonus, c => c.HeroStats.AlchemyLevelBonus, CategoryCrafting),
            UpgradeDiscount = new(nameof(UpgradeDiscount), LocTerms.UpgradeDiscount, c => c.HeroStats.UpgradeDiscount, CategoryCrafting),
            CraftingRequirementModifier = new(nameof(CraftingRequirementModifier), LocTerms.CraftingRequirementModifier, c => c.HeroStats.CraftingRequirementModifier, CategoryCrafting),

            PrisonPenaltyMultiplier = new(nameof(PrisonPenaltyMultiplier), LocTerms.PrisonPenaltyMultiplier, c => c.HeroStats.PrisonPenaltyMultiplier, CategoryOther),
            ArmorPenaltyMultiplier = new(nameof(ArmorPenaltyMultiplier), LocTerms.ArmorPenaltyMultiplier, c => c.HeroStats.ArmorPenaltyMultiplier, CategoryOther),
            FenceSellBonusMultiplier = new(nameof(FenceSellBonusMultiplier), LocTerms.FenceSellBonusMultiplier, c => c.HeroStats.FenceSellBonusMultiplier, CategoryOther),
            CraftingSkillBonus = new(nameof(CraftingSkillBonus), LocTerms.CraftingSkillBonus, c => c.HeroStats.CraftingSkillBonus, CategoryCrafting),
            FishingMeanMultiplier = new(nameof(FishingMeanMultiplier), string.Empty, c => c.HeroStats.FishingMeanMultiplier, CategoryOther);
    }
}