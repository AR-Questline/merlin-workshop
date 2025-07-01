using Awaken.TG.Main.Timing.ARTime;
using Awaken.TG.Main.Timing.ARTime.Modifiers;
using Awaken.TG.VisualScripts.Units;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Effects {
    [UnitCategory("AR/Skills/Effects")]
    [TypeIcon(typeof(FlowGraph))]
    public class ApplySlowDownTimeUnit : ARUnit, ISkillUnit {
        public const string SlowDownTimeUnitID = "_SlowDownTimeModifier";
        public ValueInput slowDownAmount;
        public ValueOutput slowDownModifier;

        string ID(Flow flow) => this.Skill(flow).ID + SlowDownTimeUnitID;

        protected override void Definition() {
            slowDownAmount = ValueInput<float>("SlowDownAmount");
            slowDownModifier = ValueOutput<MultiplyTimeModifier>("SlowDownModifier");
            DefineSimpleAction("Enter", "Exit", Enter);
        }
        
        void Enter(Flow flow) {
            float slowAmount = flow.GetValue<float>(slowDownAmount) / 100f;
            var modifier = new MultiplyTimeModifier(ID(flow), 1 - slowAmount, 0.5f);
            this.Skill(flow).Owner.GetOrCreateTimeDependent().AddTimeModifier(modifier);
            flow.SetValue(slowDownModifier, modifier);
        }
    }
}