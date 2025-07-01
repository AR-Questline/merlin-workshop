using Awaken.Utility;
using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Items.Attachments;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Heroes.Stats.Utils;
using Awaken.TG.Main.Saving;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.Utility.Attributes;
using Newtonsoft.Json;

namespace Awaken.TG.Main.Heroes.Items.Weapons {
    public partial class ItemStats : Element<Item> {
        public override ushort TypeForSerialization => SavedModels.ItemStats;

        [Saved] ItemStatsWrapper _wrapper;
        // === Properties
        // --- Stamina Costs
        public ItemStat LightAttackCost { get; private set; }
        public ItemStat HeavyAttackCost { get; private set; }
        public ItemStat HeavyAttackHoldCostPerTick { get; private set; }
        public ItemStat DrawBowCostPerTick { get; private set; }
        public ItemStat HoldItemCostPerTick { get; private set; }
        public ItemStat PushStaminaCost { get; private set; }
        public ItemStat BlockStaminaCostMultiplier { get; private set; }
        public ItemStat ParryStaminaCost { get; private set; }
        // --- Damage
        public ItemStat BaseMinDmg { get; private set; }
        public ItemStat BaseMaxDmg { get; private set; }
        public RandomRangeStat DamageValue { get; private set; }
        public ItemStat DamageGain { get; private set; }
        public ItemStat HeavyAttackDamageMultiplier { get; private set; }
        public ItemStat PushDamageMultiplier { get; private set; }
        public ItemStat BackStabDamageMultiplier { get; private set; }
        public DamageTypeData DamageTypeData { get; private set; }
        public RuntimeDamageTypeData RuntimeDamageTypeData => DamageTypeData.GetRuntimeData();
        public ProfStatType ProfFromAbstract { get; private set; }
        public ItemStat ArmorPenetration { get; private set; }
        public ItemStat DamageIncreasePerCharge { get; private set; }
        // --- Criticals
        public ItemStat CriticalDamageMultiplier { get; private set; }
        public ItemStat WeakSpotDamageMultiplier { get; private set; }
        public ItemStat SneakDamageMultiplier { get; private set; }
        // --- Armor & Blocking
        public ItemStat Armor { get; private set; }
        public ItemStat ArmorGain { get; private set; }
        public ItemStat BlockAngle { get; private set; }
        public LimitedStat Block { get; private set; }
        public ItemStat BlockGain { get; private set; }
        // --- Force
        public ItemStat ForceDamage { get; private set; }
        public ItemStat ForceDamageGain { get; private set; }
        public ItemStat ForceDamagePushMultiplier { get; private set; }
        public ItemStat RagdollForce { get; private set; }
        // --- Poise
        public ItemStat PoiseDamage { get; private set; }
        public ItemStat PoiseDamageGain { get; private set; }
        public ItemStat PoiseDamageHeavyAttackMultiplier { get; private set; }
        public ItemStat PoiseDamagePushMultiplier { get; private set; }
        // --- Weight
        public LimitedStat Weight { get; private set; }
        // --- Misc
        public ItemStat NpcDamageMultiplier { get; private set; }
        public ItemStat RangedZoomModifier { get; private set; }
        public ItemStat RangedDrawSpeedModifier { get; private set; }
        public float RandomOccurrenceEfficiency => _dataSource.randomnessModifier;
        public float AttacksPerSecond => _dataSource.attacksPerSecond;
        [UnityEngine.Scripting.Preserve] public (float, float) DamagePerSecond => 
            (BaseMinDmg.ModifiedValue * AttacksPerSecond, BaseMaxDmg.ModifiedValue * AttacksPerSecond);
        // --- Magic
        public ItemStat LightCastManaCost { get; private set; }
        public ItemStat HeavyCastManaCost { get; private set; }
        public ItemStat HeavyCastManaCostPerSecond { get; private set; }
        public LimitedStat ChargeAmount { get; private set; }
        public LimitedStat MagicHeldSpeedMultiplier { get; private set; }
        
