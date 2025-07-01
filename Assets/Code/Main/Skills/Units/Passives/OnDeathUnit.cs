using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.MVC.Events;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    [UnitCategory("AR/Skills/Passives")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class OnDeathUnit : PassiveListenerWithPayloadUnit<IAlive, DamageOutcome> {
        protected override IAlive Source(Skill skill, Flow flow) => skill.Owner;
        protected override Event<IAlive, DamageOutcome> Event(Skill skill, Flow flow) => IAlive.Events.BeforeDeath;
    }
}