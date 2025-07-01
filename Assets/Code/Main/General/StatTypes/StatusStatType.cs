using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Utility.RichEnums;

namespace Awaken.TG.Main.General.StatTypes {
    // === Stats

    [RichEnumDisplayCategory("Status")]
    public class StatusStatType : CharacterStatType {
        public static readonly StatusStatType
            BleedBuildup = new(nameof(BleedBuildup), LocTerms.BleedBuildup, c => c.StatusStats.BleedBuildup, "Buildup"),
            BleedEffectModifier = new(nameof(BleedEffectModifier), LocTerms.BleedEffectModifier, c => c.StatusStats.BleedEffectModifier, "EffectModifier"),
            BurnBuildup = new(nameof(BurnBuildup), LocTerms.BurnBuildup, c => c.StatusStats.BurnBuildup, "Buildup"),
            BurnEffectModifier = new(nameof(BurnEffectModifier), LocTerms.BurnEffectModifier, c => c.StatusStats.BurnEffectModifier, "EffectModifier"),
            FrenzyBuildup = new(nameof(FrenzyBuildup), LocTerms.FrenzyBuildup, c => c.StatusStats.FrenzyBuildup, "Buildup"),
            FrenzyEffectModifier = new(nameof(FrenzyEffectModifier), LocTerms.FrenzyEffectModifier, c => c.StatusStats.FrenzyEffectModifier, "EffectModifier"),
            ConfusionBuildup = new(nameof(ConfusionBuildup), LocTerms.ConfusionBuildup, c => c.StatusStats.ConfusionBuildup, "Buildup"),
            ConfusionEffectModifier = new(nameof(ConfusionEffectModifier), LocTerms.ConfusionEffectModifier, c => c.StatusStats.ConfusionEffectModifier, "EffectModifier"),
            CorruptionBuildup = new(nameof(CorruptionBuildup), LocTerms.CorruptionBuildup, c => c.StatusStats.CorruptionBuildup, "Buildup"),
            CorruptionEffectModifier = new(nameof(CorruptionEffectModifier), LocTerms.CorruptionEffectModifier, c => c.StatusStats.CorruptionEffectModifier, "EffectModifier"),
            MuteBuildup = new(nameof(MuteBuildup), LocTerms.MuteBuildup, c => c.StatusStats.MuteBuildup, "Buildup"),
            MuteEffectModifier = new(nameof(MuteEffectModifier), LocTerms.MuteEffectModifier, c => c.StatusStats.MuteEffectModifier, "EffectModifier"),
            PoisonBuildup = new(nameof(PoisonBuildup), LocTerms.PoisonBuildup, c => c.StatusStats.PoisonBuildup, "Buildup"),
            PoisonEffectModifier = new(nameof(PoisonEffectModifier), LocTerms.PoisonEffectModifier, c => c.StatusStats.PoisonEffectModifier, "EffectModifier"),
            SlowBuildup = new(nameof(SlowBuildup), LocTerms.SlowBuildup, c => c.StatusStats.SlowBuildup, "Buildup"),
            SlowEffectModifier = new(nameof(SlowEffectModifier), LocTerms.SlowEffectModifier, c => c.StatusStats.SlowEffectModifier, "EffectModifier"),
            StunBuildup = new(nameof(StunBuildup), LocTerms.StunBuildup, c => c.StatusStats.StunBuildup, "Buildup"),
            StunEffectModifier = new(nameof(StunEffectModifier), LocTerms.StunEffectModifier, c => c.StatusStats.StunEffectModifier, "EffectModifier"),
            WeakBuildup = new(nameof(WeakBuildup), LocTerms.WeakBuildup, c => c.StatusStats.WeakBuildup, "Buildup"),
            WeakEffectModifier = new(nameof(WeakEffectModifier), LocTerms.WeakEffectModifier, c => c.StatusStats.WeakEffectModifier, "EffectModifier"),
            
            DrunkBuildup = new(nameof(DrunkBuildup), LocTerms.DrunkBuildup, c => c.StatusStats.DrunkBuildup, "Buildup"),
            DrunkEffectModifier = new(nameof(DrunkEffectModifier), LocTerms.DrunkEffectModifier, c => c.StatusStats.DrunkEffectModifier, "EffectModifier"),
            IntoxicatedBuildup = new(nameof(IntoxicatedBuildup), LocTerms.IntoxicatedBuildup, c => c.StatusStats.IntoxicatedBuildup, "Buildup"),
            IntoxicatedEffectModifier = new(nameof(IntoxicatedEffectModifier), LocTerms.IntoxicatedEffectModifier, c => c.StatusStats.IntoxicatedEffectModifier, "EffectModifier"),
            FullBuildup = new(nameof(FullBuildup), LocTerms.FullBuildup, c => c.StatusStats.FullBuildup, "Buildup"),
            FullEffectModifier = new(nameof(FullEffectModifier), LocTerms.FullEffectModifier, c => c.StatusStats.FullEffectModifier, "EffectModifier");


        protected StatusStatType(string id, string displayName, Func<ICharacter, Stat> getter,
                                    string inspectorCategory = "", Param param = null)
            : base(id, displayName, getter, inspectorCategory, param) { }

        public override string ToString() => DisplayName;
    }
}