        // === Fields
        ItemStatsAttachment _dataSource;
        
        // === Gained values
        float _appliedDamageGain, _appliedArmorGain, _appliedBlockGain, _appliedForceGain, _appliedPoiseDamageGain, _appliedWeightLoss;
        
        // === Constructors
        [JsonConstructor, UnityEngine.Scripting.Preserve]
        public ItemStats() {}

        // === Initialization
        protected override void OnInitialize() {
            ProfFromAbstract = ProfUtils.ProfFromAbstracts(ParentModel, true);
            _dataSource = ParentModel.Template.GetAttachment<ItemStatsAttachment>();
            DamageTypeData = _dataSource.GetDamageTypeData();
            _wrapper.Initialize(this);
        }
        
        protected override void OnFullyInitialized() {
            InitListeners();
        }

        void InitListeners() {
            ParentModel.ListenTo(Stat.Events.AnyStatChanged, OnStatChanged, this);
            OnStatChanged(ParentModel.Level);
            OnStatChanged(ParentModel.WeightLevel);
        }

        void OnStatChanged(Stat stat) {
            bool levelChanged = stat.Type == ItemStatType.Level;
            
            if (levelChanged || stat.Type == ItemStatType.DamageGain) {
                float temp = _appliedDamageGain;
                ApplyGain(BaseMinDmg, DamageGain, ref temp);
                ApplyGain(BaseMaxDmg, DamageGain, ref _appliedDamageGain);
            }

            if (levelChanged || stat.Type == ItemStatType.ItemArmorGain) {
                ApplyGain(Armor, ArmorGain, ref _appliedArmorGain);
            }

            if (levelChanged || stat.Type == ItemStatType.ItemBlockGain) {
                ApplyGain(Block, BlockGain, ref _appliedBlockGain);
            }

            if (levelChanged || stat.Type == ItemStatType.ItemForceGain) {
                ApplyGain(ForceDamage, ForceDamageGain, ref _appliedForceGain);
            }

            if (levelChanged || stat.Type == ItemStatType.ItemPoiseDamageGain) {
                ApplyGain(PoiseDamage, PoiseDamageGain, ref _appliedPoiseDamageGain);
            }

            if (stat.Type == ItemStatType.ItemWeightLevel) {
                ApplyWeightLoss();
            }
        }
        
        void ApplyWeightLoss() {
            float desiredValue = ParentModel.WeightLevel.ModifiedInt * ParentModel.WeightLoss;
            float change = desiredValue - _appliedWeightLoss;
            _appliedWeightLoss = desiredValue;
            Weight.DecreaseBy(change);
        }

        void ApplyGain(Stat statToTweak, Stat tweakGain, ref float appliedValue) {
            float desiredValue = ParentModel.Level.ModifiedInt * tweakGain.ModifiedValue;
            float change = desiredValue - appliedValue;
            appliedValue = desiredValue;
            statToTweak.IncreaseBy(change);
        }
        
        public Stat GetAttackSpeedMultiplierItemDependent() {
            if (ParentModel.IsRanged) {
                return RangedDrawSpeedModifier;
            }

            return null;
        }
        
        // === Persistence

        void OnBeforeWorldSerialize() {
            _wrapper.PrepareForSave(this);
        }
        
        public partial struct ItemStatsWrapper {
            public ushort TypeForSerialization => SavedTypes.ItemStatsWrapper;

            const float DefaultChargeAmount = 0f;
            
