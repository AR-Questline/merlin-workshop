using System;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Utility.RichEnums;

namespace Awaken.TG.Main.Heroes.Items {
    [RichEnumDisplayCategory("Items")]
    public class ItemStatType : StatType<Item> {

        public static readonly ItemStatType
            Level = new(nameof(Level), LocTerms.ItemLevel, item => item.Level, "Level", new Param {Tweakable = false, Abbreviation = "lvl"}),
            LightAttackCost = new(nameof(LightAttackCost), LocTerms.LightAttackCost, h => h.ItemStats?.LightAttackCost, "Fight"),
            HeavyAttackCost = new(nameof(HeavyAttackCost), LocTerms.HeavyAttackCost, h => h.ItemStats?.HeavyAttackCost, "Fight"),
            
            HeavyAttackHoldCostPerTick = new(nameof(HeavyAttackHoldCostPerTick), "", h => h.ItemStats?.HeavyAttackHoldCostPerTick, "Fight"),
            DrawBowCostPerTick = new(nameof(DrawBowCostPerTick), "", h => h.ItemStats?.DrawBowCostPerTick, "Fight"),
            HoldItemCostPerTick = new(nameof(HoldItemCostPerTick), "", h => h.ItemStats?.HoldItemCostPerTick, "Fight"),
            PushStaminaCost = new(nameof(PushStaminaCost), "", h => h.ItemStats?.PushStaminaCost, "Fight"),
            BlockStaminaCostMultiplier = new(nameof(BlockStaminaCostMultiplier), LocTerms.BlockStaminaCostMultiplier, h => h.ItemStats?.BlockStaminaCostMultiplier, "Fight"),
            ParryStaminaCost = new(nameof(ParryStaminaCost), "", h => h.ItemStats?.ParryStaminaCost, "Fight"),
            
            BaseMinDmg = new(nameof(BaseMinDmg), LocTerms.CombatDamage, h => h.ItemStats?.BaseMinDmg, "Fight", new Param {Abbreviation = "Dmg"}),
            BaseMaxDmg = new(nameof(BaseMaxDmg), LocTerms.CombatDamage, h => h.ItemStats?.BaseMaxDmg, "Fight", new Param {Abbreviation = "Dmg"}),
            DamageValue = new(nameof(DamageValue), LocTerms.DamageValue, h => h.ItemStats?.DamageValue, "Fight", new Param {Tweakable = false, Abbreviation = "Dmg"}),
            DamageGain = new(nameof(DamageGain), string.Empty, h => h.ItemStats?.DamageGain, "Fight", new Param {Abbreviation = "Dmg"}),
            
            HeavyAttackDamageMultiplier = new(nameof(HeavyAttackDamageMultiplier), string.Empty, h => h.ItemStats?.HeavyAttackDamageMultiplier, "Fight", new Param {Abbreviation = "Dmg"}),
            PushDamageMultiplier = new(nameof(PushDamageMultiplier), string.Empty, h => h.ItemStats?.PushDamageMultiplier, "Fight", new Param {Abbreviation = "Dmg"}),
            BackStabDamageMultiplier = new(nameof(BackStabDamageMultiplier), string.Empty, h => h.ItemStats?.BackStabDamageMultiplier, "Fight", new Param {Abbreviation = "Dmg"}),
            
            ArmorPenetration = new(nameof(ArmorPenetration), LocTerms.ArmorPenetration, h => h.ItemStats?.ArmorPenetration, "Fight"),
            DamageIncreasePerCharge = new(nameof(DamageIncreasePerCharge), LocTerms.DamageIncreasePerCharge, h => h.ItemStats?.DamageIncreasePerCharge, "Fight", new Param {Abbreviation = "Dmg"}),
            
            CriticalDamageMultiplier = new(nameof(CriticalDamageMultiplier), LocTerms.CriticalDamageMultiplier, h => h.ItemStats?.CriticalDamageMultiplier, "Fight", new Param {Abbreviation = "Crit"}),
            WeakSpotDamageMultiplier = new(nameof(WeakSpotDamageMultiplier), LocTerms.WeakSpotDamageMultiplier, h => h.ItemStats?.WeakSpotDamageMultiplier, "Fight", new Param {Abbreviation = "Crit"}),
            SneakDamageMultiplier = new(nameof(SneakDamageMultiplier), LocTerms.SneakDamageMultiplier, h => h.ItemStats?.SneakDamageMultiplier, "Fight", new Param {Abbreviation = "Crit"}),
           
