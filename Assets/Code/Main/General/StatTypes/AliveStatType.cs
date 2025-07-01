using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Utility.RichEnums;

namespace Awaken.TG.Main.General.StatTypes {
    [RichEnumDisplayCategory("Alive")]
    public class AliveStatType : StatType<IAlive> {
        protected AliveStatType(string id, string displayName, Func<IAlive, Stat> getter, string inspectorCategory = "", Param param = null) : base(id, displayName, getter, inspectorCategory, param) { }

        public static readonly AliveStatType
            Health = new(nameof(Health), LocTerms.Health, a => a.AliveStats.Health, param: new Param { Tweakable = false, Abbreviation = "hp" }),
            MaxHealth = new(nameof(MaxHealth), LocTerms.MaxHealth, a => a.AliveStats.MaxHealth, param: new Param { Abbreviation = "hp" }),
            HealthRegen = new(nameof(HealthRegen), LocTerms.HealthRegeneration, c => c.AliveStats.HealthRegen, param: new Param { Abbreviation = "hp" }),
            ArmorMultiplier = new(nameof(ArmorMultiplier), LocTerms.Armor, a => a.AliveStats.ArmorMultiplier),
            Armor = new(nameof(Armor), LocTerms.Armor, a => a.AliveStats.Armor),
            StatusResistance = new(nameof(StatusResistance), LocTerms.StatusResistance, a => a.AliveStats.StatusResistance),
            ForceStumbleThreshold = new(nameof(ForceStumbleThreshold), LocTerms.ForceStumbleThreshold, a => a.AliveStats.ForceStumbleThreshold),
            TrapDamageMultiplier = new(nameof(TrapDamageMultiplier), LocTerms.TrapDamageMultiplier, a => a.AliveStats.TrapDamageMultiplier);

        public override string ToString() => DisplayName;
    }
}