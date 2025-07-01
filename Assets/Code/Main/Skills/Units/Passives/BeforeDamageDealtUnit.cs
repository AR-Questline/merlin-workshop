using Awaken.TG.Main.Character;
using Awaken.TG.Main.Fights.DamageInfo;
using Awaken.TG.MVC.Events;
using Unity.VisualScripting;

namespace Awaken.TG.Main.Skills.Units.Passives {
    [UnitCategory("AR/Skills/Passives")]
    [TypeIcon(typeof(FlowGraph))]
    [UnityEngine.Scripting.Preserve]
    public class BeforeDamageDealtUnit : PassiveListenerWithPayloadUnit<ICharacter, Damage> {
        protected override ICharacter Source(Skill skill, Flow flow) => skill.Owner;
        protected override Event<ICharacter, Damage> Event(Skill skill, Flow flow) => HealthElement.Events.BeforeDamageDealt;
    }
}