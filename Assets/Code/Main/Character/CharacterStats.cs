using Awaken.TG.Main.General.Configs;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Items;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Character {
    /// <summary>
    /// Stats used for fighting.
    /// </summary>
    public sealed partial class CharacterStats : Element<ICharacter> {
        public override ushort TypeForSerialization => SavedModels.CharacterStats;

        const float AttackSpeedMinValue = .5f;
        const float AttackSpeedMaxValue = 3f;
        
        const float BowDrawSpeedMinValue = .5f;
        const float BowDrawSpeedMaxValue = 3f;
        
        const float BlockPrepareSpeedMinValue = .5f;
        const float BlockPrepareSpeedMaxValue = 3f;
        
        const float SpellChargeSpeedMinValue = .5f;
        const float SpellChargeSpeedMaxValue = 3f;
        
        const float LightAttackSpeedMinValue = .5F;
        const float LightAttackSpeedMaxValue = 2.25F;
        const float HeavyAttackSpeedMinValue = .5F;
        const float HeavyAttackSpeedMaxValue = 2.25F;
        
        const float TwoHandedLightAttackSpeedMinValue = .5F;
        const float TwoHandedLightAttackSpeedMaxValue = 3F;
        const float TwoHandedHeavyAttackSpeedMinValue = .5F;
        const float TwoHandedHeavyAttackSpeedMaxValue = 3F;

        const float MovementSpeedMultiplierMinValue = 0.1f;
        const float MovementSpeedMultiplierMaxValue = 5f;

        const float MaxLimitStatMax = 999999f;
        
        [Saved] CharacterStatsWrapper _wrapper;

        public Stat Level { get; private set; }
        public Stat TalentPoints { get; private set; }
        public Stat BaseStatPoints { get; private set; }
        
        public LimitedStat Stamina { get; private set; }
        public Stat MaxStamina { get; private set; }
        public Stat StaminaRegen { get; private set; }
        public Stat StaminaUsageMultiplier { get; private set; }
        public LimitedStat SprintCostMultiplier { get; private set; }
        public LimitedStat Mana { get; private set; }
        public Stat MaxMana { get; private set; }
        public LimitedStat ManaUsageMultiplier { get; private set; }
        public Stat ManaRegen { get; private set; }
        public Stat ManaRegenPercentage { get; private set; }
        public LimitedStat ManaShield { get; private set; }
        public Stat ManaShieldRetaliation { get; private set; }
        
        public Stat MeleeRetaliation { get; private set; }
        
        public LimitedStat MovementSpeedMultiplier { get; private set; }
        
        public LimitedStat Strength { get; private set; }
        public Stat StrengthLinear { get; private set; }
        public Stat IncomingDamage { get; private set; }

        public Stat IncomingHealing { get; private set; }
        public Stat ConsumableHealingBonus { get; private set; }
        public Stat PotionHealingBonus { get; private set; }

        public LimitedStat Evasion { get; private set; }
        public Stat Resistance { get; private set; }
        public Stat LifeSteal { get; private set; }
        
        public LimitedStat BuffStrength { get; private set; }
        public LimitedStat BuffDuration { get; private set; }
        public LimitedStat DebuffStrength { get; private set; }
        public LimitedStat DebuffDuration { get; private set; }

        public LimitedStat MeleeDamageMultiplier { get; private set; }
        public LimitedStat OneHandedMeleeDamageMultiplier { get; private set; }
        public LimitedStat TwoHandedMeleeDamageMultiplier { get; private set; }
        public LimitedStat UnarmedMeleeDamageMultiplier { get; private set; }
        public LimitedStat RangedDamageMultiplier { get; private set; }
        
        // Weapon Multipliers
        public Stat MagicStrength { get; private set; }
        public LimitedStat HoldBlockCostReduction { get; private set; }

        //speeds
        public LimitedStat AttackSpeed { get; private set; }
        //bow
        public LimitedStat BowDrawSpeed { get; private set; }
        //oneHanded
        public LimitedStat OneHandedLightAttackSpeed { get; private set; }
        public LimitedStat OneHandedHeavyAttackSpeed { get; private set; }
        //twoHanded
        public LimitedStat TwoHandedLightAttackSpeed { get; private set; }
        public LimitedStat TwoHandedHeavyAttackSpeed { get; private set; }
        //dualHanded
        public LimitedStat DualHandedLightAttackSpeed { get; private set; }
        public LimitedStat DualHandedHeavyAttackSpeed { get; private set; }
        //fists
        public LimitedStat FistLightAttackSpeed { get; private set; }
        public LimitedStat FistHeavyAttackSpeed { get; private set; }
        //block
        public LimitedStat BlockPrepareSpeed { get; private set; }
        //spell
        public LimitedStat SpellChargeSpeed { get; private set; }
        public LimitedStat DeflectPrecision { get; private set; }
        
        // === Events
        public new static class Events {
            public static readonly Event<ICharacter, CharacterStats> FightStatsAdded = new(nameof(FightStatsAdded));
        }

        protected override void OnInitialize() {
            _wrapper.Initialize(this);
            ParentModel.ListenTo(Stat.Events.ChangingStat(CharacterStatType.Stamina), OnStaminaChangeHook, this);
            ParentModel.Trigger(Events.FightStatsAdded, this);
        }

        void OnStaminaChangeHook(HookResult<IWithStats, Stat.StatChange> obj) {
            Stat.StatChange change = obj.Value;
            if (change.value < 0) {
                change.value *= StaminaUsageMultiplier.ModifiedValue;
            }
            obj.Value = change;
        }

        public static void Create(ICharacter character) {
            CharacterStats stats = new();
            character.AddElement(stats);
        }

        public LimitedStat SelectAttackSpeed(Item item, bool isHeavyAttack) =>
            isHeavyAttack switch {
                _ when item.IsRanged => BowDrawSpeed,
                true when item.IsFists => FistHeavyAttackSpeed,
                true when item.IsOneHanded => OneHandedHeavyAttackSpeed,
                true when item.IsTwoHanded => TwoHandedHeavyAttackSpeed,
                false when item.IsFists => FistLightAttackSpeed,
                false when item.IsOneHanded => OneHandedLightAttackSpeed,
                false when item.IsTwoHanded => TwoHandedLightAttackSpeed,
                _ => BowDrawSpeed
            };

        // === Persistence

        void OnBeforeWorldSerialize() {
            _wrapper.PrepareForSave(this);
        }
        
        public interface ITemplate {
            int Level { get; }
            int TalentPoints { get; }
            int BaseStatPoints { get; }
            
            int MaxStamina { get; }
            float StaminaRegen { get; }
            float StaminaUsageMultiplier { get; }

            int MaxMana { get; }
            float ManaUsageMultiplier { get; }
            float ManaRegen { get; }
            float ManaRegenPercentage { get; }
            
            float Strength { get; }
            float StrengthLinear { get; }

            float Evasion { get; }
            float Resistance { get; }
        }
        
        public partial struct CharacterStatsWrapper {
            public ushort TypeForSerialization => SavedTypes.CharacterStatsWrapper;

            const float DefaultMultiplier = 1f;
            const float DefaultHeavyAttackSpeed = 0.9f;
            const float DefaultFistLightAttackSpeed = 1.2f;
            const float DefaultFistHeavyAttackSpeed = 1.1f;

            [Saved(0f)] float LevelDif;
            [Saved(0f)] float TalentPointsDif;
            [Saved(0f)] float BaseStatPointsDif;
            
            [Saved(0f)] float StaminaDif;
            [Saved(0f)] float MaxStaminaDif;
            [Saved(0f)] float StaminaRegenDif;
            [Saved(0f)] float StaminaUsageMultiplierDif;
            [Saved(0f)] float SprintCostMultiplierDif;

            [Saved(0f)] float ManaDif;
            [Saved(0f)] float MaxManaDif;
            [Saved(0f)] float ManaUsageMultiplierDif;
            [Saved(0f)] float ManaRegenDif;
            [Saved(0f)] float ManaRegenPercentageDif;
            [Saved(0f)] float ManaShieldDif;
            [Saved(0f)] float ManaShieldRetaliationDif;
            
            [Saved(0f)] float MeleeRetaliationDif;
            
            [Saved(0f)] float MovementSpeedMultiplierDif;
            
            [Saved(0f)] float StrengthDif;
            [Saved(0f)] float StrengthLinearDif;
            [Saved(0f)] float IncomingDamageDif;

            [Saved(0f)] float IncomingHealingDif;
            [Saved(0f)] float ConsumableHealingBonusDif;
            [Saved(0f)] float PotionHealingBonusDif;

            [Saved(0f)] float EvasionDif;
            [Saved(0f)] float ResistanceDif;
            [Saved(0f)] float LifeStealDif;

            [Saved(0f)] float BuffStrengthDif;
            [Saved(0f)] float BuffDurationDif;
            [Saved(0f)] float DebuffStrengthDif;
            [Saved(0f)] float DebuffDurationDif;

            [Saved(0f)] float MeleeDamageMultiplierDif;
            [Saved(0f)] float OneHandedMeleeDamageMultiplierDif;
            [Saved(0f)] float TwoHandedMeleeDamageMultiplierDif;
            [Saved(0f)] float UnarmedMeleeDamageMultiplierDif;
            [Saved(0f)] float RangedDamageMultiplierDif;
            
            // Weapon Multipliers
            [Saved(0f)] float MagicStrengthDif;
            [Saved(0f)] float HoldBlockCostReductionDif;

            //speeds
            [Saved(0f)] float AttackSpeedDif;
            //bow
            [Saved(0f)] float BowDrawSpeedDif;
            //oneHanded
            [Saved(0f)] float OneHandedLightAttackSpeedDif;
            [Saved(0f)] float OneHandedHeavyAttackSpeedDif;
            //twoHanded
            [Saved(0f)] float TwoHandedLightAttackSpeedDif;
            [Saved(0f)] float TwoHandedHeavyAttackSpeedDif;
            //dualHanded
            [Saved(0f)] float DualHandedLightAttackSpeedDif;
            [Saved(0f)] float DualHandedHeavyAttackSpeedDif;
            //fists
            [Saved(0f)] float FistLightAttackSpeedDif;
            [Saved(0f)] float FistHeavyAttackSpeedDif;
            //block
            [Saved(0f)] float BlockPrepareSpeedDif;
            //spell
            [Saved(0f)] float SpellChargeSpeedDif;
            //deflect
            [Saved(0f)] float DeflectPrecisionDif;

            public void Initialize(CharacterStats stats) {
                ICharacter character = stats.ParentModel;
                ITemplate template = character.CharacterStatsTemplate;
                
                stats.Level = new Stat(character, CharacterStatType.Level, template.Level + LevelDif);
                stats.TalentPoints = new Stat(character, CharacterStatType.TalentPoints, template.TalentPoints + TalentPointsDif);
                stats.BaseStatPoints = new Stat(character, CharacterStatType.BaseStatPoints, template.BaseStatPoints + BaseStatPointsDif);

                stats.MaxStamina = new Stat(character, CharacterStatType.MaxStamina, template.MaxStamina + MaxStaminaDif);
                stats.Stamina = new LimitedStat(character, CharacterStatType.Stamina, stats.MaxStamina + StaminaDif, 0, CharacterStatType.MaxStamina);
                stats.StaminaRegen = new Stat(character, CharacterStatType.StaminaRegen, template.StaminaRegen + StaminaRegenDif);
                stats.StaminaUsageMultiplier = new Stat(character, CharacterStatType.StaminaUsageMultiplier, template.StaminaUsageMultiplier + StaminaUsageMultiplierDif);
                stats.SprintCostMultiplier = new LimitedStat(character, CharacterStatType.SprintCostMultiplier, DefaultMultiplier + SprintCostMultiplierDif, 0, 5);

                stats.MaxMana = new Stat(character, CharacterStatType.MaxMana, template.MaxMana + MaxManaDif);
                stats.Mana = new LimitedStat(character, CharacterStatType.Mana, stats.MaxMana + ManaDif, 0, CharacterStatType.MaxMana);
                stats.ManaUsageMultiplier = new LimitedStat(character, CharacterStatType.ManaUsageMultiplier, template.ManaUsageMultiplier + ManaUsageMultiplierDif, 0, MaxLimitStatMax);
                stats.ManaRegen = new Stat(character, CharacterStatType.ManaRegen, template.ManaRegen + ManaRegenDif);
                stats.ManaRegenPercentage = new Stat(character, CharacterStatType.ManaRegenPercentage, template.ManaRegenPercentage + ManaRegenPercentageDif);
                stats.ManaShield = new LimitedStat(character, CharacterStatType.ManaShield, ManaShieldDif, 0, 1);
                stats.ManaShieldRetaliation = new Stat(character, CharacterStatType.ManaShieldRetaliation, ManaShieldRetaliationDif);
                
                stats.MeleeRetaliation = new Stat(character, CharacterStatType.MeleeRetaliation, MeleeRetaliationDif);

                stats.MovementSpeedMultiplier = new LimitedStat(character, CharacterStatType.MovementSpeedMultiplier, DefaultMultiplier + MovementSpeedMultiplierDif, MovementSpeedMultiplierMinValue, MovementSpeedMultiplierMaxValue);

                stats.Strength = new LimitedStat(character, CharacterStatType.Strength, template.Strength + StrengthDif, 0, MaxLimitStatMax);
                stats.StrengthLinear = new Stat(character, CharacterStatType.StrengthLinear, template.StrengthLinear + StrengthLinearDif);
                stats.IncomingDamage = new Stat(character, CharacterStatType.IncomingDamage, DefaultMultiplier + IncomingDamageDif);
                stats.IncomingHealing = new Stat(character, CharacterStatType.IncomingHealing, DefaultMultiplier + IncomingHealingDif);
                stats.ConsumableHealingBonus = new Stat(character, CharacterStatType.ConsumableHealingBonus, DefaultMultiplier + ConsumableHealingBonusDif);
                stats.PotionHealingBonus = new Stat(character, CharacterStatType.PotionHealingBonus, DefaultMultiplier + PotionHealingBonusDif);

                stats.Resistance = new Stat(character, CharacterStatType.Resistance, template.Resistance + ResistanceDif);
                stats.Evasion = new LimitedStat(character, CharacterStatType.Evasion, template.Evasion + EvasionDif, 0, World.Services.Get<GameConstants>().evasionCap);
                stats.LifeSteal = new Stat(character, CharacterStatType.LifeSteal, LifeStealDif);
                
                stats.BuffStrength = new LimitedStat(character, CharacterStatType.BuffStrength, DefaultMultiplier + BuffStrengthDif, 0, MaxLimitStatMax);
                stats.BuffDuration = new LimitedStat(character, CharacterStatType.BuffDuration, DefaultMultiplier + BuffDurationDif, 0, MaxLimitStatMax);
                stats.DebuffStrength = new LimitedStat(character, CharacterStatType.DebuffStrength, DefaultMultiplier + DebuffStrengthDif, 0, MaxLimitStatMax);
                stats.DebuffDuration = new LimitedStat(character, CharacterStatType.DebuffDuration, DefaultMultiplier + DebuffDurationDif, 0, MaxLimitStatMax);

                stats.AttackSpeed = new LimitedStat(character, CharacterStatType.AttackSpeed, DefaultMultiplier + AttackSpeedDif, AttackSpeedMinValue, AttackSpeedMaxValue);
                stats.OneHandedLightAttackSpeed = new LimitedStat(character, CharacterStatType.OneHandedLightAttackSpeed, DefaultMultiplier + OneHandedLightAttackSpeedDif, LightAttackSpeedMinValue, LightAttackSpeedMaxValue);
                stats.OneHandedHeavyAttackSpeed = new LimitedStat(character, CharacterStatType.OneHandedHeavyAttackSpeed, DefaultHeavyAttackSpeed + OneHandedHeavyAttackSpeedDif, HeavyAttackSpeedMinValue, HeavyAttackSpeedMaxValue);
                stats.TwoHandedLightAttackSpeed = new LimitedStat(character, CharacterStatType.TwoHandedLightAttackSpeed, DefaultMultiplier + TwoHandedLightAttackSpeedDif, TwoHandedLightAttackSpeedMinValue, TwoHandedLightAttackSpeedMaxValue);
                stats.TwoHandedHeavyAttackSpeed = new LimitedStat(character, CharacterStatType.TwoHandedHeavyAttackSpeed, DefaultHeavyAttackSpeed + TwoHandedHeavyAttackSpeedDif, TwoHandedHeavyAttackSpeedMinValue, TwoHandedHeavyAttackSpeedMaxValue);
                stats.DualHandedLightAttackSpeed = new LimitedStat(character, CharacterStatType.DualHandedLightAttackSpeed, DefaultFistLightAttackSpeed + DualHandedLightAttackSpeedDif, LightAttackSpeedMinValue, LightAttackSpeedMaxValue);
                stats.DualHandedHeavyAttackSpeed = new LimitedStat(character, CharacterStatType.DualHandedHeavyAttackSpeed, DefaultFistHeavyAttackSpeed + DualHandedHeavyAttackSpeedDif, HeavyAttackSpeedMinValue, HeavyAttackSpeedMaxValue);
                stats.FistLightAttackSpeed = new LimitedStat(character, CharacterStatType.FistLightAttackSpeed, DefaultMultiplier + FistLightAttackSpeedDif, LightAttackSpeedMinValue, LightAttackSpeedMaxValue);
                stats.FistHeavyAttackSpeed = new LimitedStat(character, CharacterStatType.FistHeavyAttackSpeed, DefaultHeavyAttackSpeed + FistHeavyAttackSpeedDif, HeavyAttackSpeedMinValue, HeavyAttackSpeedMaxValue);
                stats.BowDrawSpeed = new LimitedStat(character, CharacterStatType.BowDrawSpeed, DefaultMultiplier + BowDrawSpeedDif, BowDrawSpeedMinValue, BowDrawSpeedMaxValue);
                stats.BlockPrepareSpeed = new LimitedStat(character, CharacterStatType.BlockPrepareSpeed, DefaultMultiplier + BlockPrepareSpeedDif, BlockPrepareSpeedMinValue, BlockPrepareSpeedMaxValue);
                stats.SpellChargeSpeed = new LimitedStat(character, CharacterStatType.SpellChargeSpeed, DefaultMultiplier + SpellChargeSpeedDif, SpellChargeSpeedMinValue, SpellChargeSpeedMaxValue);
                stats.DeflectPrecision = new LimitedStat(character, CharacterStatType.DeflectPrecision, DeflectPrecisionDif, 0, 1);
                
                stats.MeleeDamageMultiplier = new LimitedStat(character, CharacterStatType.MeleeDamageMultiplier, DefaultMultiplier + MeleeDamageMultiplierDif, 0, MaxLimitStatMax);
                stats.OneHandedMeleeDamageMultiplier = new LimitedStat(character, CharacterStatType.OneHandedMeleeDamageMultiplier, DefaultMultiplier + OneHandedMeleeDamageMultiplierDif, 0, MaxLimitStatMax);
                stats.TwoHandedMeleeDamageMultiplier = new LimitedStat(character, CharacterStatType.TwoHandedMeleeDamageMultiplier, DefaultMultiplier + TwoHandedMeleeDamageMultiplierDif, 0, MaxLimitStatMax);
                stats.UnarmedMeleeDamageMultiplier = new LimitedStat(character, CharacterStatType.UnarmedMeleeDamageMultiplier, DefaultMultiplier + UnarmedMeleeDamageMultiplierDif, 0, MaxLimitStatMax);
                stats.RangedDamageMultiplier = new LimitedStat(character, CharacterStatType.RangedDamageMultiplier, DefaultMultiplier + RangedDamageMultiplierDif, 0, MaxLimitStatMax);
                
                stats.MagicStrength = new Stat(character, CharacterStatType.MagicStrength, DefaultMultiplier + MagicStrengthDif);
                stats.HoldBlockCostReduction = new LimitedStat(character, CharacterStatType.HoldBlockCostReduction, DefaultMultiplier + HoldBlockCostReductionDif, 0, 1);
            }

            public void PrepareForSave(CharacterStats characterStats) {
                ITemplate template = characterStats.ParentModel.CharacterStatsTemplate;
                
                LevelDif = characterStats.Level.ValueForSave - template.Level;
                TalentPointsDif = characterStats.TalentPoints.ValueForSave - template.TalentPoints;
                BaseStatPointsDif = characterStats.BaseStatPoints.ValueForSave - template.BaseStatPoints;
                
                MaxStaminaDif = characterStats.MaxStamina.ValueForSave - template.MaxStamina;
                StaminaDif = characterStats.Stamina.ValueForSave - characterStats.MaxStamina.ValueForSave;
                StaminaRegenDif = characterStats.StaminaRegen.ValueForSave - template.StaminaRegen;
                StaminaUsageMultiplierDif = characterStats.StaminaUsageMultiplier.ValueForSave - template.StaminaUsageMultiplier;
                SprintCostMultiplierDif = characterStats.SprintCostMultiplier.ValueForSave - DefaultMultiplier;
                
                MaxManaDif = characterStats.MaxMana.ValueForSave - template.MaxMana;
                ManaDif = characterStats.Mana.ValueForSave - characterStats.MaxMana.ValueForSave;
                ManaUsageMultiplierDif = characterStats.ManaUsageMultiplier.ValueForSave - template.ManaUsageMultiplier;
                ManaRegenDif = characterStats.ManaRegen.ValueForSave - template.ManaRegen;
                ManaRegenPercentageDif = characterStats.ManaRegenPercentage.ValueForSave - template.ManaRegenPercentage;
                ManaShieldDif = characterStats.ManaShield.ValueForSave;
                ManaShieldRetaliationDif = characterStats.ManaShieldRetaliation.ValueForSave;
                
                MeleeRetaliationDif = characterStats.MeleeRetaliation.ValueForSave;
                
                MovementSpeedMultiplierDif = characterStats.MovementSpeedMultiplier.ValueForSave - DefaultMultiplier;
                
                StrengthDif = characterStats.Strength.ValueForSave - template.Strength;
                StrengthLinearDif = characterStats.StrengthLinear.ValueForSave - template.StrengthLinear;
                
                IncomingDamageDif = characterStats.IncomingDamage.ValueForSave - DefaultMultiplier;
                IncomingHealingDif = characterStats.IncomingHealing.ValueForSave - DefaultMultiplier;
                ConsumableHealingBonusDif = characterStats.ConsumableHealingBonus.ValueForSave - DefaultMultiplier;
                PotionHealingBonusDif = characterStats.PotionHealingBonus.ValueForSave - DefaultMultiplier;

                EvasionDif = characterStats.Evasion.ValueForSave - template.Evasion;
                ResistanceDif = characterStats.Resistance.ValueForSave - template.Resistance;
                LifeStealDif = characterStats.LifeSteal.ValueForSave;
                
                BuffStrengthDif = characterStats.BuffStrength.ValueForSave - DefaultMultiplier;
                BuffDurationDif = characterStats.BuffDuration.ValueForSave - DefaultMultiplier;
                DebuffStrengthDif = characterStats.DebuffStrength.ValueForSave - DefaultMultiplier;
                DebuffDurationDif = characterStats.DebuffDuration.ValueForSave - DefaultMultiplier;

                //speeds
                AttackSpeedDif = characterStats.AttackSpeed.ValueForSave - DefaultMultiplier;

                //oneHanded
                OneHandedLightAttackSpeedDif = characterStats.OneHandedLightAttackSpeed.ValueForSave - DefaultMultiplier;
                OneHandedHeavyAttackSpeedDif = characterStats.OneHandedHeavyAttackSpeed.ValueForSave - DefaultHeavyAttackSpeed;
                //twoHanded
                TwoHandedLightAttackSpeedDif = characterStats.TwoHandedLightAttackSpeed.ValueForSave - DefaultMultiplier;
                TwoHandedHeavyAttackSpeedDif = characterStats.TwoHandedHeavyAttackSpeed.ValueForSave - DefaultHeavyAttackSpeed;
                //dualHanded
                DualHandedLightAttackSpeedDif = characterStats.DualHandedLightAttackSpeed.ValueForSave - DefaultFistLightAttackSpeed;
                DualHandedHeavyAttackSpeedDif = characterStats.DualHandedHeavyAttackSpeed.ValueForSave - DefaultFistHeavyAttackSpeed;
                //fists
                FistLightAttackSpeedDif = characterStats.FistLightAttackSpeed.ValueForSave - DefaultMultiplier;
                FistHeavyAttackSpeedDif = characterStats.FistHeavyAttackSpeed.ValueForSave - DefaultHeavyAttackSpeed;                
                //bow
                BowDrawSpeedDif = characterStats.BowDrawSpeed.ValueForSave - DefaultMultiplier;
                //block
                BlockPrepareSpeedDif = characterStats.BlockPrepareSpeed.ValueForSave - DefaultMultiplier;
                //spell
                SpellChargeSpeedDif = characterStats.SpellChargeSpeed.ValueForSave - DefaultMultiplier;
                //deflect
                DeflectPrecisionDif = characterStats.DeflectPrecision.ValueForSave;
                
                MeleeDamageMultiplierDif = characterStats.MeleeDamageMultiplier.ValueForSave - DefaultMultiplier;
                OneHandedMeleeDamageMultiplierDif = characterStats.OneHandedMeleeDamageMultiplier.ValueForSave - DefaultMultiplier;
                TwoHandedMeleeDamageMultiplierDif = characterStats.TwoHandedMeleeDamageMultiplier.ValueForSave - DefaultMultiplier;
                UnarmedMeleeDamageMultiplierDif = characterStats.UnarmedMeleeDamageMultiplier.ValueForSave - DefaultMultiplier;
                RangedDamageMultiplierDif = characterStats.RangedDamageMultiplier.ValueForSave - DefaultMultiplier;
                
                // Weapon Multipliers
                MagicStrengthDif = characterStats.MagicStrength.ValueForSave - DefaultMultiplier;
                HoldBlockCostReductionDif = characterStats.HoldBlockCostReduction.ValueForSave - DefaultMultiplier;
            }
        }
    }
}