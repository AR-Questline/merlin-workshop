using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;

namespace Awaken.TG.Main.Heroes.HUD {
    public class VCHeroOxygenBar : VCStatBar<Hero> {
        protected override StatType StatType => HeroStatType.OxygenLevel;
        protected override float Percentage => Target.HeroStats?.OxygenLevel?.Percentage ?? 1f;
    }
}