            [Saved(0f)] float LightAttackCostDif;
            [Saved(0f)] float HeavyAttackCostDif;
            [Saved(0f)] float HeavyAttackHoldCostPerTickDif;
            [Saved(0f)] float DrawBowCostPerTickDif;
            [Saved(0f)] float HoldItemCostPerTickDif;
            [Saved(0f)] float PushStaminaCostDif;
            [Saved(0f)] float BlockStaminaCostMultiplierDif;
            [Saved(0f)] float ParryStaminaCostDif;
            [Saved(0f)] float BaseMinDmgDif;
            [Saved(0f)] float BaseMaxDmgDif;
            [Saved(0f)] float DamageGainDif;
            [Saved(0f)] float HeavyAttackDamageMultiplierDif;
            [Saved(0f)] float PushDamageMultiplierDif;
            [Saved(0f)] float BackStabDamageMultiplierDif;
            [Saved(0f)] float ArmorPenetrationDif;
            [Saved(0f)] float DamageIncreasePerChargeDif;
            [Saved(0f)] float ArmorDif;
            [Saved(0f)] float ArmorGainDif;
            [Saved(0f)] float BlockAngleDif;
            [Saved(0f)] float BlockDif;
            [Saved(0f)] float BlockGainDif;
            [Saved(0f)] float ForceDif;
            [Saved(0f)] float ForceGainDif;
            [Saved(0f)] float ForceDamagePushMultiplierDif;
            [Saved(0f)] float RagdollForceDif;
            [Saved(0f)] float PoiseDamageDif;
            [Saved(0f)] float PoiseDamageGainDif;
            [Saved(0f)] float PoiseDamageHeavyAttackMultiplierDif;
            [Saved(0f)] float PoiseDamagePushMultiplierDif;
            [Saved(0f)] float WeightDif;
            [Saved(0f)] float NpcDamageMultiplierDif;
            [Saved(0f)] float RangedZoomModifierDif;
            [Saved(0f)] float RangedDrawSpeedModifierDif;
            [Saved(0f)] float LightCastManaCostDif;
            [Saved(0f)] float HeavyCastManaCostDif;
            [Saved(0f)] float HeavyCastManaCostPerSecondDif;
            [Saved(0f)] float ChargeAmountDif;
            [Saved(0f)] float MagicHeldSpeedMultiplierDif;
            [Saved(0f)] float CriticalDamageMultiplierDif;
            [Saved(0f)] float WeakSpotDamageMultiplierDif;
            [Saved(0f)] float SneakDamageMultiplierDif;

