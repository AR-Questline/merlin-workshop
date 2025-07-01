using Awaken.TG.Main.Character;
using Awaken.TG.Main.General.Costs;
using Awaken.TG.Main.Heroes.Stats;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class ReserveManaCostUnit : SpendManaCostUnit {
        protected override bool SpendMana(Skill skill, Stat stat, ICharacter owner, float manaCost, bool perSecond) {
            if (base.SpendMana(skill, stat, owner, manaCost, perSecond)) {
                skill.ReservedCosts.Add(new StatCost(stat, manaCost, skill: skill));
                return true;
            }
            return false;
        }
    }
}