            ItemArmor = new(nameof(ItemArmor), LocTerms.ItemArmor, h => h.ItemStats?.Armor, "Armor", new Param{Abbreviation = "Armor"}),
            ItemArmorGain = new(nameof(ItemArmorGain), LocTerms.ItemArmor, h => h.ItemStats?.ArmorGain, "Armor", new Param{Abbreviation = "Armor"}),
            ItemBlockAngle = new(nameof(ItemBlockAngle), LocTerms.ItemBlockAngle, h => h.ItemStats?.BlockAngle, "Block", new Param{Abbreviation = "BlockAngle"}),
            Block = new(nameof(Block), LocTerms.ItemBlock, h => h.ItemStats?.Block, "Block", new Param{Abbreviation = "Block"}),
            ItemBlockGain = new(nameof(ItemBlockGain), LocTerms.ItemBlock, h => h.ItemStats?.BlockGain, "Block", new Param{Abbreviation = "Block"}),
            
            ItemForce = new(nameof(ItemForce), LocTerms.ItemForce, h => h.ItemStats?.ForceDamage, "Force", new Param{Abbreviation = "Force"}),
            ItemForceGain = new(nameof(ItemForceGain), LocTerms.ItemForceGain, h => h.ItemStats?.ForceDamageGain, "Force", new Param{Abbreviation = "ForceGain"}),
            ItemForceDamagePushMultiplier = new(nameof(ItemForceDamagePushMultiplier), LocTerms.ItemForceDamagePushMultiplier, h => h.ItemStats?.ForceDamagePushMultiplier, "Force", new Param{Abbreviation = "ForcePushGain"}),
            RagdollForce = new(nameof(RagdollForce), "", h => h.ItemStats?.RagdollForce, "Force", new Param{Abbreviation = "Force"}),
            
            ItemPoiseDamage = new(nameof(ItemPoiseDamage), LocTerms.ItemPoiseDamage, h => h.ItemStats?.PoiseDamage, "Poise", new Param{Abbreviation = "PoiseDamage"}),
            ItemPoiseDamageGain = new(nameof(ItemPoiseDamageGain), LocTerms.ItemPoiseDamageGain, h => h.ItemStats?.PoiseDamageGain, "Poise", new Param{Abbreviation = "PoiseDamageGain"}),
            ItemPoiseDamageHeavyAttackMultiplier = new(nameof(ItemPoiseDamageHeavyAttackMultiplier), LocTerms.ItemPoiseDamageHeavyAttackMultiplier, h => h.ItemStats?.PoiseDamageHeavyAttackMultiplier, "Poise", new Param{Abbreviation = "PoiseDamageGain"}),
            ItemPoiseDamagePushMultiplier = new(nameof(ItemPoiseDamagePushMultiplier), LocTerms.ItemPoiseDamagePushMultiplier, h => h.ItemStats?.PoiseDamagePushMultiplier, "Poise", new Param{Abbreviation = "PoiseDamageGain"}),
            
            ItemWeight = new(nameof(ItemWeight), LocTerms.Weight, h => h.ItemStats?.Weight, "Weight", new Param{Abbreviation = "Weight"}),
            ItemWeightLevel = new(nameof(ItemWeightLevel), LocTerms.WeightLevel, h => h.WeightLevel, "Weight", new Param{Abbreviation = "WeightLevel"}),
            
            NpcDamageMultiplier = new(nameof(NpcDamageMultiplier), "", h => h.ItemStats?.NpcDamageMultiplier, "Misc"),
            RangedZoomModifier = new (nameof(RangedZoomModifier), "", h => h.ItemStats?.RangedZoomModifier, "Misc"),
            RangedDrawSpeedModifier = new (nameof(RangedDrawSpeedModifier), "", h => h.ItemStats?.RangedDrawSpeedModifier, "Misc"),
            LightCastManaCost = new (nameof(LightCastManaCost), "", h => h.ItemStats?.LightCastManaCost, "Magic"),
            HeavyCastManaCost = new (nameof(HeavyCastManaCost), "", h => h.ItemStats?.HeavyCastManaCost, "Magic"),
            HeavyCastManaCostPerSecond = new (nameof(HeavyCastManaCostPerSecond), "", h => h.ItemStats?.HeavyCastManaCostPerSecond, "Magic"),
            ChargeAmount = new (nameof(ChargeAmount), "", h => h.ItemStats?.ChargeAmount, "Magic"),
            MagicHeldSpeedMultiplier = new (nameof(MagicHeldSpeedMultiplier), "", h => h.ItemStats?.MagicHeldSpeedMultiplier, "Magic");
        
        protected ItemStatType(string id, string displayName, Func<Item, Stat> getter, string inspectorCategory = "", Param param = null)
            : base(id, displayName, getter, inspectorCategory, param) { }
    }
}