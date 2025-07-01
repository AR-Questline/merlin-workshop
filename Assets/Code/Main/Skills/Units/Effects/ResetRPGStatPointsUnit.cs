using Awaken.TG.Main.Heroes;
using Awaken.TG.Main.Heroes.Development;
using Awaken.TG.Main.Heroes.Stats;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class ResetRPGStatPointsUnit : ARUnit, ISkillUnit {
        protected override void Definition() {
            DefineSimpleAction(
                _ => {
                    Hero current = Hero.Current;
                    HeroDevelopment currentDevelopment = current.Development;
                    if (current == null || currentDevelopment == null) return;
                    
                    foreach (Stat heroRPGStat in current.HeroRPGStats.GetHeroRPGStats()) {
                        currentDevelopment.BaseStatPoints.IncreaseBy(heroRPGStat.BaseInt - 1);
                        heroRPGStat.SetTo(1);
                    }
                });
        }
    }
}