            public void Initialize(ItemStats stats) {
                Item parentModel = stats.ParentModel;
                ItemStatsAttachment dataSource = stats._dataSource;

                stats.LightAttackCost = new ItemStat(parentModel, ItemStatType.LightAttackCost, dataSource.lightAttackStaminaCost + LightAttackCostDif);
                stats.HeavyAttackCost = new ItemStat(parentModel, ItemStatType.HeavyAttackCost, dataSource.heavyAttackStaminaCost + HeavyAttackCostDif);
                stats.HeavyAttackHoldCostPerTick = new ItemStat(parentModel, ItemStatType.HeavyAttackHoldCostPerTick, dataSource.heavyAttackHoldCostPerTick + HeavyAttackHoldCostPerTickDif);
                stats.DrawBowCostPerTick = new ItemStat(parentModel, ItemStatType.DrawBowCostPerTick, dataSource.drawBowStaminaCostPerTick + DrawBowCostPerTickDif);
                stats.HoldItemCostPerTick = new ItemStat(parentModel, ItemStatType.HoldItemCostPerTick, dataSource.holdItemStaminaCostPerTick + HoldItemCostPerTickDif);
                stats.PushStaminaCost = new ItemStat(parentModel, ItemStatType.PushStaminaCost, dataSource.pushStaminaCost + PushStaminaCostDif);
                stats.BlockStaminaCostMultiplier = new ItemStat(parentModel, ItemStatType.BlockStaminaCostMultiplier, dataSource.blockStaminaCostMultiplier + BlockStaminaCostMultiplierDif);
                stats.ParryStaminaCost = new ItemStat(parentModel, ItemStatType.ParryStaminaCost, dataSource.parryStaminaCost + ParryStaminaCostDif);
                
                // --- Damage
                stats.BaseMinDmg = new ItemStat(parentModel, ItemStatType.BaseMinDmg, dataSource.minDamage + BaseMinDmgDif);
                stats.BaseMaxDmg = new ItemStat(parentModel, ItemStatType.BaseMaxDmg, dataSource.maxDamage + BaseMaxDmgDif);
                stats.DamageValue = new RandomRangeStat(parentModel, ItemStatType.DamageValue, stats.BaseMinDmg.Type, stats.BaseMaxDmg.Type); 
                stats.DamageGain = new ItemStat(parentModel, ItemStatType.DamageGain, dataSource.damageGain + DamageGainDif);
                stats.HeavyAttackDamageMultiplier = new ItemStat(parentModel, ItemStatType.HeavyAttackDamageMultiplier, dataSource.heavyAttackDamageMultiplier + HeavyAttackDamageMultiplierDif);
                stats.PushDamageMultiplier = new ItemStat(parentModel, ItemStatType.PushDamageMultiplier, dataSource.pushDamageMultiplier + PushDamageMultiplierDif);
                stats.BackStabDamageMultiplier = new ItemStat(parentModel, ItemStatType.BackStabDamageMultiplier, dataSource.backStabDamageMultiplier + BackStabDamageMultiplierDif);
                stats.ArmorPenetration = new ItemStat(parentModel, ItemStatType.ArmorPenetration, dataSource.armorPenetration + ArmorPenetrationDif);
                stats.DamageIncreasePerCharge = new ItemStat(parentModel, ItemStatType.DamageIncreasePerCharge, dataSource.damageIncreasePerCharge + DamageIncreasePerChargeDif);
                
                // --- Criticals
                stats.CriticalDamageMultiplier = new ItemStat(parentModel, ItemStatType.CriticalDamageMultiplier, dataSource.criticalDamageMultiplier + CriticalDamageMultiplierDif);
                stats.WeakSpotDamageMultiplier = new ItemStat(parentModel, ItemStatType.WeakSpotDamageMultiplier, dataSource.weakSpotDamageMultiplier + WeakSpotDamageMultiplierDif);
                stats.SneakDamageMultiplier = new ItemStat(parentModel, ItemStatType.SneakDamageMultiplier, dataSource.sneakDamageMultiplier + SneakDamageMultiplierDif);

                // --- Armor & Blocking
                stats.Armor = new ItemStat(parentModel, ItemStatType.ItemArmor, dataSource.armor + ArmorDif);
                stats.ArmorGain = new ItemStat(parentModel, ItemStatType.ItemArmorGain, dataSource.armorGain + ArmorGainDif);
                stats.BlockAngle = new ItemStat(parentModel, ItemStatType.ItemBlockAngle, dataSource.blockAngle + BlockAngleDif);
                stats.Block = new LimitedStat(parentModel, ItemStatType.Block, dataSource.blockDamageReductionPercent + BlockDif, 0, 100);
                stats.BlockGain = new ItemStat(parentModel, ItemStatType.ItemBlockGain, dataSource.blockGain + BlockGainDif);
                // --- Force
                stats.ForceDamage = new ItemStat(parentModel, ItemStatType.ItemForce, dataSource.forceDamage + ForceDif);
                stats.ForceDamageGain = new ItemStat(parentModel, ItemStatType.ItemForceGain, dataSource.forceDamageGain + ForceGainDif);
                stats.ForceDamagePushMultiplier = new ItemStat(parentModel, ItemStatType.ItemForceDamagePushMultiplier, dataSource.forceDamagePushMultiplier + ForceDamagePushMultiplierDif);
                stats.RagdollForce = new ItemStat(parentModel, ItemStatType.RagdollForce, dataSource.ragdollForce + RagdollForceDif);
                // --- Poise
                stats.PoiseDamage = new ItemStat(parentModel, ItemStatType.ItemPoiseDamage, dataSource.poiseDamage + PoiseDamageDif);
                stats.PoiseDamageGain = new ItemStat(parentModel, ItemStatType.ItemPoiseDamageGain, dataSource.poiseDamageGain + PoiseDamageGainDif);
                stats.PoiseDamageHeavyAttackMultiplier = new ItemStat(parentModel, ItemStatType.ItemPoiseDamageHeavyAttackMultiplier, dataSource.poiseDamageHeavyAttackMultiplier + PoiseDamageHeavyAttackMultiplierDif);
                stats.PoiseDamagePushMultiplier = new ItemStat(parentModel, ItemStatType.ItemPoiseDamagePushMultiplier, dataSource.poiseDamagePushMultiplier + PoiseDamagePushMultiplierDif);
                // --- Weight
                stats.Weight = new LimitedStat(parentModel, ItemStatType.ItemWeight, dataSource.Weight + WeightDif, 0, float.MaxValue);
                // --- Misc
                stats.NpcDamageMultiplier = new ItemStat(parentModel, ItemStatType.NpcDamageMultiplier, dataSource.npcDamageMultiplier + NpcDamageMultiplierDif);
                stats.RangedZoomModifier = new ItemStat(parentModel, ItemStatType.RangedZoomModifier, dataSource.rangedZoomModifier + RangedZoomModifierDif);
                stats.RangedDrawSpeedModifier = new ItemStat(parentModel, ItemStatType.RangedDrawSpeedModifier, dataSource.bowDrawSpeedModifier + RangedDrawSpeedModifierDif);
                // --- Magic
                stats.LightCastManaCost = new ItemStat(parentModel, ItemStatType.LightCastManaCost, dataSource.lightCastManaCost + LightCastManaCostDif);
                stats.HeavyCastManaCost = new ItemStat(parentModel, ItemStatType.HeavyCastManaCost, dataSource.heavyCastManaCost + HeavyCastManaCostDif);
                stats.HeavyCastManaCostPerSecond = new ItemStat(parentModel, ItemStatType.HeavyCastManaCostPerSecond, dataSource.heavyCastManaCostPerSecond + HeavyCastManaCostPerSecondDif);
                stats.ChargeAmount = new LimitedStat(parentModel, ItemStatType.ChargeAmount, DefaultChargeAmount + ChargeAmountDif, 0, 1);
                stats.MagicHeldSpeedMultiplier = new LimitedStat(parentModel, ItemStatType.MagicHeldSpeedMultiplier, dataSource.magicHeldSpeedMultiplier + MagicHeldSpeedMultiplierDif, 0, 1);
            }

