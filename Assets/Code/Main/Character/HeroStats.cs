using Awaken.Utility;
using System;
using Awaken.TG.Main.AI.States;
using Awaken.TG.Main.Animations.FSM.Heroes.States.Shared;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Development;
using Awaken.TG.Main.Heroes.Setup;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Character {
    public partial class HeroStats : Element<Hero> {
        public override ushort TypeForSerialization => SavedModels.HeroStats;

        const float MaxStaminaDamageMultiplier = 10;
        const float MaxHeavyAttackDamageAdd = 10;
        const float MaxLootChanceMultiplier = 100;
        const float MinCrouchSpeedMultiplier = 0.5f;
        const float MaxCrouchSpeedMultiplier = 10;

        [Saved] HeroStatsWrapper _wrapper;
        
        public Stat XPForNextLevel { get; private set; }
        public LimitedStat XP { get; private set; }
        
        //Mobility
        public Stat MoveSpeed { get; private set; }
        public Stat SprintSpeed { get; private set; }
        public LimitedStat CrouchSpeedMultiplier { get; private set; }
        public Stat SwimSpeed { get; private set; }
        public Stat BlockingMovementMultiplier { get; private set; }
        public Stat JumpHeight { get; private set; }
        public Stat DashStamina { get; private set; }
        public Stat DashSpeed { get; private set; }
        public Stat DashCostMultiplier { get; private set; }
        public Stat MaxDashOptimalCounter { get; private set; }
        public LimitedStat DashRegenDurationMultiplier { get; private set; }
        public Stat EncumbranceLimit { get; private set; }
        public Stat ArmorWeightMultiplier { get; private set; }
        public LimitedStat FootstepsNoisiness { get; private set; }
        public LimitedStat OxygenLevel { get; private set; }
        public Stat OxygenUsage { get; private set; }
        public LimitedStat DamageNullifier { get; private set; }
        public LimitedStat FallDamageMultiplier { get; private set; }
        // Perception
        public LimitedStat VisibilityMultiplier { get; private set; }
        public LimitedStat NoiseMultiplier { get; private set; }
        public LimitedStat CrouchNoiseMultiplier { get; private set; }
        public LimitedStat CrouchVisibilityMultiplier { get; private set; }
        public LimitedStat LockpickDamageMultiplier { get; private set; }
        public LimitedStat LockpickToleranceMultiplier { get; private set; }
        public LimitedStat LootChanceMultiplier { get; private set; }
        // Damage
        public LimitedStat CriticalChance { get; private set; }
        public LimitedStat CriticalDamageMultiplier { get; private set; }
        public LimitedStat SneakDamageMultiplier { get; private set; }
        public LimitedStat BackStabDamageMultiplier { get; private set; }
        public LimitedStat MeleeSneakDamageMultiplier { get; private set; }
        public LimitedStat WeakSpotDamageMultiplier { get; private set; }
        public LimitedStat MeleeWeakSpotDamageMultiplier { get; private set; }
        public LimitedStat MeleeCriticalChance { get; private set; }
        public LimitedStat RangedCriticalChance { get; private set; }
        public LimitedStat MagicCriticalChance { get; private set; }
        // Wyrding
        public Stat MaxWyrdSkillDuration { get; private set; }
        public LimitedStat WyrdSkillDuration { get; private set; }
        public Stat WyrdWhispers { get; private set; }
        public Stat WyrdMemoryShards { get; private set; }
        // Combat
        public LimitedStat ParryStaminaDamageMultiplier { get; private set; }
        public LimitedStat ParryWindowBonus { get; private set; }
        public LimitedStat BlockingStaminaDamageMultiplier { get; private set; }
        public LimitedStat ArrowRetrievalChance { get; private set; }
        public LimitedStat BowSwayMultiplier { get; private set; }
        public LimitedStat MinimumHeavyDamageAdd { get; private set; }
        public LimitedStat MaximumHeavyDamageAdd { get; private set; }
        public LimitedStat AimSensitivityMultiplier { get; private set; }
        public Stat ItemStaminaCostMultiplier { get; private set; }
        public LimitedStat EquipWeaponActionCooldown { get; private set; }
        public LimitedStat StaminaDepletedTimeMultiplier { get; private set; }
        public LimitedStat SummonsManaDrainMultiplier { get; private set; }
        public LimitedStat DualWieldHeavyAttackCostMultiplier { get; private set; }
        public LimitedStat MaxManaReservation { get; private set; }
        public LimitedStat SummonLimit { get; private set; }
        // Gathering
        public LimitedStat AdditionalScrapChance { get; private set; }
        public LimitedStat TheftHoldTimeModifier { get; private set; }
        public LimitedStat PickpocketHoldTimeModifier { get; private set; }
        public LimitedStat PickpocketRecoveryChance { get; private set; }
        
        // Crafting
        public Stat EquipmentLevelBonus { get; private set; }
        public Stat CookingLevelBonus { get; private set; }
        public Stat AlchemyLevelBonus { get; private set; }
        public Stat UpgradeDiscount { get; private set; }
        public Stat CraftingRequirementModifier { get; private set; }
        
        // Others
        public LimitedStat PrisonPenaltyMultiplier { get; private set; }
        public LimitedStat ArmorPenaltyMultiplier { get; private set; }
        public Stat FenceSellBonusMultiplier { get; private set; }
        public Stat CraftingSkillBonus { get; private set; }
        public Stat FishingMeanMultiplier { get; private set; }

        protected override void OnInitialize() {
            _wrapper.Initialize(this);
        }
        
        public static void CreateFromHeroTemplate(Hero hero) {
            HeroStats heroStats = new();
            hero.AddElement(heroStats);
        }

        // === Persistence
        
        void OnBeforeWorldSerialize() {
            _wrapper.PrepareForSave(this);
        }
        
        public partial struct HeroStatsWrapper {
            public ushort TypeForSerialization => SavedTypes.HeroStatsWrapper;

            const float DefaultMultiplier = 1f;
            const float DefaultBonusValue = 0f;
            const float DefaultCrouchNoiseMultiplier = 0.25f;
            const float MinPerceptionMultiplier = 0.1f;
            
            [Saved(0f)] float XPForNextLevelDif;
            [Saved(0f)] float XPDif;
            
            [Saved(0f)] float MoveSpeedDif;
            [Saved(0f)] float SprintSpeedDif;
            [Saved(0f)] float CrouchSpeedMultiplierDif;
            [Saved(0f)] float SwimSpeedDif;
            [Saved(0f)] float BlockingMovementMultiplierDif;
            [Saved(0f)] float JumpHeightDif;
            [Saved(0f)] float DashSpeedDif;
            [Saved(0f)] float DashStaminaDif;
            [Saved(0f)] float DashCostMultiplierDif;
            [Saved(0f)] float DashMaxOptimalCountersDif;
            [Saved(0f)] float DashRegenDurationMultiplierDif;
            [Saved(0f)] float EncumbranceLimitDif;
            [Saved(0f)] float ArmorWeightMultiplierDif;
            [Saved(0f)] float FootstepsNoisinessDif;
            [Saved(0f)] float OxygenLevelDif;
            [Saved(0f)] float OxygenUsageDif;
            [Saved(0f)] float DamageNullifierDif;
            [Saved(0f)] float FallDamageMultiplierDif;
            
            [Saved(0f)] float VisibilityMultiplierDif;
            [Saved(0f)] float NoiseMultiplierDif;
            [Saved(0f)] float CrouchNoiseMultiplierDif;
            [Saved(0f)] float CrouchVisibilityMultiplierDif;
            [Saved(0f)] float LockpickDamageMultiplierDif;
            [Saved(0f)] float LockpickToleranceMultiplierDif;
            [Saved(0f)] float LootChanceMultiplierDif;
            
            [Saved(0f)] float CriticalChanceDif;
            [Saved(0f)] float CriticalDamageMultiplierDif;
            [Saved(0f)] float SneakDamageMultiplierDif;
            [Saved(0f)] float BackStabDamageMultiplierDif;
            [Saved(0f)] float MeleeSneakDamageMultiplierDif;
            [Saved(0f)] float WeakSpotDamageMultiplierDif;
            [Saved(0f)] float MeleeWeakSpotDamageMultiplierDif;
            [Saved(0f)] float MeleeCriticalChanceDif;
            [Saved(0f)] float RangedCriticalChanceDif;
            [Saved(0f)] float MagicCriticalChanceDif;
            [Saved(0f)] float ItemStaminaCostMultiplierDif;
            [Saved(0f)] float EquipWeaponActionCooldownDif;
            [Saved(0f)] float StaminaDepletedTimeMultiplierDif;
            [Saved(0f)] float SummonsManaDrainMultiplierDif;
            [Saved(0f)] float DualWieldHeavyAttackCostMultiplierDif;
            [Saved(0f)] float MaxManaReservationDif;
            [Saved(0f)] float SummonLimitDif;
            
            [Saved(0f)] float WyrdSkillDurationDif;
            [Saved(0f)] float MaxWyrdSkillDurationDif;
            [Saved(0f)] float WyrdWhispersDif;
            [Saved(0f)] float WyrdMemoryShardsDif;
            
            [Saved(0f)] float ParryStaminaDamageMultiplierDif;
            [Saved(0f)] float ParryWindowBonusDif;
            [Saved(0f)] float BlockingStaminaDamageMultiplierDif;
            [Saved(0f)] float ArrowRetrievalChanceDif;
            [Saved(0f)] float BowSwayMultiplierDif;
            [Saved(0f)] float AimSensitivityMultiplierDif;
            
            [Saved(0f)] float MinimumHeavyDamageAddDif;
            [Saved(0f)] float MaximumHeavyDamageAddDif;
            
            [Saved(0f)] float AdditionalScrapChanceDif;
            [Saved(0f)] float TheftHoldTimeModifierDif;
            [Saved(0f)] float PickpocketHoldTimeModifierDif;
            [Saved(0f)] float PickpocketRecoveryChanceDif;
            
            [Saved(0f)] float EquipmentLevelBonusDif;
            [Saved(0f)] float CookingLevelBonusDif;
            [Saved(0f)] float AlchemyLevelBonusDif;
            [Saved(0f)] float UpgradeDiscountDif;
            [Saved(0f)] float CraftingRequirementModifierDif;
            
            [Saved(0f)] float PrisonPenaltyMultiplierDif;
            [Saved(0f)] float ArmorPenaltyMultiplierDif;
            [Saved(0f)] float FenceSellBonusMultiplierDif;
            [Saved(0f)] float CraftingSkillBonusDif;
            [Saved(0f)] float FishingMeanMultiplierDif;

            public void Initialize(HeroStats heroStats) {
                Hero hero = heroStats.ParentModel;
                HeroTemplate template = hero.Template;
                
                heroStats.XPForNextLevel = new Stat(hero, HeroStatType.XPForNextLevel, HeroDevelopment.RequiredExpFor(template.startLevel + 1) + XPForNextLevelDif);
                heroStats.XP = new LimitedStat(hero, HeroStatType.XP, 0 + XPDif, 0, HeroStatType.XPForNextLevel, true);

                HeroControllerData controllerData = template.heroControllerData;
                if (controllerData == null) {
                    throw new Exception("Incomplete data: HeroControllerData in HeroTemplate null");
                }
                // Mobility
                heroStats.MoveSpeed = new Stat(hero, HeroStatType.MoveSpeed, controllerData.moveSpeed + MoveSpeedDif);
                heroStats.SprintSpeed = new Stat(hero, HeroStatType.SprintSpeed, controllerData.sprintSpeed + SprintSpeedDif);
                heroStats.CrouchSpeedMultiplier = new LimitedStat(hero, HeroStatType.CrouchSpeedMultiplier, 1 + CrouchSpeedMultiplierDif, MinCrouchSpeedMultiplier, MaxCrouchSpeedMultiplier);
                heroStats.SwimSpeed = new Stat(hero, HeroStatType.SwimSpeed, controllerData.swimSpeed + SwimSpeedDif);
                heroStats.BlockingMovementMultiplier = new Stat(hero, HeroStatType.BlockingMovementMultiplier, controllerData.blockingMultiplier + BlockingMovementMultiplierDif);
                heroStats.JumpHeight = new Stat(hero, HeroStatType.JumpHeight, controllerData.jumpHeight + JumpHeightDif);
                heroStats.DashSpeed = new Stat(hero, HeroStatType.DashSpeed, controllerData.dashSpeed + DashSpeedDif);
                heroStats.DashStamina = new Stat(hero, HeroStatType.DashStamina, controllerData.dashStaminaCost + DashStaminaDif);
                heroStats.DashCostMultiplier = new Stat(hero, HeroStatType.DashCostMultiplier, controllerData.dashCostMultiplier + DashCostMultiplierDif);
                heroStats.MaxDashOptimalCounter = new Stat(hero, HeroStatType.MaxDashOptimalCounter, controllerData.dashMaxOptimalCounters + DashMaxOptimalCountersDif);
                heroStats.DashRegenDurationMultiplier = new LimitedStat(hero, HeroStatType.DashRegenDurationMultiplier, 1 + DashRegenDurationMultiplierDif, 0, float.MaxValue);
                heroStats.EncumbranceLimit = new Stat(hero, HeroStatType.EncumbranceLimit, template.encumbranceLimit + EncumbranceLimitDif);
                heroStats.ArmorWeightMultiplier = new Stat(hero, HeroStatType.ArmorWeightMultiplier, DefaultMultiplier + ArmorWeightMultiplierDif);
                heroStats.FootstepsNoisiness = new LimitedStat(hero, HeroStatType.FootstepsNoisiness, controllerData.footstepNoisiness + FootstepsNoisinessDif, 0, float.PositiveInfinity);
                heroStats.OxygenLevel = new LimitedStat(hero, HeroStatType.OxygenLevel, controllerData.oxygenLevel + OxygenLevelDif, 0, controllerData.oxygenLevel);
                heroStats.OxygenUsage = new Stat(hero, HeroStatType.OxygenUsage, controllerData.oxygenUsageBase + OxygenUsageDif);
                heroStats.DamageNullifier = new LimitedStat(hero, HeroStatType.DamageNullifier, controllerData.damageNullifier + DamageNullifierDif, 0, float.MaxValue);
                heroStats.FallDamageMultiplier = new LimitedStat(hero, HeroStatType.FallDamageMultiplier, controllerData.fallDamageMultiplier + FallDamageMultiplierDif, 0, float.MaxValue);
                // Perception
                heroStats.VisibilityMultiplier = new LimitedStat(hero, HeroStatType.VisibilityMultiplier, DefaultMultiplier + VisibilityMultiplierDif, Perception.MinimumHeroSight, 1);
                heroStats.NoiseMultiplier = new LimitedStat(hero, HeroStatType.NoiseMultiplier, DefaultMultiplier + NoiseMultiplierDif, MinPerceptionMultiplier, 1);
                heroStats.CrouchNoiseMultiplier = new LimitedStat(hero, HeroStatType.CrouchNoiseMultiplier, DefaultCrouchNoiseMultiplier + CrouchNoiseMultiplierDif, MinPerceptionMultiplier, 1);
                heroStats.CrouchVisibilityMultiplier = new LimitedStat(hero, HeroStatType.CrouchVisibilityMultiplier, DefaultMultiplier + CrouchVisibilityMultiplierDif, MinPerceptionMultiplier, 1);
                heroStats.LockpickDamageMultiplier = new LimitedStat(hero, HeroStatType.LockpickDamageMultiplier, DefaultMultiplier + LockpickDamageMultiplierDif, 0, 1);
                heroStats.LockpickToleranceMultiplier = new LimitedStat(hero, HeroStatType.LockpickToleranceMultiplier, DefaultMultiplier + LockpickToleranceMultiplierDif, 0, float.MaxValue);
                heroStats.LootChanceMultiplier = new LimitedStat(hero, HeroStatType.LootChanceMultiplier, DefaultMultiplier + LootChanceMultiplierDif, 1, MaxLootChanceMultiplier);
                // Damage
                heroStats.CriticalChance = new LimitedStat(hero, HeroStatType.CriticalChance, template.criticalChance + CriticalChanceDif, 0, 1);
                heroStats.CriticalDamageMultiplier = new LimitedStat(hero, HeroStatType.CriticalDamageMultiplier, template.criticalDamageMultiplier + CriticalDamageMultiplierDif, 0, float.MaxValue);
                heroStats.SneakDamageMultiplier = new LimitedStat(hero, HeroStatType.SneakDamageMultiplier, template.sneakDamageMultiplier + SneakDamageMultiplierDif, 0, float.MaxValue);
                heroStats.BackStabDamageMultiplier = new LimitedStat(hero, HeroStatType.BackStabDamageMultiplier, template.backStabDamageMultiplier + BackStabDamageMultiplierDif, 0, float.MaxValue);
                heroStats.MeleeSneakDamageMultiplier = new LimitedStat(hero, HeroStatType.MeleeSneakDamageMultiplier, template.meleeSneakDamageMultiplier + MeleeSneakDamageMultiplierDif, 0, float.MaxValue);
                heroStats.WeakSpotDamageMultiplier = new LimitedStat(hero, HeroStatType.WeakSpotDamageMultiplier, template.weakSpotDamageMultiplier + WeakSpotDamageMultiplierDif, 0, float.MaxValue);
                heroStats.MeleeWeakSpotDamageMultiplier = new LimitedStat(hero, HeroStatType.MeleeWeakSpotDamageMultiplier, template.meleeWeakSpotDamageMultiplier + MeleeWeakSpotDamageMultiplierDif, 0, float.MaxValue);
                heroStats.MeleeCriticalChance = new LimitedStat(hero, HeroStatType.MeleeCriticalChance, template.meleeCriticalChance + MeleeCriticalChanceDif, 0, 1);
                heroStats.RangedCriticalChance = new LimitedStat(hero, HeroStatType.RangedCriticalChance, template.rangedCriticalChance + RangedCriticalChanceDif, 0, 1);
                heroStats.MagicCriticalChance = new LimitedStat(hero, HeroStatType.MagicCriticalChance, template.magicCriticalChance + MagicCriticalChanceDif, 0, 1);
                heroStats.ItemStaminaCostMultiplier = new Stat(hero, HeroStatType.ItemStaminaCostMultiplier, template.itemStaminaCostMultiplier + ItemStaminaCostMultiplierDif);
                heroStats.EquipWeaponActionCooldown = new LimitedStat(hero, HeroStatType.EquipWeaponActionCooldown, EquipWeapon.DefaultActionCooldown + EquipWeaponActionCooldownDif, 0, 1);
                heroStats.StaminaDepletedTimeMultiplier = new LimitedStat(hero, HeroStatType.StaminaDepletedTimeMultiplier, DefaultMultiplier + StaminaDepletedTimeMultiplierDif, 0, float.MaxValue);
                heroStats.SummonsManaDrainMultiplier = new LimitedStat(hero, HeroStatType.SummonsManaDrainMultiplier, DefaultMultiplier + SummonsManaDrainMultiplierDif, 0, float.MaxValue);
                heroStats.DualWieldHeavyAttackCostMultiplier = new LimitedStat(hero, HeroStatType.DualWieldHeavyAttackCostMultiplier, template.dualWieldHeavyAttackCostMultiplier + DualWieldHeavyAttackCostMultiplierDif, 0, float.MaxValue);
                heroStats.MaxManaReservation = new LimitedStat(hero, HeroStatType.MaxManaReservation, DefaultBonusValue + MaxManaReservationDif, 0, float.MaxValue);
                heroStats.SummonLimit = new LimitedStat(hero, HeroStatType.SummonLimit, template.summonLimit + SummonLimitDif, 0, float.MaxValue);
                // Wyrding
                heroStats.MaxWyrdSkillDuration = new Stat(hero, HeroStatType.MaxWyrdSkillDuration, template.wyrdSkillDuration + MaxWyrdSkillDurationDif);
                heroStats.WyrdSkillDuration = new LimitedStat(hero, HeroStatType.WyrdSkillDuration, heroStats.MaxWyrdSkillDuration + WyrdSkillDurationDif, 0, HeroStatType.MaxWyrdSkillDuration);
                heroStats.WyrdWhispers = new Stat(hero, HeroStatType.WyrdWhispers, DefaultBonusValue + WyrdWhispersDif);
                heroStats.WyrdMemoryShards = new Stat(hero, HeroStatType.WyrdMemoryShards, DefaultBonusValue + WyrdMemoryShardsDif);
                // Combat
                heroStats.ParryStaminaDamageMultiplier = new LimitedStat(hero, HeroStatType.ParryStaminaDamageMultiplier, DefaultMultiplier + ParryStaminaDamageMultiplierDif, 0, MaxStaminaDamageMultiplier);
                heroStats.ParryWindowBonus = new LimitedStat(hero, HeroStatType.ParryWindowBonus, DefaultBonusValue + ParryWindowBonusDif, -1, 1);
                heroStats.BlockingStaminaDamageMultiplier = new LimitedStat(hero, HeroStatType.BlockingStaminaDamageMultiplier, DefaultMultiplier + BlockingStaminaDamageMultiplierDif, 0, MaxStaminaDamageMultiplier);
                heroStats.ArrowRetrievalChance = new LimitedStat(hero, HeroStatType.ArrowRetrievalChance, DefaultBonusValue + ArrowRetrievalChanceDif, 0, 1);
                heroStats.BowSwayMultiplier = new LimitedStat(hero, HeroStatType.BowSwayMultiplier, DefaultMultiplier + BowSwayMultiplierDif, 0, 2);
                heroStats.AimSensitivityMultiplier = new LimitedStat(hero, HeroStatType.AimSensitivityMultiplier, DefaultMultiplier + AimSensitivityMultiplierDif, 0.01f, 2);
                
                heroStats.MinimumHeavyDamageAdd = new LimitedStat(hero, HeroStatType.MinimumHeavyDamageAdd, DefaultBonusValue + MinimumHeavyDamageAddDif, 0, MaxHeavyAttackDamageAdd);
                heroStats.MaximumHeavyDamageAdd = new LimitedStat(hero, HeroStatType.MaximumHeavyDamageAdd, DefaultBonusValue + MaximumHeavyDamageAddDif, 0, MaxHeavyAttackDamageAdd);
                // Others
                heroStats.AdditionalScrapChance = new LimitedStat(hero, HeroStatType.AdditionalScrapChance, DefaultBonusValue + AdditionalScrapChanceDif, 0, 1);
                heroStats.TheftHoldTimeModifier = new LimitedStat(hero, HeroStatType.TheftHoldTimeModifier, DefaultMultiplier + TheftHoldTimeModifierDif, 0, 2);
                heroStats.PickpocketHoldTimeModifier = new LimitedStat(hero, HeroStatType.PickpocketHoldTimeModifier, DefaultMultiplier + PickpocketHoldTimeModifierDif, 0, 2);
                heroStats.PickpocketRecoveryChance = new LimitedStat(hero, HeroStatType.PickpocketRecoveryChance, DefaultBonusValue + PickpocketRecoveryChanceDif, 0, 1);
                
                // Crafting
                heroStats.EquipmentLevelBonus = new Stat(hero, HeroStatType.EquipmentLevelBonus, DefaultBonusValue + EquipmentLevelBonusDif);
                heroStats.CookingLevelBonus = new Stat(hero, HeroStatType.CookingLevelBonus, DefaultBonusValue + CookingLevelBonusDif);
                heroStats.AlchemyLevelBonus = new Stat(hero, HeroStatType.AlchemyLevelBonus, DefaultBonusValue + AlchemyLevelBonusDif);
                heroStats.UpgradeDiscount = new Stat(hero, HeroStatType.UpgradeDiscount, DefaultBonusValue + UpgradeDiscountDif);
                heroStats.CraftingRequirementModifier = new Stat(hero, HeroStatType.CraftingRequirementModifier, DefaultBonusValue + CraftingRequirementModifierDif);
                
                heroStats.PrisonPenaltyMultiplier = new LimitedStat(hero, HeroStatType.PrisonPenaltyMultiplier, DefaultMultiplier + PrisonPenaltyMultiplierDif, 0.01f, 2);
                heroStats.ArmorPenaltyMultiplier = new LimitedStat(hero, HeroStatType.ArmorPenaltyMultiplier, DefaultMultiplier + ArmorPenaltyMultiplierDif, 0.01f, 2);
                heroStats.FenceSellBonusMultiplier = new Stat(hero, HeroStatType.FenceSellBonusMultiplier, DefaultBonusValue + FenceSellBonusMultiplierDif);
                heroStats.CraftingSkillBonus = new Stat(hero, HeroStatType.CraftingSkillBonus, DefaultBonusValue + CraftingSkillBonusDif);
                heroStats.FishingMeanMultiplier = new Stat(hero, HeroStatType.FishingMeanMultiplier, DefaultMultiplier + FishingMeanMultiplierDif);
            }

            public void PrepareForSave(HeroStats heroStats) {
                HeroTemplate template = heroStats.ParentModel.Template;
                HeroControllerData controllerData = template.heroControllerData;
                
                XPForNextLevelDif = heroStats.XPForNextLevel.BaseValue - HeroDevelopment.RequiredExpFor(template.startLevel + 1);
                XPDif = heroStats.XP.BaseValue - 0;
                
                MoveSpeedDif = heroStats.MoveSpeed.BaseValue - controllerData.moveSpeed;
                SprintSpeedDif = heroStats.SprintSpeed.BaseValue - controllerData.sprintSpeed;
                CrouchSpeedMultiplierDif = heroStats.CrouchSpeedMultiplier.BaseValue - DefaultMultiplier;
                SwimSpeedDif = heroStats.SwimSpeed.BaseValue - controllerData.swimSpeed;
                BlockingMovementMultiplierDif = heroStats.BlockingMovementMultiplier.BaseValue - controllerData.blockingMultiplier;
                JumpHeightDif = heroStats.JumpHeight.BaseValue - controllerData.jumpHeight;
                DashSpeedDif = heroStats.DashSpeed.BaseValue - controllerData.dashSpeed;
                DashStaminaDif = heroStats.DashStamina.BaseValue - controllerData.dashStaminaCost;
                DashCostMultiplierDif = heroStats.DashCostMultiplier.BaseValue - controllerData.dashCostMultiplier;
                DashMaxOptimalCountersDif = heroStats.MaxDashOptimalCounter.BaseValue - controllerData.dashMaxOptimalCounters;
                DashRegenDurationMultiplierDif = heroStats.DashRegenDurationMultiplier.BaseValue - 1;
                EncumbranceLimitDif = heroStats.EncumbranceLimit.BaseValue - template.encumbranceLimit;
                ArmorWeightMultiplierDif = heroStats.ArmorWeightMultiplier.BaseValue - 1;
                FootstepsNoisinessDif = heroStats.FootstepsNoisiness.BaseValue - controllerData.footstepNoisiness;
                OxygenLevelDif = heroStats.OxygenLevel.BaseValue - controllerData.oxygenLevel;
                OxygenUsageDif = heroStats.OxygenUsage.BaseValue - controllerData.oxygenUsageBase;
                DamageNullifierDif = heroStats.DamageNullifier.BaseValue - controllerData.damageNullifier;
                FallDamageMultiplierDif = heroStats.FallDamageMultiplier.BaseValue - controllerData.fallDamageMultiplier;
                
                VisibilityMultiplierDif = heroStats.VisibilityMultiplier.BaseValue - DefaultMultiplier;
                NoiseMultiplierDif = heroStats.NoiseMultiplier.BaseValue - DefaultMultiplier;
                CrouchNoiseMultiplierDif = heroStats.CrouchNoiseMultiplier.BaseValue - DefaultCrouchNoiseMultiplier;
                CrouchVisibilityMultiplierDif = heroStats.CrouchVisibilityMultiplier.BaseValue - DefaultMultiplier;
                LockpickDamageMultiplierDif = heroStats.LockpickDamageMultiplier.BaseValue - DefaultMultiplier;
                LockpickToleranceMultiplierDif = heroStats.LockpickToleranceMultiplier.BaseValue - DefaultMultiplier;
                LootChanceMultiplierDif = heroStats.LootChanceMultiplier.BaseValue - DefaultMultiplier;
                
                CriticalChanceDif = heroStats.CriticalChance.BaseValue - template.criticalChance;
                CriticalDamageMultiplierDif = heroStats.CriticalDamageMultiplier.BaseValue - template.criticalDamageMultiplier;
                SneakDamageMultiplierDif = heroStats.SneakDamageMultiplier.BaseValue - template.sneakDamageMultiplier;
                BackStabDamageMultiplierDif = heroStats.BackStabDamageMultiplier.BaseValue - template.backStabDamageMultiplier;
                MeleeSneakDamageMultiplierDif = heroStats.MeleeSneakDamageMultiplier.BaseValue - template.meleeSneakDamageMultiplier;
                WeakSpotDamageMultiplierDif = heroStats.WeakSpotDamageMultiplier.BaseValue - template.weakSpotDamageMultiplier;
                MeleeWeakSpotDamageMultiplierDif = heroStats.MeleeWeakSpotDamageMultiplier.BaseValue - template.meleeWeakSpotDamageMultiplier;
                MeleeCriticalChanceDif = heroStats.MeleeCriticalChance.BaseValue - template.meleeCriticalChance;
                RangedCriticalChanceDif = heroStats.RangedCriticalChance.BaseValue - template.rangedCriticalChance;
                MagicCriticalChanceDif = heroStats.MagicCriticalChance.BaseValue - template.magicCriticalChance;
                ItemStaminaCostMultiplierDif = heroStats.ItemStaminaCostMultiplier.BaseValue - template.itemStaminaCostMultiplier;
                EquipWeaponActionCooldownDif = heroStats.EquipWeaponActionCooldown.BaseValue - EquipWeapon.DefaultActionCooldown;
                StaminaDepletedTimeMultiplierDif = heroStats.StaminaDepletedTimeMultiplier.BaseValue - DefaultMultiplier;
                SummonsManaDrainMultiplierDif = heroStats.SummonsManaDrainMultiplier.BaseValue - DefaultMultiplier;
                DualWieldHeavyAttackCostMultiplierDif = heroStats.DualWieldHeavyAttackCostMultiplier.BaseValue - template.dualWieldHeavyAttackCostMultiplier;
                MaxManaReservationDif = heroStats.MaxManaReservation.BaseValue - DefaultBonusValue;
                SummonLimitDif = heroStats.SummonLimit.BaseValue - template.summonLimit;
                
                MaxWyrdSkillDurationDif = heroStats.MaxWyrdSkillDuration.BaseValue - template.wyrdSkillDuration;
                WyrdSkillDurationDif = heroStats.WyrdSkillDuration.BaseValue - heroStats.MaxWyrdSkillDuration.BaseValue;
                WyrdWhispersDif = heroStats.WyrdWhispers.BaseValue - DefaultBonusValue;
                WyrdMemoryShardsDif = heroStats.WyrdMemoryShards.BaseValue - DefaultBonusValue;
                
                ParryStaminaDamageMultiplierDif = heroStats.ParryStaminaDamageMultiplier.BaseValue - DefaultMultiplier;
                ParryWindowBonusDif = heroStats.ParryWindowBonus.BaseValue - DefaultBonusValue;
                BlockingStaminaDamageMultiplierDif = heroStats.BlockingStaminaDamageMultiplier.BaseValue - DefaultMultiplier;
                ArrowRetrievalChanceDif = heroStats.ArrowRetrievalChance.BaseValue - DefaultBonusValue;
                BowSwayMultiplierDif = heroStats.BowSwayMultiplier.BaseValue - DefaultMultiplier;
                AimSensitivityMultiplierDif = heroStats.AimSensitivityMultiplier.BaseValue - DefaultMultiplier;
                
                MinimumHeavyDamageAddDif = heroStats.MinimumHeavyDamageAdd.BaseValue - DefaultBonusValue;
                MaximumHeavyDamageAddDif = heroStats.MaximumHeavyDamageAdd.BaseValue - DefaultBonusValue;
                
                AdditionalScrapChanceDif = heroStats.AdditionalScrapChance.BaseValue - DefaultBonusValue;
                TheftHoldTimeModifierDif = heroStats.TheftHoldTimeModifier.BaseValue - DefaultMultiplier;
                PickpocketHoldTimeModifierDif = heroStats.PickpocketHoldTimeModifier.BaseValue - DefaultMultiplier;
                PickpocketRecoveryChanceDif = heroStats.PickpocketRecoveryChance.BaseValue - DefaultBonusValue;
                
                EquipmentLevelBonusDif = heroStats.EquipmentLevelBonus.BaseValue - DefaultBonusValue;
                CookingLevelBonusDif = heroStats.CookingLevelBonus.BaseValue - DefaultBonusValue;
                AlchemyLevelBonusDif = heroStats.AlchemyLevelBonus.BaseValue - DefaultBonusValue;
                UpgradeDiscountDif = heroStats.UpgradeDiscount.BaseValue - DefaultBonusValue;
                CraftingRequirementModifierDif = heroStats.CraftingRequirementModifier.BaseValue - DefaultBonusValue;
                
                PrisonPenaltyMultiplierDif = heroStats.PrisonPenaltyMultiplier.BaseValue - DefaultMultiplier;
                ArmorPenaltyMultiplierDif = heroStats.ArmorPenaltyMultiplier.BaseValue - DefaultMultiplier;
                FenceSellBonusMultiplierDif = heroStats.FenceSellBonusMultiplier.BaseValue - DefaultBonusValue;
                CraftingSkillBonusDif = heroStats.CraftingSkillBonus.BaseValue - DefaultBonusValue;
                FishingMeanMultiplierDif = heroStats.FishingMeanMultiplier.BaseValue - DefaultMultiplier;
            }
        }
    }
}
