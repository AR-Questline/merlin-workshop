using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.MVC.Events;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    [UnitCategory("AR/Skills/Passives")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class OnDamageParriedUnit : PassiveListenerWithPayloadUnit<HealthElement, Damage> {
        protected override HealthElement Source(Skill skill, Flow flow) => skill.Owner.HealthElement;
        protected override Event<HealthElement, Damage> Event(Skill skill, Flow flow) => HealthElement.Events.OnDamageParried;
    }
}