            public void PrepareForSave(ItemStats itemStats) {
                ItemStatsAttachment dataSource = itemStats._dataSource;
                
                LightAttackCostDif = itemStats.LightAttackCost.BaseValue - dataSource.lightAttackStaminaCost;
                HeavyAttackCostDif = itemStats.HeavyAttackCost.BaseValue - dataSource.heavyAttackStaminaCost;
                HeavyAttackHoldCostPerTickDif = itemStats.HeavyAttackHoldCostPerTick.BaseValue - dataSource.heavyAttackHoldCostPerTick;
                DrawBowCostPerTickDif = itemStats.DrawBowCostPerTick.BaseValue - dataSource.drawBowStaminaCostPerTick;
                HoldItemCostPerTickDif = itemStats.HoldItemCostPerTick.BaseValue - dataSource.holdItemStaminaCostPerTick;
                PushStaminaCostDif = itemStats.PushStaminaCost.BaseValue - dataSource.pushStaminaCost;
                BlockStaminaCostMultiplierDif = itemStats.BlockStaminaCostMultiplier.BaseValue - dataSource.blockStaminaCostMultiplier;
                ParryStaminaCostDif = itemStats.ParryStaminaCost.BaseValue - dataSource.parryStaminaCost;
                
                BaseMinDmgDif = itemStats.BaseMinDmg.BaseValue - dataSource.minDamage - itemStats._appliedDamageGain;
                BaseMaxDmgDif = itemStats.BaseMaxDmg.BaseValue - dataSource.maxDamage - itemStats._appliedDamageGain;
                DamageGainDif = itemStats.DamageGain.BaseValue - dataSource.damageGain;
                HeavyAttackDamageMultiplierDif = itemStats.HeavyAttackDamageMultiplier.BaseValue - dataSource.heavyAttackDamageMultiplier;
                PushDamageMultiplierDif = itemStats.PushDamageMultiplier.BaseValue - dataSource.pushDamageMultiplier;
                BackStabDamageMultiplierDif = itemStats.BackStabDamageMultiplier.BaseValue - dataSource.backStabDamageMultiplier;
                ArmorPenetrationDif = itemStats.ArmorPenetration.BaseValue - dataSource.armorPenetration;
                DamageIncreasePerChargeDif = itemStats.DamageIncreasePerCharge.BaseValue - dataSource.damageIncreasePerCharge;
                ArmorDif = itemStats.Armor.BaseValue - dataSource.armor - itemStats._appliedArmorGain;
                ArmorGainDif = itemStats.ArmorGain.BaseValue - dataSource.armorGain;
                BlockAngleDif = itemStats.BlockAngle.BaseValue - dataSource.blockAngle;
                BlockDif = itemStats.Block.BaseValue - dataSource.blockDamageReductionPercent - itemStats._appliedBlockGain;
                BlockGainDif = itemStats.BlockGain.BaseValue - dataSource.blockGain;
                ForceDif = itemStats.ForceDamage.BaseValue - dataSource.forceDamage - itemStats._appliedForceGain;
                ForceGainDif = itemStats.ForceDamageGain.BaseValue - dataSource.forceDamageGain;
                ForceDamagePushMultiplierDif = itemStats.ForceDamagePushMultiplier.BaseValue - dataSource.forceDamagePushMultiplier;
                RagdollForceDif = itemStats.RagdollForce.BaseValue - dataSource.ragdollForce;
                PoiseDamageDif = itemStats.PoiseDamage.BaseValue - dataSource.poiseDamage - itemStats._appliedPoiseDamageGain;
                PoiseDamageGainDif = itemStats.PoiseDamageGain.BaseValue - dataSource.poiseDamageGain;
                PoiseDamageHeavyAttackMultiplierDif = itemStats.PoiseDamageHeavyAttackMultiplier.BaseValue - dataSource.poiseDamageHeavyAttackMultiplier;
                PoiseDamagePushMultiplierDif = itemStats.PoiseDamagePushMultiplier.BaseValue - dataSource.poiseDamagePushMultiplier;
                WeightDif = itemStats.Weight.BaseValue - dataSource.Weight + itemStats._appliedWeightLoss;
                NpcDamageMultiplierDif = itemStats.NpcDamageMultiplier.BaseValue - dataSource.npcDamageMultiplier;
                RangedZoomModifierDif = itemStats.RangedZoomModifier.BaseValue - dataSource.rangedZoomModifier;
                RangedDrawSpeedModifierDif = itemStats.RangedDrawSpeedModifier.BaseValue - dataSource.bowDrawSpeedModifier;
                LightCastManaCostDif = itemStats.LightCastManaCost.BaseValue - dataSource.lightCastManaCost;
                HeavyCastManaCostDif = itemStats.HeavyCastManaCost.BaseValue - dataSource.heavyCastManaCost;
                HeavyCastManaCostPerSecondDif = itemStats.HeavyCastManaCostPerSecond.BaseValue - dataSource.heavyCastManaCostPerSecond;
                ChargeAmountDif = itemStats.ChargeAmount.BaseValue - DefaultChargeAmount;
                MagicHeldSpeedMultiplierDif = itemStats.MagicHeldSpeedMultiplier.BaseValue - dataSource.magicHeldSpeedMultiplier;
                CriticalDamageMultiplierDif = itemStats.CriticalDamageMultiplier.BaseValue - dataSource.criticalDamageMultiplier;
                WeakSpotDamageMultiplierDif = itemStats.WeakSpotDamageMultiplier.BaseValue - dataSource.weakSpotDamageMultiplier;
                SneakDamageMultiplierDif = itemStats.SneakDamageMultiplier.BaseValue - dataSource.sneakDamageMultiplier;
            }
        }
    }
}