using System.Collections.Generic;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.NewGamePlus;
using Awaken.TG.MVC;
using Awaken.TG.MVC.Elements;
using Awaken.TG.MVC.Events;
using Awaken.TG.Utility.Attributes;
using Awaken.Utility;

namespace Awaken.TG.Main.Character {
    /// <summary>
    /// Stats that every character is based on.
    /// </summary>
    public sealed partial class AliveStats : Element<IAlive> {
        public override ushort TypeForSerialization => SavedModels.AliveStats;

        [Saved] AliveStatsWrapper _wrapper;
        
        public Stat MaxHealth { get; set; }
        public LimitedStat Health { get; set; }
        public Stat HealthRegen { get; set; }
        public Stat ArmorMultiplier { get; set; }
        public Stat Armor { get; set; }
        public DamageReceivedMultiplierData DamageReceivedMultiplierData { get; private set; }
        public LimitedStat StatusResistance { get; set; }
        public Stat ForceStumbleThreshold { get; set; }
        public Stat TrapDamageMultiplier { get; set; }
        [UnityEngine.Scripting.Preserve] public float TotalArmor => ParentModel.TotalArmor(DamageSubType.GenericPhysical);

        IEnumerable<Stat> AllStats() {
            yield return MaxHealth;
            yield return Health;
            yield return HealthRegen;
            yield return ArmorMultiplier;
            yield return Armor;
            yield return StatusResistance;
            yield return ForceStumbleThreshold;
            yield return TrapDamageMultiplier;
        }

        // === Events
        public new static class Events {
            public static readonly Event<IAlive, AliveStats> GeneralStatsAdded = new(nameof(GeneralStatsAdded));
        }

        protected override void OnInitialize() {
            _wrapper.Initialize(this, ParentModel.HeroLevelAtInitialization);
            DamageReceivedMultiplierData = ParentModel.AliveStatsTemplate.GetDamageReceivedMultiplierData();
            ParentModel.Trigger(Events.GeneralStatsAdded, this);
        }

        public static void Create(IAlive parent) {
            AliveStats stats = new();
            parent.AddElement(stats);
        }

        public void RecalculateAllStats(int previousHereLevel, int newHeroLevel, bool saveBefore = true) {
            if (saveBefore) {
                _wrapper.PrepareForSave(this, previousHereLevel);
            }
            _wrapper.Initialize(this, newHeroLevel);
            foreach (var stat in AllStats()) {
                stat.SetTo(stat.BaseValue);
            }
        }

        // === Persistence

        void OnBeforeWorldSerialize() {
            _wrapper.PrepareForSave(this, ParentModel.HeroLevelAtInitialization);
        }
        
        public interface ITemplate {
            public int MaxHealth { get; }
            public float ArmorMultiplier { get; }
            public int Armor { get; }
            public DamageReceivedMultiplierData GetDamageReceivedMultiplierData();
            public float StatusResistance { get; }
            public float ForceStumbleThreshold { get; }
            public float TrapDamageMultiplier { get; }
        }
        
        public partial struct AliveStatsWrapper {
            public ushort TypeForSerialization => SavedTypes.AliveStatsWrapper;

            [Saved(0f)] float MaxHealthDif;
            [Saved(0f)] float HealthDif;
            [Saved(0f)] float HealthRegenDif;
            [Saved(0f)] float ArmorMultiplierDif;
            [Saved(0f)] float ArmorDif;
            [Saved(0f)] float StatusResistanceDif;
            [Saved(0f)] float ForceStumbleThresholdDif;
            [Saved(0f)] float TrapDamageMultiplierDif;
            
            public void Initialize(AliveStats stats, int heroLevel) {
                IAlive parent = stats.ParentModel;
                ITemplate data = parent.AliveStatsTemplate;

                int ngLevel = parent.NewGamePlusLevel;
                NewGamePlusSystem.GetAllAliveStats(data, ngLevel, heroLevel, out var health);

                stats.MaxHealth = new Stat(parent, AliveStatType.MaxHealth, health + MaxHealthDif);
                stats.Health = new LimitedStat(parent, AliveStatType.Health, stats.MaxHealth + HealthDif, 0, AliveStatType.MaxHealth, upperLimitMinimumValue: 1);
                stats.HealthRegen = new Stat(parent, AliveStatType.HealthRegen, HealthRegenDif);
                stats.ArmorMultiplier = new Stat(parent, AliveStatType.ArmorMultiplier, data.ArmorMultiplier + ArmorMultiplierDif);
                stats.Armor = new Stat(parent, AliveStatType.Armor, data.Armor + ArmorDif);
                stats.StatusResistance = new LimitedStat(parent, AliveStatType.StatusResistance, data.StatusResistance + StatusResistanceDif, 0, 1);
                stats.ForceStumbleThreshold = new Stat(parent, AliveStatType.ForceStumbleThreshold, data.ForceStumbleThreshold + ForceStumbleThresholdDif);
                stats.TrapDamageMultiplier = new Stat(parent, AliveStatType.TrapDamageMultiplier, data.TrapDamageMultiplier + TrapDamageMultiplierDif);
            }

            public void PrepareForSave(AliveStats aliveStats, int heroLevel) {
                IAlive parent = aliveStats.ParentModel;
                ITemplate defaultData = parent.AliveStatsTemplate;

                int ngLevel = parent.NewGamePlusLevel;
                NewGamePlusSystem.GetAllAliveStats(defaultData, ngLevel, heroLevel, out var health);
                
                MaxHealthDif = aliveStats.MaxHealth.ValueForSave - health;
                HealthDif = aliveStats.Health.ValueForSave - aliveStats.MaxHealth.ValueForSave;
                HealthRegenDif = aliveStats.HealthRegen.ValueForSave;
                ArmorMultiplierDif = aliveStats.ArmorMultiplier.ValueForSave - defaultData.ArmorMultiplier;
                ArmorDif = aliveStats.Armor.ValueForSave - defaultData.Armor;
                StatusResistanceDif = aliveStats.StatusResistance.ValueForSave - defaultData.StatusResistance;
                ForceStumbleThresholdDif = aliveStats.ForceStumbleThreshold.ValueForSave - defaultData.ForceStumbleThreshold;
                TrapDamageMultiplierDif = aliveStats.TrapDamageMultiplier.ValueForSave - defaultData.TrapDamageMultiplier;
            }
        }
    }
}
