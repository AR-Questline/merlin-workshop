using Awaken.TG.Main.General.StatTypes;
using Awaken.TG.Main.Heroes.Stats;
using Unity.Mathematics;

namespace Awaken.TG.Main.Heroes.HUD {
    public class VCHeroWyrdSkillBar : VCStatBarWithFail {
        protected override StatType StatType => HeroStatType.WyrdSkillDuration;
        protected override float Percentage => math.clamp(Target.WyrdSkillDuration?.Percentage ?? 1f, 0f, 1f);
    }
}