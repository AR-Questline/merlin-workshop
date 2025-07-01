using System;
using System.Collections.Generic;
using System.Linq;
using Awaken.TG.Main.Heroes.CharacterSheet;
using Awaken.TG.Main.Localization;
using Awaken.Utility;
using Awaken.Utility.Enums;

namespace Awaken.TG.Main.Skills {
    public class Keyword : RichEnum {
        public LocString Name { get; }
        public LocString Description { get; }
        public ARColor DescColor { get; }
        
        static Dictionary<string, Keyword> s_keywordsByStrings = new();

        public Keyword(string enumName, string name, string description, ARColor descColor = null) : base(enumName) {
            Name = new LocString {ID = name};
            Description = new LocString {ID = description};
            DescColor = descColor ?? ARColor.SpecialAccent;

            s_keywordsByStrings[enumName] = this;
        }

        [UnityEngine.Scripting.Preserve]
        public static readonly Keyword
            Buildup = new(nameof(Buildup), LocTerms.Buildup, LocTerms.BuildupDesc),

            StatusBleed = new(nameof(StatusBleed), LocTerms.StatusBleed, LocTerms.StatusBleedDesc),
            StatusBurn = new(nameof(StatusBurn), LocTerms.StatusBurn, LocTerms.StatusBurnDesc),
            StatusInferno = new(nameof(StatusInferno), LocTerms.StatusInferno, LocTerms.StatusInfernoDesc),
            StatusChilled = new(nameof(StatusChilled), LocTerms.StatusChilled, LocTerms.StatusChilledDesc),
            StatusFrozen = new(nameof(StatusFrozen), LocTerms.StatusFrozen, LocTerms.StatusFrozenDesc),
            StatusConfusion = new(nameof(StatusConfusion), LocTerms.StatusConfusion, LocTerms.StatusConfusionDesc),
            StatusCorruption = new(nameof(StatusCorruption), LocTerms.StatusCorruption, LocTerms.StatusCorruptionDesc),
            StatusMute = new(nameof(StatusMute), LocTerms.StatusMute, LocTerms.StatusMuteDesc),
            StatusBlind = new(nameof(StatusBlind), LocTerms.StatusBlind, LocTerms.StatusBlindDesc),
            StatusPoison = new(nameof(StatusPoison), LocTerms.StatusPoison, LocTerms.StatusPoisonDesc),
            StatusSlow = new(nameof(StatusSlow), LocTerms.StatusSlow, LocTerms.StatusSlowDesc),
            StatusStun = new(nameof(StatusStun), LocTerms.StatusStun, LocTerms.StatusStunDesc),
            StatusWeak = new(nameof(StatusWeak), LocTerms.StatusWeak, LocTerms.StatusWeakDesc),
            StatusDrunk = new(nameof(StatusDrunk), LocTerms.StatusDrunk, LocTerms.StatusDrunkDesc),
            StatusIntoxicated = new(nameof(StatusIntoxicated), LocTerms.StatusIntoxicated, LocTerms.StatusIntoxicatedDesc),
            StatusFull = new(nameof(StatusFull), LocTerms.StatusFull, LocTerms.StatusFullDesc),
            StatusBerserk = new(nameof(StatusBerserk), LocTerms.StatusBerserk, LocTerms.StatusBerserkDesc),
            StatusHaste = new(nameof(StatusHaste), LocTerms.StatusHaste, LocTerms.StatusHasteDesc),
            StatusRage = new(nameof(StatusRage), LocTerms.StatusRage, LocTerms.StatusRageDesc),
            StatusRejuvenated = new(nameof(StatusRejuvenated), LocTerms.StatusRejuvenated, LocTerms.StatusRejuvenatedDesc),
            StatusOverexerted = new(nameof(StatusOverexerted), LocTerms.StatusOverexerted, LocTerms.StatusOverexertedDesc),
            StatusUntouchable = new(nameof(StatusUntouchable), LocTerms.StatusUntouchable, LocTerms.StatusUntouchableDesc),
            StatusWellRested = new(nameof(StatusWellRested), LocTerms.StatusWellRested, LocTerms.StatusWellRestedDesc),
            StatusResilienceRush = new(nameof(StatusResilienceRush), LocTerms.StatusResilienceRush, LocTerms.StatusResilienceRushDesc),
            StatusFrenzy = new(nameof(StatusFrenzy), LocTerms.StatusFrenzy, LocTerms.StatusFrenzyDesc),

            ManaShield = new(nameof(ManaShield), LocTerms.ManaShield, LocTerms.ManaShieldDesc),
            Lifesteal = new(nameof(Lifesteal), LocTerms.Lifesteal, LocTerms.LifestealDesc),
            WeakSpot = new(nameof(WeakSpot), LocTerms.WeakSpot, LocTerms.WeakSpotDesc),
            CriticalHit = new(nameof(CriticalHit), LocTerms.CriticalHit, LocTerms.CriticalHitDesc),
            SneakDamage = new(nameof(SneakDamage), LocTerms.SneakDamage, LocTerms.SneakDamageDesc),
            Staggered = new(nameof(Staggered), LocTerms.Staggered, LocTerms.StaggeredDesc),
            ConsecutiveHit = new(nameof(ConsecutiveHit), LocTerms.ConsecutiveHit, LocTerms.ConsecutiveHitDesc),

            ToolMining = new(nameof(ToolMining), LocTerms.ToolMining, LocTerms.ToolMiningDesc),
            ToolDigging = new(nameof(ToolDigging), LocTerms.ToolDigging, LocTerms.ToolDiggingDesc),
            ToolFishing = new(nameof(ToolFishing), LocTerms.ToolFishing, LocTerms.ToolFishingDesc),
            ToolSketching = new(nameof(ToolSketching), LocTerms.ToolSketching, LocTerms.ToolSketchingDesc),
            ToolSpyglassing = new(nameof(ToolSpyglassing), LocTerms.ToolSpyglassing, LocTerms.ToolSpyglassingDesc),
            ToolGliding = new(nameof(ToolGliding), LocTerms.ToolGliding, LocTerms.ToolGlidingDesc),
            ToolLumbering = new(nameof(ToolLumbering), LocTerms.ToolLumbering, LocTerms.ToolLumberingDesc);


        public static Keyword KeywordFor(string keyword) {
            if (!s_keywordsByStrings.TryGetValue(keyword, out var value)) {
                value = RichEnum.AllValuesOfType<Keyword>()
                    .FirstOrDefault(k => string.Equals(k.EnumName, keyword, StringComparison.InvariantCultureIgnoreCase));
                s_keywordsByStrings[keyword] = value;
            }

            return value;
        }
    }
}
