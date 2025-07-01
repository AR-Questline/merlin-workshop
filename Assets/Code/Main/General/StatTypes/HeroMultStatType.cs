using System;
using Awaken.TG.Main.Character;
using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.Main.Localization;
using Awaken.TG.Main.Utility.RichEnums;

namespace Awaken.TG.Main.General.StatTypes {
    [RichEnumDisplayCategory("Hero/Multipliers")]
    public class HeroMultStatType : HeroStatType {
        public static readonly HeroMultStatType
            ExpMultiplier = new (nameof(ExpMultiplier), LocTerms.Experience,
                h => h.HeroMultStats.ExpMultiplier, "", new Param{Abbreviation = "xp"}),
            KillExpMultiplier = new(nameof(KillExpMultiplier), LocTerms.Experience,
                h => h.HeroMultStats.KillExpMultiplier, "", new Param { Abbreviation = "k-xp" }),
            WealthMultiplier = new (nameof(WealthMultiplier), LocTerms.Wealth,
                h => h.HeroMultStats.WealthMultiplier, "", new Param{Abbreviation = "$"}),
            ProfMultiplier = new (nameof(ProfMultiplier), LocTerms.Proficiency,
                h => h.HeroMultStats.ProfMultiplier, "", new Param{Abbreviation = "prof"});

        HeroMultStatType(string id, string displayName, Func<Hero, Stat> getter, string inspectorCategory = "", Param param = null)
            : base(id, displayName, getter, inspectorCategory, param) { }
    }
}
