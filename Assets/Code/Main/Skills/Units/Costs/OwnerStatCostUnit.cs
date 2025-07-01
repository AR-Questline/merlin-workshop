using Awaken.TG.Main.General.Costs;
using Awaken.TG.Main.Heroes.Stats;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Costs {
    [UnitCategory("AR/Skills/Costs")]
    [TypeIcon(typeof(ICost))]
    public class OwnerStatCostUnit : Unit, ISkillUnit {
        ValueInput _statType;
        ValueInput _value;
        [UnityEngine.Scripting.Preserve] ValueOutput _cost;

        protected override void Definition() {
            _statType = ValueInput<StatType>("statType");
            _value = ValueInput("value", 0F);
            _cost = ValueOutput("cost", Cost);
        }

        ICost Cost(Flow flow) {
            var skill = this.Skill(flow);
            var owner = skill.Owner;
            if (owner == null) {
                return null;
            }

            return new StatCost(owner.Stat(flow.GetValue<StatType>(_statType)), flow.GetValue<float>(_value),
                skill: skill);
        }
    }
}